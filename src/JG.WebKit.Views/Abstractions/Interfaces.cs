namespace JG.WebKit.Views.Abstractions;

/// <summary>
/// Provides template sources from various backends (file, database, memory, etc.).
/// </summary>
public interface ITemplateProvider
{
    /// <summary>
    /// Gets a template by path.
    /// </summary>
    /// <param name="path">The template path or identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The template source, or null if not found.</returns>
    ValueTask<TemplateSource?> GetTemplateAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Gets a value indicating whether this provider supports hot-reload notification.
    /// </summary>
    bool SupportsHotReload { get; }
}

/// <summary>
/// Represents a registered template helper function.
/// </summary>
public interface ITemplateHelper
{
    /// <summary>
    /// Gets the name of the helper as used in templates.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the helper with the given arguments.
    /// </summary>
    /// <param name="arguments">The arguments passed to the helper.</param>
    /// <param name="context">The template context.</param>
    /// <returns>The string output of the helper.</returns>
    string Execute(object?[] arguments, TemplateContext context);
}

/// <summary>
/// The main template rendering engine.
/// </summary>
public interface IViewEngine : IDisposable
{
    /// <summary>
    /// Renders a template by path with the given context.
    /// </summary>
    /// <param name="templatePath">The path to the template.</param>
    /// <param name="context">The template context with data and globals.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rendered template output.</returns>
    ValueTask<string> RenderAsync(string templatePath, TemplateContext context, CancellationToken ct = default);

    /// <summary>
    /// Renders a template string directly with the given context.
    /// </summary>
    /// <param name="templateContent">The template content as a string.</param>
    /// <param name="context">The template context with data and globals.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rendered template output.</returns>
    ValueTask<string> RenderStringAsync(string templateContent, TemplateContext context, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the compiled template cache.
    /// </summary>
    /// <param name="templatePath">The template path to invalidate, or null to clear the entire cache.</param>
    void InvalidateCache(string? templatePath = null);
}
