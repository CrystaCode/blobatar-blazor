using System.Globalization;

namespace Blobatar.Internal;

/// <summary>
/// The JavaScript numeric semantics this port reproduces exactly.
/// </summary>
/// <remarks>
/// The upstream library emits numbers through JavaScript's <c>Math.round</c>, <c>Math.hypot</c> and
/// <c>String(number)</c>. Each differs from the nearest .NET call in ways that change bytes of the
/// rendered SVG (<see cref="Math.Round(double)"/> rounds half to even, <c>(-0.0).ToString()</c> is
/// <c>"-0"</c>), so the differences are isolated here and nowhere else.
/// </remarks>
internal static class JsMath
{
    /// <summary>JavaScript <c>Math.imul</c>: 32-bit multiplication, modulo 2^32.</summary>
    public static uint Imul(uint a, uint b) => unchecked(a * b);

    /// <summary>JavaScript <c>(h &lt;&lt; bits) | (h &gt;&gt;&gt; (32 - bits))</c> on a 32-bit value.</summary>
    public static uint RotateLeft(uint h, int bits) => (h << bits) | (h >> (32 - bits));

    /// <summary>
    /// JavaScript <c>Math.round</c>: rounds half toward positive infinity, so <c>2.5</c> is <c>3</c>
    /// and <c>-2.5</c> is <c>-2</c>, where <see cref="Math.Round(double)"/> gives <c>2</c> and <c>-2</c>.
    /// </summary>
    public static double Round(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v))
        {
            return v;
        }

        var floor = Math.Floor(v);
        return v - floor >= 0.5 ? floor + 1 : floor;
    }

    /// <summary>
    /// JavaScript <c>Math.hypot</c> as JavaScriptCore implements it: the arguments are scaled by the
    /// largest magnitude and summed with Kahan compensation before the square root.
    /// </summary>
    /// <remarks>
    /// Upstream's golden fixture was generated under Bun, whose engine is JavaScriptCore. A plain
    /// <c>Math.Sqrt(x * x + y * y)</c> can differ in the last bit, and that bit feeds the factor the eye
    /// cluster is scaled by to fit the face, so the engine's algorithm is reproduced rather than approximated.
    /// </remarks>
    public static double Hypot(double x, double y)
    {
        if (double.IsInfinity(x) || double.IsInfinity(y))
        {
            return double.PositiveInfinity;
        }

        if (double.IsNaN(x) || double.IsNaN(y))
        {
            return double.NaN;
        }

        var max = Math.Max(Math.Abs(x), Math.Abs(y));
        if (max == 0)
        {
            max = 1;
        }

        double sum = 0;
        double compensation = 0;
        Accumulate(x / max, ref sum, ref compensation);
        Accumulate(y / max, ref sum, ref compensation);
        return Math.Sqrt(sum) * max;

        static void Accumulate(double scaled, ref double sum, ref double compensation)
        {
            var summand = scaled * scaled - compensation;
            var preliminary = sum + summand;
            compensation = (preliminary - sum) - summand;
            sum = preliminary;
        }
    }

    /// <summary>
    /// JavaScript <c>String(number)</c>: the shortest digits that round-trip, plain decimal notation
    /// between 1e-7 and 1e21, and <c>"0"</c> for negative zero.
    /// </summary>
    public static string ToJsString(double v)
    {
        if (v == 0)
        {
            return "0";
        }

        if (double.IsNaN(v))
        {
            return "NaN";
        }

        if (double.IsInfinity(v))
        {
            return v > 0 ? "Infinity" : "-Infinity";
        }

        var s = v.ToString("R", CultureInfo.InvariantCulture);
        return s.Contains('E', StringComparison.Ordinal) ? Reformat(s) : s;
    }

    /// <summary>
    /// Rewrites a .NET <c>"R"</c> exponent form (<c>1.5E+15</c>, <c>1E-07</c>) into the form JavaScript
    /// would have produced for the same value. Never reached by the renderer, whose numbers are all
    /// between 0.001 and 1100, but kept so the helper is honest for every input.
    /// </summary>
    private static string Reformat(string s)
    {
        var negative = s[0] == '-';
        if (negative)
        {
            s = s[1..];
        }

        var e = s.IndexOf('E', StringComparison.Ordinal);
        var mantissa = s[..e];
        var exponent = int.Parse(s[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var digits = mantissa.Replace(".", "", StringComparison.Ordinal);
        var pointIndex = mantissa.IndexOf('.', StringComparison.Ordinal);
        var intDigits = (pointIndex < 0 ? mantissa.Length : pointIndex) + exponent;

        string body;
        if (exponent >= 21 || exponent <= -7)
        {
            var fraction = digits.Length > 1 ? "." + digits[1..] : string.Empty;
            body = digits[0] + fraction + "e" + (exponent >= 0 ? "+" : "-") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
        }
        else if (intDigits <= 0)
        {
            body = "0." + new string('0', -intDigits) + digits;
        }
        else if (intDigits >= digits.Length)
        {
            body = digits + new string('0', intDigits - digits.Length);
        }
        else
        {
            body = digits[..intDigits] + "." + digits[intDigits..];
        }

        return negative ? "-" + body : body;
    }
}
