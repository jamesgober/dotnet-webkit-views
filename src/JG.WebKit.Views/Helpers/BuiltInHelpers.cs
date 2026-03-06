namespace JG.WebKit.Views.Helpers;

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using JG.WebKit.Views.Abstractions;

/// <summary>
/// Built-in date formatting helper.
/// </summary>
public sealed class DateHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "date";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 2)
            return string.Empty;

        if (arguments[0] is not DateTimeOffset && arguments[0] is not DateTime)
            return string.Empty;

        var format = arguments[1]?.ToString() ?? "O";

        var value = arguments[0] switch
        {
            DateTimeOffset d => d.DateTime,
            DateTime d => d,
            _ => DateTime.MinValue
        };

        try
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Built-in string truncation helper.
/// </summary>
public sealed class TruncateHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "truncate";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 2)
            return string.Empty;

        var text = arguments[0]?.ToString() ?? string.Empty;
        if (!int.TryParse(arguments[1]?.ToString(), out var maxLength))
            return text;

        if (text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }
}

/// <summary>
/// Built-in uppercase helper.
/// </summary>
public sealed class UppercaseHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "uppercase";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        return (arguments[0]?.ToString() ?? string.Empty).ToUpperInvariant();
    }
}

/// <summary>
/// Built-in lowercase helper.
/// </summary>
public sealed class LowercaseHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "lowercase";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        return (arguments[0]?.ToString() ?? string.Empty).ToLowerInvariant();
    }
}

/// <summary>
/// Built-in JSON serialization helper.
/// </summary>
public sealed class JsonHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "json";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return "null";

        return JsonSerializer.Serialize(arguments[0]);
    }
}

/// <summary>
/// Built-in asset path helper.
/// </summary>
public sealed class AssetHelper : ITemplateHelper
{
    private readonly ViewEngineOptions _options;

    /// <inheritdoc />
    public string Name => "asset";

    /// <summary>
    /// Initializes a new instance of the AssetHelper class.
    /// </summary>
    /// <param name="options">The view engine options.</param>
    public AssetHelper(ViewEngineOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        var path = arguments[0]?.ToString() ?? string.Empty;
        var basePath = _options.Assets.BasePath;
        var cdnUrl = _options.Assets.CdnBaseUrl;

        var fullPath = $"{basePath}/{path}";
        if (!string.IsNullOrEmpty(cdnUrl))
            fullPath = $"{cdnUrl}{fullPath}";

        if (_options.Assets.AppendVersionHash)
            fullPath += "?v=" + GetVersionHash(fullPath);

        return fullPath;
    }

    private static string GetVersionHash(string path)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hash)[..8].ToLower(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Built-in image path helper.
/// </summary>
public sealed class ImageHelper : ITemplateHelper
{
    private readonly ViewEngineOptions _options;

    /// <inheritdoc />
    public string Name => "image";

    /// <summary>
    /// Initializes a new instance of the ImageHelper class.
    /// </summary>
    /// <param name="options">The view engine options.</param>
    public ImageHelper(ViewEngineOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        var path = arguments[0]?.ToString() ?? string.Empty;
        var imagePath = _options.Assets.Images;
        var cdnUrl = _options.Assets.CdnBaseUrl;

        var fullPath = $"{imagePath}/{path}";
        if (!string.IsNullOrEmpty(cdnUrl))
            fullPath = $"{cdnUrl}{fullPath}";

        if (_options.Assets.AppendVersionHash)
            fullPath += "?v=" + GetVersionHash(fullPath);

        return fullPath;
    }

    private static string GetVersionHash(string path)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hash)[..8].ToLower(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Built-in script path helper.
/// </summary>
public sealed class ScriptHelper : ITemplateHelper
{
    private readonly ViewEngineOptions _options;

    /// <inheritdoc />
    public string Name => "script";

    /// <summary>
    /// Initializes a new instance of the ScriptHelper class.
    /// </summary>
    /// <param name="options">The view engine options.</param>
    public ScriptHelper(ViewEngineOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        var path = arguments[0]?.ToString() ?? string.Empty;
        var scriptPath = _options.Assets.Scripts;
        var cdnUrl = _options.Assets.CdnBaseUrl;

        var fullPath = $"{scriptPath}/{path}";
        if (!string.IsNullOrEmpty(cdnUrl))
            fullPath = $"{cdnUrl}{fullPath}";

        if (_options.Assets.AppendVersionHash)
            fullPath += "?v=" + GetVersionHash(fullPath);

        return fullPath;
    }

    private static string GetVersionHash(string path)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hash)[..8].ToLower(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Built-in font path helper.
/// </summary>
public sealed class FontHelper : ITemplateHelper
{
    private readonly ViewEngineOptions _options;

    /// <inheritdoc />
    public string Name => "font";

    /// <summary>
    /// Initializes a new instance of the FontHelper class.
    /// </summary>
    /// <param name="options">The view engine options.</param>
    public FontHelper(ViewEngineOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        var path = arguments[0]?.ToString() ?? string.Empty;
        var fontPath = _options.Assets.Fonts;
        var cdnUrl = _options.Assets.CdnBaseUrl;

        var fullPath = $"{fontPath}/{path}";
        if (!string.IsNullOrEmpty(cdnUrl))
            fullPath = $"{cdnUrl}{fullPath}";

        if (_options.Assets.AppendVersionHash)
            fullPath += "?v=" + GetVersionHash(fullPath);

        return fullPath;
    }

    private static string GetVersionHash(string path)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hash)[..8].ToLower(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Built-in media path helper.
/// </summary>
public sealed class MediaHelper : ITemplateHelper
{
    private readonly ViewEngineOptions _options;

    /// <inheritdoc />
    public string Name => "media";

    /// <summary>
    /// Initializes a new instance of the MediaHelper class.
    /// </summary>
    /// <param name="options">The view engine options.</param>
    public MediaHelper(ViewEngineOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        var path = arguments[0]?.ToString() ?? string.Empty;
        var mediaPath = _options.Assets.Media;
        var cdnUrl = _options.Assets.CdnBaseUrl;

        var fullPath = $"{mediaPath}/{path}";
        if (!string.IsNullOrEmpty(cdnUrl))
            fullPath = $"{cdnUrl}{fullPath}";

        if (_options.Assets.AppendVersionHash)
            fullPath += "?v=" + GetVersionHash(fullPath);

        return fullPath;
    }

    private static string GetVersionHash(string path)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hash)[..8].ToLower(CultureInfo.InvariantCulture);
    }
}
