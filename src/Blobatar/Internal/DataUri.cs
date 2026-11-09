using System.Globalization;
using System.Text;

namespace Blobatar.Internal;

/// <summary>
/// A <c>data:</c> URI suitable for <c>&lt;img src&gt;</c> or <c>background-image</c>.
/// </summary>
/// <remarks>
/// Percent-encoded rather than base64: base64 inflates payloads by a third, while SVG markup is mostly
/// characters that survive percent-encoding untouched. Only the characters that actually break inside an
/// attribute are escaped, and double quotes become single quotes so the markup can sit inside a
/// double-quoted attribute.
/// </remarks>
internal static class DataUri
{
    public static string Encode(string svg)
    {
        var sb = new StringBuilder(svg.Length + 64);
        sb.Append("data:image/svg+xml,");
        var pendingSpace = false;
        foreach (var raw in svg)
        {
            var c = raw == '"' ? '\'' : raw;
            if (JsString.IsWhiteSpace(c))
            {
                // Runs of whitespace collapse to one space, as upstream's `.replace(/\s+/g, " ")` does.
                pendingSpace = true;
                continue;
            }

            if (pendingSpace)
            {
                sb.Append(' ');
                pendingSpace = false;
            }

            if (c is '%' or '#' or '<' or '>' or '{' or '}' or '|' or '\\' or '^' or '[' or ']' or '`')
            {
                sb.Append('%').Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(c);
            }
        }

        if (pendingSpace)
        {
            sb.Append(' ');
        }

        return sb.ToString();
    }
}
