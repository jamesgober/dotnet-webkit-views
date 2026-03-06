namespace JG.WebKit.Views;

/// <summary>
/// Contains the rendering context for a template, including data and globals.
/// </summary>
public sealed class TemplateContext
{
    /// <summary>
    /// Gets the template data model.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Data { get; }

    /// <summary>
    /// Gets site-wide globals available to all templates.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Globals { get; }

    /// <summary>
    /// Gets the current HTTP context, if applicable.
    /// </summary>
    public HttpContext? HttpContext { get; }

    /// <summary>
    /// Initializes a new instance of the TemplateContext class.
    /// </summary>
    /// <param name="data">The template data model.</param>
    /// <param name="globals">Site-wide globals available to all templates.</param>
    /// <param name="httpContext">The current HTTP context, if applicable.</param>
    public TemplateContext(
        IReadOnlyDictionary<string, object?> data,
        IReadOnlyDictionary<string, object?>? globals = null,
        HttpContext? httpContext = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Globals = globals ?? new Dictionary<string, object?>();
        HttpContext = httpContext;
    }
}
