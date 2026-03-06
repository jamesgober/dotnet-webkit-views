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

/// <summary>
/// Built-in default value helper.
/// Returns the first argument if non-null and non-empty, otherwise returns the fallback.
/// Usage: {{ default title "Untitled Page" }}
/// </summary>
public sealed class DefaultHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "default";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return string.Empty;

        var value = arguments[0];
        var fallback = arguments.Length > 1 ? arguments[1]?.ToString() ?? string.Empty : string.Empty;

        // null → return fallback
        if (value == null)
            return fallback;

        // Convert to string
        var str = value.ToString() ?? string.Empty;

        // empty or whitespace → return fallback
        if (string.IsNullOrWhiteSpace(str))
            return fallback;

        // Non-empty value → return it
        return str;
    }
}

/// <summary>
/// Built-in conditional value helper (ternary operator).
/// Returns second argument if first is truthy, otherwise returns third argument.
/// Usage: {{ ifval user.isAdmin "Administrator" "User" }}
/// </summary>
public sealed class IfValHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "ifval";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 3)
            return string.Empty;

        var condition = arguments[0];
        var trueValue = arguments[1]?.ToString() ?? string.Empty;
        var falseValue = arguments[2]?.ToString() ?? string.Empty;

        return IsTruthy(condition) ? trueValue : falseValue;
    }

    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            long l => l != 0,
            double d => d != 0.0,
            float f => f != 0.0f,
            decimal m => m != 0m,
            _ => true
        };
    }
}

/// <summary>
/// Built-in string concatenation helper.
/// Concatenates all arguments into a single string.
/// Usage: {{ concat firstName " " lastName }}
/// </summary>
public sealed class ConcatHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "concat";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var arg in arguments)
        {
            sb.Append(arg?.ToString() ?? string.Empty);
        }

        return sb.ToString();
    }
}

/// <summary>
/// Built-in string replacement helper.
/// Replaces all occurrences of the search string with the replacement string.
/// Usage: {{ replace title "-" " " }}
/// </summary>
public sealed class ReplaceHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "replace";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 3)
            return string.Empty;

        var input = arguments[0]?.ToString() ?? string.Empty;
        var search = arguments[1]?.ToString() ?? string.Empty;
        var replacement = arguments[2]?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(search))
            return input;

        return input.Replace(search, replacement);
    }
}

/// <summary>
/// Built-in collection count helper.
/// Returns the count of items in a collection.
/// Usage: {{ count items }}
/// </summary>
public sealed class CountHelper : ITemplateHelper
{
    /// <inheritdoc />
    public string Name => "count";

    /// <inheritdoc />
    public string Execute(object?[] arguments, TemplateContext context)
    {
        if (arguments.Length < 1)
            return "0";

        var collection = arguments[0];

        if (collection == null)
            return "0";

        if (collection is string)
            return "0"; // Strings are not counted as collections

        if (collection is System.Collections.ICollection col)
            return col.Count.ToString(CultureInfo.InvariantCulture);

        if (collection is System.Collections.IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable)
                count++;
            return count.ToString(CultureInfo.InvariantCulture);
        }

        return "0";
    }
}
