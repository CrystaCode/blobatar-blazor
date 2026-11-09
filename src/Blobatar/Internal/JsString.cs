using System.Globalization;
using System.Text;

namespace Blobatar.Internal;

/// <summary>
/// The JavaScript string semantics seed normalization depends on.
/// </summary>
/// <remarks>
/// Upstream's <c>normalizeSeed</c> is <c>seed.normalize("NFC").trim().toLowerCase()</c>. Each of the
/// three differs slightly from its .NET namesake: JavaScript's <c>trim</c> strips U+FEFF and not
/// U+0085, and its <c>toLowerCase</c> applies the two context-free parts of Unicode's full case
/// mapping (<c>İ</c> and the final sigma) where .NET applies only the simple mapping. The same name
/// has to hash identically on both sides, so the JavaScript behaviour is reproduced here.
/// Code points are written as numeric constants so that none of them can be mistaken for, or
/// mangled into, the invisible character it names.
/// </remarks>
internal static class JsString
{
    private const char NoBreakSpace = (char)0x00A0;
    private const char OghamSpaceMark = (char)0x1680;
    private const char EnQuad = (char)0x2000;
    private const char HairSpace = (char)0x200A;
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;
    private const char NarrowNoBreakSpace = (char)0x202F;
    private const char MediumMathematicalSpace = (char)0x205F;
    private const char IdeographicSpace = (char)0x3000;
    private const char ByteOrderMark = (char)0xFEFF;

    private const char CapitalIWithDotAbove = (char)0x0130;
    private const char CombiningDotAbove = (char)0x0307;
    private const char CapitalSigma = (char)0x03A3;
    private const char FinalSigma = (char)0x03C2;

    private const char SoftHyphen = (char)0x00AD;
    private const char MiddleDot = (char)0x00B7;
    private const char LeftSingleQuotationMark = (char)0x2018;
    private const char RightSingleQuotationMark = (char)0x2019;
    private const char OneDotLeader = (char)0x2024;

    /// <summary>JavaScript's <c>WhiteSpace</c> and <c>LineTerminator</c> productions, which is what <c>String.prototype.trim</c> removes.</summary>
    public static bool IsWhiteSpace(char c) => c switch
    {
        '\t' or '\n' or '\v' or '\f' or '\r' or ' ' => true,
        NoBreakSpace or OghamSpaceMark or LineSeparator or ParagraphSeparator => true,
        NarrowNoBreakSpace or MediumMathematicalSpace or IdeographicSpace or ByteOrderMark => true,
        >= EnQuad and <= HairSpace => true,
        _ => false,
    };

    /// <summary>JavaScript <c>String.prototype.trim</c>.</summary>
    public static string Trim(string s)
    {
        var start = 0;
        var end = s.Length;
        while (start < end && IsWhiteSpace(s[start]))
        {
            start++;
        }

        while (end > start && IsWhiteSpace(s[end - 1]))
        {
            end--;
        }

        return start == 0 && end == s.Length ? s : s.Substring(start, end - start);
    }

    /// <summary>
    /// JavaScript <c>String.prototype.normalize("NFC")</c>.
    /// </summary>
    /// <remarks>
    /// Under globalization-invariant mode (which some Blazor WebAssembly apps enable to shrink their
    /// download) the runtime may refuse to normalize non-ASCII text. The seed is then hashed
    /// unnormalized, which only matters for names written in a decomposed form.
    /// </remarks>
    public static string NormalizeNfc(string s)
    {
        try
        {
            return s.IsNormalized(NormalizationForm.FormC) ? s : s.Normalize(NormalizationForm.FormC);
        }
        catch (PlatformNotSupportedException)
        {
            return s;
        }
    }

    /// <summary>
    /// JavaScript <c>String.prototype.toLowerCase</c>: the Unicode full lowercase mapping.
    /// </summary>
    /// <remarks>
    /// .NET lowercases with the simple mapping. The full mapping differs in two language-independent
    /// places, both of which a pasted name can hit: U+0130 LATIN CAPITAL LETTER I WITH DOT ABOVE becomes
    /// <c>i</c> followed by U+0307, and a capital sigma preceded by a cased letter and not followed by one
    /// takes its final form <c>ς</c>. Those two are applied first; everything else is
    /// <see cref="string.ToLowerInvariant"/>.
    /// </remarks>
    public static string ToLowerCase(string s)
    {
        var needsWork = false;
        foreach (var c in s)
        {
            if (c is CapitalIWithDotAbove or CapitalSigma)
            {
                needsWork = true;
                break;
            }
        }

        if (!needsWork)
        {
#pragma warning disable CA1308 // The lowercase mapping is the specified behaviour, not a normalization choice.
            return s.ToLowerInvariant();
#pragma warning restore CA1308
        }

        var sb = new StringBuilder(s.Length + 4);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == CapitalIWithDotAbove)
            {
                sb.Append('i').Append(CombiningDotAbove);
            }
            else if (c == CapitalSigma && PrecededByCased(s, i) && !FollowedByCased(s, i))
            {
                sb.Append(FinalSigma);
            }
            else
            {
                sb.Append(c);
            }
        }

#pragma warning disable CA1308
        return sb.ToString().ToLowerInvariant();
#pragma warning restore CA1308
    }

    private static bool PrecededByCased(string s, int index)
    {
        for (var j = index - 1; j >= 0; j--)
        {
            if (IsCased(s[j]))
            {
                return true;
            }

            if (!IsCaseIgnorable(s[j]))
            {
                return false;
            }
        }

        return false;
    }

    private static bool FollowedByCased(string s, int index)
    {
        for (var j = index + 1; j < s.Length; j++)
        {
            if (IsCased(s[j]))
            {
                return true;
            }

            if (!IsCaseIgnorable(s[j]))
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>Unicode <c>Cased</c>, approximated by the three letter-case categories.</summary>
    private static bool IsCased(char c) =>
        CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter;

    /// <summary>Unicode <c>Case_Ignorable</c>, approximated by its defining categories plus the apostrophes and dots the property adds.</summary>
    private static bool IsCaseIgnorable(char c) =>
        c is '\'' or '.' or ':' or MiddleDot or LeftSingleQuotationMark or RightSingleQuotationMark or OneDotLeader or SoftHyphen
        || CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.Format
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.ModifierSymbol;
}
