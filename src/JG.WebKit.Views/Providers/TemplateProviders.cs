namespace JG.WebKit.Views.Providers;

using JG.WebKit.Views.Abstractions;

/// <summary>
/// Template provider that reads templates from the file system.
/// </summary>
public sealed class FileTemplateProvider : ITemplateProvider, IDisposable
{
    private readonly string _basePath;
    private readonly ViewEngineOptions _options;
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether this provider supports hot-reload notification.
    /// </summary>
    public bool SupportsHotReload => true;

    /// <summary>
    /// Initializes a new instance of the FileTemplateProvider class.
    /// </summary>
    /// <param name="basePath">The base directory for template files.</param>
    /// <param name="options">The view engine options.</param>
    public FileTemplateProvider(string basePath, ViewEngineOptions options)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets a template by path from the file system.
    /// </summary>
    /// <param name="path">The template path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The template source, or null if not found.</returns>
    public async ValueTask<TemplateSource?> GetTemplateAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        var fullPath = Path.Combine(_basePath, path + _options.TemplateExtension);
        fullPath = Path.GetFullPath(fullPath);

        var baseFull = Path.GetFullPath(_basePath);
        if (!fullPath.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!File.Exists(fullPath))
            return null;

        var content = await File.ReadAllTextAsync(fullPath, ct).ConfigureAwait(false);
        var lastModified = File.GetLastWriteTimeUtc(fullPath);

        return new TemplateSource
        {
            Content = content,
            Path = path,
            LastModified = new DateTimeOffset(lastModified)
        };
    }

    /// <summary>
    /// Enables hot-reload by watching for file changes.
    /// </summary>
    /// <param name="onChanged">Callback when a template file changes.</param>
    public void EnableHotReload(Action<string> onChanged)
    {
        if (_watcher != null)
            return;

        _watcher = new FileSystemWatcher(_basePath)
        {
            IncludeSubdirectories = true,
            Filter = $"*{_options.TemplateExtension}"
        };

        _watcher.Changed += (s, e) => onChanged(GetRelativePath(e.FullPath));
        _watcher.Created += (s, e) => onChanged(GetRelativePath(e.FullPath));
        _watcher.Deleted += (s, e) => onChanged(GetRelativePath(e.FullPath));
        _watcher.Renamed += (s, e) => onChanged(GetRelativePath(e.FullPath));

        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Disposes the file watcher.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _watcher?.Dispose();
        _disposed = true;
    }

    private string GetRelativePath(string fullPath)
    {
        var relative = Path.GetRelativePath(_basePath, fullPath);
        return relative.Replace(_options.TemplateExtension, string.Empty, StringComparison.Ordinal).Replace('\\', '/');
    }
}

/// <summary>
/// Template provider that stores templates in memory.
/// </summary>
public sealed class InMemoryTemplateProvider : ITemplateProvider
{
    private readonly Dictionary<string, TemplateSource> _templates = new();

    /// <summary>
    /// Gets a value indicating whether this provider supports hot-reload notification.
    /// </summary>
    public bool SupportsHotReload => false;

    /// <summary>
    /// Adds a template to the in-memory store.
    /// </summary>
    /// <param name="path">The template path.</param>
    /// <param name="content">The template content.</param>
    public void AddTemplate(string path, string content)
    {
        _templates[path] = new TemplateSource
        {
            Path = path,
            Content = content,
            LastModified = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Gets a template by path from memory.
    /// </summary>
    /// <param name="path">The template path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The template source, or null if not found.</returns>
    public ValueTask<TemplateSource?> GetTemplateAsync(string path, CancellationToken ct = default)
    {
        return new ValueTask<TemplateSource?>(_templates.TryGetValue(path, out var template) ? template : null);
    }
}
