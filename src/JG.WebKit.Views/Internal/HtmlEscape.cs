namespace JG.WebKit.Views.Internal;

/// <summary>
/// Provides high-performance HTML entity escaping to prevent XSS attacks.
/// Implements a zero-allocation fast path for strings that don't need escaping.
/// </summary>
internal static class HtmlEscape
{
    /// <summary>
    /// Escapes HTML special characters to their entity equivalents.
    /// Fast path: Returns the input unchanged if no escaping is needed (zero allocations).
    /// Slow path: Creates a new string with escaped entities.
    /// </summary>
    /// <param name="input">The string to escape.</param>
    /// <returns>
    /// The HTML-safe string with the following replacements:
    /// &amp; -> &amp;amp;
    /// &lt; -> &amp;lt;
    /// &gt; -> &amp;gt;
    /// " -> &amp;quot;
    /// ' -> &amp;#39;
    /// </returns>
    public static string Escape(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        // Fast path: check if escaping is needed
        var span = input.AsSpan();
        var hasEscapable = false;

        foreach (var ch in span)
        {
            if (ch == '&' || ch == '<' || ch == '>' || ch == '"' || ch == '\'')
            {
                hasEscapable = true;
                break;
            }
        }

        if (!hasEscapable)
            return input;

        // Slow path: allocate and escape
        var sb = new StringBuilder(input.Length * 2);

        foreach (var ch in span)
        {
            switch (ch)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&#x27;");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }
}
