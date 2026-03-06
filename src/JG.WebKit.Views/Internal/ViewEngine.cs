namespace JG.WebKit.Views.Internal;

using System.Collections.Concurrent;
using JG.WebKit.Views.Abstractions;
using JG.WebKit.Views.Compilation;
using JG.WebKit.Views.Nodes;

/// <summary>
/// Represents a compiled template with its rendering delegate and metadata.
/// </summary>
internal sealed class CompiledTemplate
{
    /// <summary>
    /// Gets the compiled rendering function that executes the template.
    /// </summary>
    public required Func<TemplateContext, ValueTask<string>> RenderFunc { get; init; }
    
    /// <summary>
    /// Gets the timestamp when this template was compiled.
    /// </summary>
    public required DateTimeOffset CompiledAt { get; init; }
    
    /// <summary>
    /// Gets the parsed nodes that make up the template.
    /// </summary>
    public required IReadOnlyList<INode> Nodes { get; init; }
    
    /// <summary>
    /// Gets the layout path if this template declares a layout, otherwise null.
    /// </summary>
    public string? LayoutPath { get; init; }
    
    /// <summary>
    /// Gets the estimated output size for StringBuilder pre-allocation.
    /// </summary>
    public int EstimatedOutputSize { get; init; }
}

/// <summary>
/// Core view engine implementation that compiles and renders templates.
/// Thread-safe and optimized for high-throughput scenarios.
/// </summary>
internal sealed class ViewEngine : IViewEngine
{
    private readonly ITemplateProvider _provider;
    private readonly ViewEngineOptions _options;
    private readonly Dictionary<string, ITemplateHelper> _helpers;
    private readonly ConcurrentDictionary<string, CompiledTemplate> _cache;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the ViewEngine class.
    /// </summary>
    /// <param name="provider">The template provider for loading template sources.</param>
    /// <param name="options">Configuration options for the engine.</param>
    /// <param name="helpers">Dictionary of custom template helpers.</param>
    public ViewEngine(ITemplateProvider provider, ViewEngineOptions options, Dictionary<string, ITemplateHelper> helpers)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _helpers = helpers ?? new Dictionary<string, ITemplateHelper>();
        _cache = new ConcurrentDictionary<string, CompiledTemplate>();

        if (options.EnableHotReload && provider is FileTemplateProvider fileProvider)
        {
            fileProvider.EnableHotReload(path => InvalidateCache(path));
        }
    }

    /// <summary>
    /// Renders a template asynchronously from the specified path.
    /// </summary>
    /// <param name="templatePath">The path of the template to render.</param>
    /// <param name="context">The context object containing template data.</param>
    /// <param name="ct">Optional. A cancellation token.</param>
    /// <returns>The rendered template content.</returns>
    public async ValueTask<string> RenderAsync(string templatePath, TemplateContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(templatePath);
        ArgumentNullException.ThrowIfNull(context);

        var source = await _provider.GetTemplateAsync(templatePath, ct).ConfigureAwait(false);
        if (source == null)
            return string.Empty;

        return await RenderTemplateSourceAsync(source, context, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Renders an inline template string with the provided context.
    /// Useful for rendering dynamic templates without file system access.
    /// </summary>
    /// <param name="templateContent">The template content as a string.</param>
    /// <param name="context">The rendering context containing data and globals.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>The rendered HTML string.</returns>
    public async ValueTask<string> RenderStringAsync(string templateContent, TemplateContext context, CancellationToken ct = default)
    {
        var source = new TemplateSource
        {
            Content = templateContent,
            Path = $"__inline__{templateContent.GetHashCode():X}__",
            LastModified = DateTimeOffset.UtcNow
        };

        return await RenderTemplateSourceAsync(source, context, ct).ConfigureAwait(false);
    }

    private async ValueTask<string> RenderTemplateSourceAsync(TemplateSource source, TemplateContext context, CancellationToken ct)
    {
        var cacheKey = NormalizePath(source.Path);
        CompiledTemplate compiled;

        if (_options.CacheCompiledTemplates && _cache.TryGetValue(cacheKey, out var cached))
        {
            compiled = cached;
        }
        else
        {
            compiled = CompileTemplate(source);
            if (_options.CacheCompiledTemplates)
            {
                _cache.TryAdd(cacheKey, compiled);
            }
        }

        // Extract sections from nodes
        var sections = new Dictionary<string, List<INode>>();
        ExtractSections(compiled.Nodes, sections);

        var result = await compiled.RenderFunc(context).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(compiled.LayoutPath))
        {
            result = await ApplyLayoutAsync(compiled.LayoutPath, result, sections, context, 0, ct).ConfigureAwait(false);
        }

        return result;
    }

    private static void ExtractSections(IReadOnlyList<INode> nodes, Dictionary<string, List<INode>> sections)
    {
        foreach (var node in nodes)
        {
            if (node is SectionNode sectionNode)
            {
                sections[sectionNode.SectionName] = sectionNode.Nodes;
            }
        }
    }

    private CompiledTemplate CompileTemplate(TemplateSource source)
    {
        var tokenizer = new Tokenizer(source.Content);
        var tokens = tokenizer.Tokenize();

        var parser = new Parser(tokens, _helpers, _options);
        var nodes = parser.Parse();

        var estimatedSize = source.Content.Length;
        var layoutPath = FindLayoutPath(nodes);

        var renderFunc = CompileNodes(nodes);

        return new CompiledTemplate
        {
            RenderFunc = renderFunc,
            CompiledAt = DateTimeOffset.UtcNow,
            Nodes = nodes,
            LayoutPath = layoutPath,
            EstimatedOutputSize = estimatedSize
        };
    }

    private Func<TemplateContext, ValueTask<string>> CompileNodes(IReadOnlyList<INode> nodes)
    {
        return async (context) =>
        {
            var sb = new StringBuilder();
            foreach (var node in nodes)
            {
                if (node is PartialNode partialNode)
                {
                    // Handle partials with access to provider
                    sb.Append(await RenderPartialAsync(partialNode, context).ConfigureAwait(false));
                }
                else if (node is not LayoutNode && node is not SectionNode && node is not YieldNode && node is not YieldDefaultNode)
                {
                    sb.Append(await node.RenderAsync(context).ConfigureAwait(false));
                }
            }
            return sb.ToString();
        };
    }

    private static string? FindLayoutPath(IReadOnlyList<INode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is LayoutNode layoutNode)
                return layoutNode.LayoutName;
        }
        return null;
    }

    private async ValueTask<string> ApplyLayoutAsync(string layoutPath, string content, Dictionary<string, List<INode>> sections, TemplateContext context, int depth, CancellationToken ct)
    {
        // Prevent infinite layout nesting
        if (depth >= _options.MaxIncludeDepth)
            return content;

        var layoutSource = await _provider.GetTemplateAsync(layoutPath, ct).ConfigureAwait(false);
        if (layoutSource == null)
            return content;

        var tokenizer = new Tokenizer(layoutSource.Content);
        var tokens = tokenizer.Tokenize();

        var parser = new Parser(tokens, _helpers, _options);
        var layoutNodes = parser.Parse();

        var sb = new StringBuilder();
        foreach (var node in layoutNodes)
        {
            if (node is YieldNode yieldNode)
            {
                if (yieldNode.SectionName == "content")
                {
                    sb.Append(content);
                }
                else if (sections.TryGetValue(yieldNode.SectionName, out var sectionNodes))
                {
                    foreach (var sectionNode in sectionNodes)
                    {
                        sb.Append(await sectionNode.RenderAsync(context, depth).ConfigureAwait(false));
                    }
                }
            }
            else if (node is YieldDefaultNode yieldDefaultNode)
            {
                if (yieldDefaultNode.SectionName == "content")
                {
                    sb.Append(content);
                }
                else if (sections.TryGetValue(yieldDefaultNode.SectionName, out var sectionNodes))
                {
                    foreach (var sectionNode in sectionNodes)
                    {
                        sb.Append(await sectionNode.RenderAsync(context, depth).ConfigureAwait(false));
                    }
                }
                else
                {
                    // Render default content when section not provided
                    sb.Append(await yieldDefaultNode.RenderAsync(context, depth).ConfigureAwait(false));
                }
            }
            else if (node is PartialNode partialNode)
            {
                // Handle partials in layout rendering
                sb.Append(await RenderPartialAsync(partialNode, context, 0).ConfigureAwait(false));
            }
            else if (node is not LayoutNode && node is not SectionNode)
            {
                sb.Append(await node.RenderAsync(context).ConfigureAwait(false));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders a partial template with optional context.
    /// This method is the core of partial inclusion, handling:
    /// - Path resolution with multiple fallback attempts
    /// - Variant-based partial selection (e.g., card-mobile.html vs card.html)
    /// - Context transformation from POCO objects to dictionaries
    /// - Depth tracking to prevent infinite inclusion
    /// </summary>
    private async ValueTask<string> RenderPartialAsync(PartialNode partialNode, TemplateContext context, int depth = 0)
    {
        // Prevent infinite partial nesting (e.g., partial includes itself)
        if (depth >= _options.MaxIncludeDepth)
            return string.Empty;

        TemplateSource? partialSource = null;
        
        // Try multiple path combinations to find the partial
        var pathsToTry = new List<string>();
        
        if (!string.IsNullOrEmpty(partialNode.Variant))
        {
            // With variant: Try variant-specific paths first
            pathsToTry.Add($"{_options.PartialPath}/{partialNode.Path}-{partialNode.Variant}{_options.TemplateExtension}");
            pathsToTry.Add($"{partialNode.Path}-{partialNode.Variant}{_options.TemplateExtension}");
            pathsToTry.Add($"{partialNode.Path}-{partialNode.Variant}");
        }
        
        // Without variant (fallback or primary)
        pathsToTry.Add($"{_options.PartialPath}/{partialNode.Path}{_options.TemplateExtension}");
        pathsToTry.Add($"{partialNode.Path}{_options.TemplateExtension}");
        pathsToTry.Add(partialNode.Path);  // As-is for full paths

        foreach (var path in pathsToTry)
        {
            partialSource = await _provider.GetTemplateAsync(path).ConfigureAwait(false);
            if (partialSource != null)
                break;
        }

        if (partialSource == null)
            return string.Empty;

        // Resolve context if expression is provided
        var partialContext = context;
        if (!string.IsNullOrEmpty(partialNode.ContextExpr))
        {
            var contextValue = Expression.Parse(partialNode.ContextExpr).Evaluate(context);
            if (contextValue != null)
            {
                // Use case-insensitive dictionary for partial data
                var partialData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                
                // For dictionaries, copy entries
                if (contextValue is IReadOnlyDictionary<string, object?> roDict)
                {
                    foreach (var kvp in roDict)
                        partialData[kvp.Key] = kvp.Value;
                }
                else if (contextValue is IDictionary<string, object?> mutableDict)
                {
                    foreach (var kvp in mutableDict)
                        partialData[kvp.Key] = kvp.Value;
                }
                else
                {
                    // For POCO objects, reflect public properties
                    var type = contextValue.GetType();
                    var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    foreach (var prop in props)
                    {
                        try
                        {
                            var value = prop.GetValue(contextValue, null);
                            partialData[prop.Name] = value;
                        }
                        catch { }
                    }
                }

                partialContext = new TemplateContext(partialData, context.Globals, context.HttpContext);
            }
        }

        // Render the partial
        var tokenizer = new Tokenizer(partialSource.Content);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens, _helpers, _options);
        var nodes = parser.Parse();

        var sb = new StringBuilder();
        foreach (var node in nodes)
        {
            if (node is PartialNode nestedPartial)
            {
                sb.Append(await RenderPartialAsync(nestedPartial, partialContext, depth + 1).ConfigureAwait(false));
            }
            else if (node is not LayoutNode && node is not SectionNode && node is not YieldNode && node is not YieldDefaultNode)
            {
                sb.Append(await node.RenderAsync(partialContext, depth + 1).ConfigureAwait(false));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Invalidates the cache for the specified template path, or clears the entire cache if no path is specified.
    /// </summary>
    /// <param name="templatePath">Optional. The path of the template to invalidate.</param>
    public void InvalidateCache(string? templatePath = null)
    {
        if (templatePath == null)
        {
            _cache.Clear();
        }
        else
        {
            var key = NormalizePath(templatePath);
            _cache.TryRemove(key, out _);
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').ToLowerInvariant();
    }

    /// <summary>
    /// Disposes the view engine, releasing any resources.
    /// </summary>
    public void Dispose()
    {
    if (_disposed)
        return;

    if (_provider is IDisposable disposable)
        disposable.Dispose();

    _disposed = true;
    }
}
