namespace JG.WebKit.Views;

/// <summary>
/// Configures the template engine behavior.
/// </summary>
public sealed class ViewEngineOptions
{
    /// <summary>
    /// Gets or sets the directory containing template files.
    /// </summary>
    public string TemplatePath { get; set; } = "templates";

    /// <summary>
    /// Gets or sets the directory containing layout files.
    /// </summary>
    public string LayoutPath { get; set; } = "templates/layouts";

    /// <summary>
    /// Gets or sets the directory containing partial files.
    /// </summary>
    public string PartialPath { get; set; } = "templates/partials";

    /// <summary>
    /// Gets the asset path configuration.
    /// </summary>
    public AssetOptions Assets { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to cache compiled templates.
    /// </summary>
    public bool CacheCompiledTemplates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable hot-reload in development mode.
    /// </summary>
    public bool EnableHotReload { get; set; }

    /// <summary>
    /// Gets or sets the default layout name used when no layout is specified.
    /// </summary>
    public string DefaultLayout { get; set; } = "main";

    /// <summary>
    /// Gets or sets the file extension for template files.
    /// </summary>
    public string TemplateExtension { get; set; } = ".tpl";

    /// <summary>
    /// Gets or sets a value indicating whether to allow raw unescaped output ({{{ }}}).
    /// </summary>
    public bool AllowRawOutput { get; set; }

    /// <summary>
    /// Gets or sets the maximum include depth to prevent infinite recursion.
    /// </summary>
    public int MaxIncludeDepth { get; set; } = 10;
}

/// <summary>
/// Configures asset path resolution for templates.
/// </summary>
public sealed class AssetOptions
{
    /// <summary>
    /// Gets or sets the base path for all assets.
    /// </summary>
    public string BasePath { get; set; } = "/assets";

    /// <summary>
    /// Gets or sets the path for image assets.
    /// </summary>
    public string Images { get; set; } = "/assets/images";

    /// <summary>
    /// Gets or sets the path for stylesheet assets.
    /// </summary>
    public string Styles { get; set; } = "/assets/css";

    /// <summary>
    /// Gets or sets the path for script assets.
    /// </summary>
    public string Scripts { get; set; } = "/assets/js";

    /// <summary>
    /// Gets or sets the path for font assets.
    /// </summary>
    public string Fonts { get; set; } = "/assets/fonts";

    /// <summary>
    /// Gets or sets the path for media assets.
    /// </summary>
    public string Media { get; set; } = "/assets/media";

    /// <summary>
    /// Gets or sets the CDN base URL, prepended to all asset URLs if set.
    /// </summary>
    public string? CdnBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to append a version hash for cache busting.
    /// </summary>
    public bool AppendVersionHash { get; set; }
}
