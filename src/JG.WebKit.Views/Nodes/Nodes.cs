namespace JG.WebKit.Views.Nodes;

using System.Collections;
using JG.WebKit.Views.Internal;

/// <summary>
/// Represents a renderable node in the template's abstract syntax tree (AST).
/// All nodes implement async rendering with depth tracking for recursion prevention.
/// </summary>
internal interface INode
{
    /// <summary>
    /// Renders this node asynchronously to produce HTML output.
    /// </summary>
    /// <param name="context">The template context containing data and globals.</param>
    /// <param name="depth">Current inclusion depth for preventing infinite recursion.</param>
    /// <returns>The rendered HTML string.</returns>
    ValueTask<string> RenderAsync(TemplateContext context, int depth = 0);
}

/// <summary>
/// Represents a literal text node that outputs its content unchanged.
/// Used for static HTML/text content between template tags.
/// </summary>
internal sealed class LiteralNode : INode
{
    private readonly string _content;

    /// <summary>
    /// Initializes a new instance of the LiteralNode class.
    /// </summary>
    /// <param name="content">The literal text content to render.</param>
    public LiteralNode(string content)
    {
        _content = content;
    }

    /// <summary>
    /// Renders the literal content unchanged.
    /// </summary>
    public ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        return new ValueTask<string>(_content);
    }
}

/// <summary>
/// Represents a variable node that evaluates expressions and outputs HTML-escaped content.
/// Supports dot notation (user.name), array indexing (items[0]), and helper calls.
/// Syntax: {{ expression }} or {{ helperName args }}
/// </summary>
internal sealed class VariableNode : INode
{
    private readonly string _expression;
    private readonly Lazy<Expression> _parsed;
    private readonly Dictionary<string, ITemplateHelper> _helpers;

    /// <summary>
    /// Initializes a new instance of the VariableNode class.
    /// </summary>
    /// <param name="expression">The expression to evaluate (e.g., "user.name" or "date publishedAt").</param>
    /// <param name="helpers">Optional dictionary of registered helpers for helper call detection.</param>
    public VariableNode(string expression, Dictionary<string, ITemplateHelper>? helpers = null)
    {
        _expression = expression;
        _helpers = helpers ?? new Dictionary<string, ITemplateHelper>();
        _parsed = new Lazy<Expression>(() => Expression.Parse(_expression));
    }

    /// <summary>
    /// Renders the variable or helper call with HTML escaping.
    /// If the expression starts with a registered helper name, executes the helper.
    /// Otherwise evaluates the expression and HTML-escapes the result.
    /// </summary>
    public async ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        // Check if this is a helper call
        var firstWord = ExtractFirstWord(_expression);
        if (_helpers.TryGetValue(firstWord, out var helper))
        {
            var args = ParseHelperArguments(_expression, context);
            var result = helper.Execute(args, context);
            return result ?? string.Empty;
        }

        // Otherwise evaluate as variable
        var value = _parsed.Value.Evaluate(context);
        var str = ToString(value);
        return HtmlEscape.Escape(str);
    }

    private static string ExtractFirstWord(string expression)
    {
        var trimmed = expression.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        return spaceIndex >= 0 ? trimmed[..spaceIndex] : trimmed;
    }

    private static object?[] ParseHelperArguments(string expression, TemplateContext context)
    {
        var trimmed = expression.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex < 0)
            return Array.Empty<object?>();

        var args = trimmed[(spaceIndex + 1)..].Trim();
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        foreach (var ch in args)
        {
            if ((ch == '"' || ch == '\'') && !inQuotes)
            {
                inQuotes = true;
                quoteChar = ch;
                current.Append(ch);
            }
            else if (ch == quoteChar && inQuotes)
            {
                inQuotes = false;
                current.Append(ch);
            }
            else if (ch == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        // Evaluate each argument
        var result = new object?[parts.Count];
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i].Trim();
            // If it's a quoted string, unquote it
            if ((part.StartsWith('"') && part.EndsWith('"')) || (part.StartsWith('\'') && part.EndsWith('\'')))
            {
                result[i] = part[1..^1];
            }
            // Try to parse as an integer
            else if (int.TryParse(part, out var intVal))
            {
                result[i] = intVal;
            }
            // Try to parse as a double
            else if (double.TryParse(part, out var doubleVal))
            {
                result[i] = doubleVal;
            }
            else
            {
                // Otherwise evaluate as an expression
                result[i] = Expression.Parse(part).Evaluate(context);
            }
        }

        return result;
    }

    private static string ToString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            IEnumerable<object?> enumerable => string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }
}

/// <summary>
/// Represents a raw output variable node that renders unescaped content.
/// SECURITY WARNING: Only use with trusted content to prevent XSS attacks.
/// Syntax: {{{ rawVariable }}}
/// Can be globally disabled via ViewEngineOptions.AllowRawOutput.
/// </summary>
internal sealed class RawVariableNode : INode
{
    private readonly string _expression;
    private readonly Lazy<Expression> _parsed;
    private readonly bool _allowRawOutput;

    /// <summary>
    /// Initializes a new instance of the RawVariableNode class.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="allowRawOutput">Whether raw output is allowed (from ViewEngineOptions).</param>
    public RawVariableNode(string expression, bool allowRawOutput = false)
    {
        _expression = expression;
        _allowRawOutput = allowRawOutput;
        _parsed = new Lazy<Expression>(() => Expression.Parse(_expression));
    }

    /// <summary>
    /// Renders the variable value. If AllowRawOutput is false, HTML-escapes the output for security.
    /// </summary>
    public ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        var value = _parsed.Value.Evaluate(context);
        var str = value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            IEnumerable<object?> enumerable => string.Empty,
            _ => value.ToString() ?? string.Empty
        };
        
        // If raw output is not allowed, escape it
        if (!_allowRawOutput)
            return new ValueTask<string>(HtmlEscape.Escape(str));
        
        return new ValueTask<string>(str);
    }
}

/// <summary>
/// Represents a conditional node with if/elseif/else branches.
/// Supports comparison operators, logical negation, and truthy checks.
/// Syntax: {{#if condition }}...{{#elseif condition }}...{{#else}}...{{/if}}
/// </summary>
internal sealed class IfNode : INode
{
    private readonly string _condition;
    private readonly List<INode> _trueNodes;
    private readonly Dictionary<string, List<INode>> _elseIfNodes;
    private readonly List<INode>? _elseNodes;
    private readonly Lazy<ConditionEvaluator> _evaluator;

    /// <summary>
    /// Initializes a new instance of the IfNode class.
    /// </summary>
    /// <param name="condition">The primary condition expression.</param>
    /// <param name="trueNodes">Nodes to render if the primary condition is true.</param>
    /// <param name="elseIfNodes">Dictionary of elseif conditions and their nodes.</param>
    /// <param name="elseNodes">Nodes to render if all conditions are false (optional).</param>
    public IfNode(string condition, List<INode> trueNodes, Dictionary<string, List<INode>> elseIfNodes, List<INode>? elseNodes)
    {
        _condition = condition;
        _trueNodes = trueNodes;
        _elseIfNodes = elseIfNodes;
        _elseNodes = elseNodes;
        _evaluator = new Lazy<ConditionEvaluator>(() => new ConditionEvaluator(_condition));
    }

    /// <summary>
    /// Evaluates the condition(s) and renders the appropriate branch.
    /// Evaluates conditions in order: if -> elseif(s) -> else.
    /// </summary>
    public async ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        var result = new StringBuilder();

        if (_evaluator.Value.Evaluate(context))
        {
            foreach (var node in _trueNodes)
            {
                result.Append(await node.RenderAsync(context, depth).ConfigureAwait(false));
            }
        }
        else
        {
            var rendered = false;
            foreach (var (condition, nodes) in _elseIfNodes)
            {
                var evaluator = new ConditionEvaluator(condition);
                if (evaluator.Evaluate(context))
                {
                    foreach (var node in nodes)
                    {
                        result.Append(await node.RenderAsync(context, depth).ConfigureAwait(false));
                    }
                    rendered = true;
                    break;
                }
            }

            if (!rendered && _elseNodes != null)
            {
                foreach (var node in _elseNodes)
                {
                    result.Append(await node.RenderAsync(context, depth).ConfigureAwait(false));
                }
            }
        }

        return result.ToString();
    }
}

/// <summary>
/// Represents a loop node that iterates over collections with optional empty fallback.
/// Injects the current item and index into the rendering context.
/// Syntax: {{#each collection as itemName }}...{{#empty}}...{{/each}}
/// </summary>
internal sealed class EachNode : INode
{
    private readonly string _collectionExpr;
    private readonly string _itemName;
    private readonly List<INode> _bodyNodes;
    private readonly List<INode>? _emptyNodes;
    private readonly Lazy<Expression> _collectionParsed;

    /// <summary>
    /// Initializes a new instance of the EachNode class.
    /// </summary>
    /// <param name="collectionExpr">Expression that evaluates to an enumerable collection.</param>
    /// <param name="itemName">Variable name for the current item in the loop.</param>
    /// <param name="bodyNodes">Nodes to render for each item in the collection.</param>
    /// <param name="emptyNodes">Nodes to render if the collection is null or empty (optional).</param>
    public EachNode(string collectionExpr, string itemName, List<INode> bodyNodes, List<INode>? emptyNodes)
    {
        _collectionExpr = collectionExpr;
        _itemName = itemName;
        _bodyNodes = bodyNodes;
        _emptyNodes = emptyNodes;
        _collectionParsed = new Lazy<Expression>(() => Expression.Parse(_collectionExpr));
    }

    /// <summary>
    /// Renders the loop by iterating over the collection.
    /// For each item, creates a new context with:
    /// - {itemName}: the current item
    /// - {itemName}.index: the current iteration index (0-based)
    /// If the collection is empty or null, renders the empty block if present.
    /// </summary>
    public async ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        var collection = _collectionParsed.Value.Evaluate(context);

        if (!IsEnumerable(collection) || !HasElements(collection))
        {
            if (_emptyNodes != null)
            {
                var emptyResult = new StringBuilder();
                foreach (var node in _emptyNodes)
                {
                    emptyResult.Append(await node.RenderAsync(context, depth).ConfigureAwait(false));
                }
                return emptyResult.ToString();
            }
            return string.Empty;
        }

        var result = new StringBuilder();
        var index = 0;

        foreach (var item in (IEnumerable)(collection ?? throw new InvalidOperationException()))
        {
            var itemData = new Dictionary<string, object?>(context.Data)
            {
                [_itemName] = item,
                [$"{_itemName}.index"] = index
            };

            var itemContext = new TemplateContext(itemData, context.Globals ?? new Dictionary<string, object?>(), context.HttpContext);

            foreach (var node in _bodyNodes)
            {
                result.Append(await node.RenderAsync(itemContext, depth).ConfigureAwait(false));
            }

            index++;
        }

        return result.ToString();
    }

    private static bool IsEnumerable(object? obj)
    {
        return obj is IEnumerable && !(obj is string);
    }

    private static bool HasElements(object? obj)
    {
        if (obj is not IEnumerable enumerable)
            return false;

        foreach (var _ in enumerable)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Represents a comment node that produces no output.
/// Comments are stripped during rendering for clean HTML output.
/// Syntax: {{-- comment text }}
/// </summary>
internal sealed class CommentNode : INode
{
    /// <summary>
    /// Renders nothing (comments are not included in output).
    /// </summary>
    public ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        return new ValueTask<string>(string.Empty);
    }
}

/// <summary>
/// Represents a partial include node with optional variant and context.
/// Partials are sub-templates that can be included and reused across templates.
/// Syntax: {{> partialName }}, {{> partialName "variant" }}, {{> partialName context }}
/// Rendering is delegated to ViewEngine.RenderPartialAsync for proper path resolution and caching.
/// </summary>
internal sealed class PartialNode : INode
{
    private readonly string _path;
    private readonly string? _variant;
    private readonly string? _contextExpr;

    /// <summary>
    /// Initializes a new instance of the PartialNode class.
    /// </summary>
    /// <param name="path">The partial name/path (e.g., "header", "cards/featured").</param>
    /// <param name="variant">Optional variant suffix (e.g., "mobile" for header-mobile.html).</param>
    /// <param name="contextExpr">Optional expression to evaluate for the partial's context.</param>
    /// <param name="provider">Unused parameter (kept for compatibility).</param>
    /// <param name="options">Unused parameter (kept for compatibility).</param>
    public PartialNode(string path, string? variant = null, string? contextExpr = null, ITemplateProvider? provider = null, ViewEngineOptions? options = null)
    {
        _path = path;
        _variant = variant;
        _contextExpr = contextExpr;
    }

    /// <summary>
    /// Rendering is handled by ViewEngine's RenderPartialAsync method.
    /// This method returns empty as partials are processed during compilation.
    /// </summary>
    public ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        return new ValueTask<string>(string.Empty);
    }

    /// <summary>
    /// Gets the partial name/path.
    /// </summary>
    public string Path => _path;
    
    /// <summary>
    /// Gets the optional variant name.
    /// </summary>
    public string? Variant => _variant;
    
    /// <summary>
    /// Gets the optional context expression.
    /// </summary>
    public string? ContextExpr => _contextExpr;
}

/// <summary>
/// Represents a layout declaration node.
/// Indicates that this template should be wrapped in the specified layout.
/// Syntax: {{#layout "layoutName" }}
/// The layout node itself produces no output; layout application happens after template rendering.
/// </summary>
internal sealed class LayoutNode : INode
{
    private readonly string _layoutName;

    /// <summary>
    /// Initializes a new instance of the LayoutNode class.
    /// </summary>
    /// <param name="layoutName">The name of the layout template (e.g., "main", "_layouts/site").</param>
    public LayoutNode(string layoutName)
    {
        _layoutName = layoutName;
    }

    /// <summary>
    /// Layout nodes produce no output in the main render pass.
    /// The layout name is extracted during compilation and used for post-render layout application.
    /// </summary>
    public ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        return new ValueTask<string>(string.Empty);
    }

    /// <summary>
    /// Gets the layout template name.
    /// </summary>
    public string LayoutName => _layoutName;
}

/// <summary>
/// Represents a section definition node for layout content injection.
/// Sections defined in content templates are injected into layout yield points.
/// Syntax: {{#section "sectionName" }}content{{/section}}
/// Section nodes produce no output in the main render; content is extracted and injected during layout rendering.
/// </summary>
internal sealed class SectionNode : INode
{
    private readonly string _sectionName;
    private readonly List<INode> _nodes;

    /// <summary>
    /// Initializes a new instance of the SectionNode class.
    /// </summary>
    /// <param name="sectionName">The section name (must match a yield in the layout).</param>
    /// <param name="nodes">The nodes to render when this section is yielded.</param>
    public SectionNode(string sectionName, List<INode> nodes)
    {
        _sectionName = sectionName;
        _nodes = nodes;
    }

    /// <summary>
    /// Sections produce no output in the main content render.
    /// Their content is extracted and rendered only when yielded by the layout.
    /// </summary>
    public async ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        return string.Empty;
    }

    /// <summary>
    /// Gets the section name.
    /// </summary>
    public string SectionName => _sectionName;
    
    /// <summary>
    /// Gets the nodes that make up the section content.
    /// </summary>
    public List<INode> Nodes => _nodes;
}

/// <summary>
/// Represents a yield point in a layout where content or sections are injected.
/// Syntax: {{#yield "sectionName" }}
/// If sectionName is "content", injects the entire rendered content.
/// Otherwise, injects the named section from the content template.
/// </summary>
internal sealed class YieldNode : INode
{
    private readonly string _sectionName;

    /// <summary>
    /// Initializes a new instance of the YieldNode class.
    /// </summary>
    /// <param name="sectionName">The section name to yield ("content" for main content).</param>
    public YieldNode(string sectionName)
    {
        _sectionName = sectionName;
    }

    /// <summary>
    /// Yield nodes produce no output when rendered directly.
    /// Content injection is handled by ViewEngine.ApplyLayoutAsync.
    /// </summary>
    public ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        return new ValueTask<string>(string.Empty);
    }

    /// <summary>
    /// Gets the section name to yield.
    /// </summary>
    public string SectionName => _sectionName;
}

/// <summary>
/// Represents a yield point with default fallback content.
/// If the named section is not provided by the content template, renders the default content.
/// Syntax: {{#yield-default "sectionName" }}default content{{/yield-default}}
/// </summary>
internal sealed class YieldDefaultNode : INode
{
    private readonly string _sectionName;
    private readonly List<INode> _defaultNodes;

    /// <summary>
    /// Initializes a new instance of the YieldDefaultNode class.
    /// </summary>
    /// <param name="sectionName">The section name to yield.</param>
    /// <param name="defaultNodes">Nodes to render if the section is not provided.</param>
    public YieldDefaultNode(string sectionName, List<INode> defaultNodes)
    {
        _sectionName = sectionName;
        _defaultNodes = defaultNodes;
    }

    /// <summary>
    /// Renders the default content.
    /// During layout application, this is only rendered if the section is not provided.
    /// </summary>
    public async ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        var result = new StringBuilder();
        foreach (var node in _defaultNodes)
        {
            result.Append(await node.RenderAsync(context, depth).ConfigureAwait(false));
        }
        return result.ToString();
    }

    /// <summary>
    /// Gets the section name.
    /// </summary>
    public string SectionName => _sectionName;
}

/// <summary>
/// Represents a helper call node (currently unused, reserved for future use).
/// Helpers are currently processed inline by VariableNode.
/// </summary>
internal sealed class HelperNode : INode
{
    private readonly string _helperName;
    private readonly List<string> _arguments;

    /// <summary>
    /// Initializes a new instance of the HelperNode class.
    /// </summary>
    /// <param name="helperName">The helper name.</param>
    /// <param name="arguments">The helper arguments.</param>
    public HelperNode(string helperName, List<string> arguments)
    {
        _helperName = helperName;
        _arguments = arguments;
    }

    /// <summary>
    /// Reserved for future use. Currently returns empty.
    /// </summary>
    public async ValueTask<string> RenderAsync(TemplateContext context, int depth = 0)
    {
        return await Task.FromResult(string.Empty).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the helper name.
    /// </summary>
    public string HelperName => _helperName;
    
    /// <summary>
    /// Gets the helper arguments.
    /// </summary>
    public IReadOnlyList<string> Arguments => _arguments;
}
