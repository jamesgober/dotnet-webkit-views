namespace JG.WebKit.Views;

/// <summary>
/// Contains the content and metadata of a template source.
/// </summary>
public sealed class TemplateSource
{
    /// <summary>
    /// Gets the template content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the template path or identifier.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the last modified date of the template, if available.
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }
}
