using System.Globalization;
using System.Text.RegularExpressions;
using Blobatar.Internal;

namespace Blobatar;

/// <summary>
/// Palette construction.
/// </summary>
/// <remarks>
/// <para>Hue is the only colour value the name controls. Lightness and chroma come from six authored
/// swatches (<see cref="BlobatarTone"/>), which is what makes every blobatar look like it came from the
/// same designer rather than from a random number generator.</para>
/// <para>Two contrast floors are enforced: the body clears the plate at 1.25:1 (weak on purpose, so pale
/// swatches stay pale) and the eyes clear the body at 4.5:1, the WCAG text floor, at every hue and tone.
/// Eye polarity follows the body, so the near-black tone gets light eyes rather than an invisible face.
/// A body is also kept visible against a dark host surface, since the plate is off by default.</para>
/// <para>Colours are resolved to hex rather than emitted as <c>oklch()</c>, and the contrast guarantee is
/// measured against real sRGB luminance rather than assumed from OKLab lightness.</para>
/// </remarks>
public static partial class ColorRamp
{
    /// <summary>Minimum contrast ratios as <c>(foreground slot, background slot, ratio)</c>, applied in order so the chain converges.</summary>
    public static IReadOnlyList<(string Foreground, string Background, double Ratio)> Floors { get; } =
    [
        ("head", "bg", 1.25),
        ("eye", "head", 4.5),
    ];

    /// <summary>The darkest host surface a plate-less body is expected to land on (about <c>#0a0a0b</c>), and the ratio it must clear against it.</summary>
    private static readonly Oklch DarkSurface = new(0.145, 0, 0);
    private const double SurfaceFloor = 1.5;

    /// <summary>A hair over the 4.5:1 the eye floor asks for, so byte quantization of the hex cannot land under it.</summary>
    private const double TintFloor = 4.55;

    /// <summary>The tone set: cumulative upper edges, so pale and mid tones dominate and the near-black body stays rare.</summary>
    private static readonly (double Edge, double L, double C)[] Tones =
    [
        (0.2, 0.86, 0.085), // pastel
        (0.36, 0.9, 0.028), // pale neutral
        (0.62, 0.73, 0.135), // mid
        (0.8, 0.62, 0.165), // deep
        (0.93, 0.87, 0.16), // bright
        (1.0, 0.34, 0.035), // ink: dark, but not darker than a dark host surface
    ];

    /// <summary>Resolves the palette for a hue and tone to hex colours.</summary>
    /// <param name="hue">Hue in degrees.</param>
    /// <param name="enforceContrast">Whether to run the contrast floors.</param>
    /// <param name="tone">Position in the tone set, 0 to 1. An exact 1 sits on the top edge and renders the first swatch.</param>
    /// <param name="backgroundColor">
    /// A caller's plate colour. A <c>#rgb</c>/<c>#rrggbb</c> value replaces the authored plate before the floors
    /// run, so the body is pushed off the plate actually drawn; any other string is painted as given and moves nothing.
    /// </param>
    public static BlobatarPalette Palette(double hue, bool enforceContrast = true, double tone = 0, string? backgroundColor = null)
    {
        var r = Ramp(hue, enforceContrast, tone, backgroundColor);
        return new BlobatarPalette(
            string.IsNullOrEmpty(backgroundColor) ? ToHex(r.Background) : backgroundColor,
            ToHex(r.Head),
            ToHex(r.Eye));
    }

    /// <summary>The palette in OKLCh, before hex encoding. See <see cref="Palette"/> for the parameters.</summary>
    public static OklchRamp Ramp(double hue, bool enforceContrast = true, double tone = 0, string? backgroundColor = null)
    {
        var t = ToneAt(tone);
        var head = EnsureContrast(new Oklch(t.L, t.C, hue), DarkSurface, SurfaceFloor);
        var bg = new Oklch(0.965, 0.01, hue);
        // Polarity follows the body: dark eyes on a light body, light eyes on a dark one.
        var eye = head.L >= 0.5 ? new Oklch(0.17, 0.02, hue) : new Oklch(0.97, 0.012, hue);

        if (!string.IsNullOrEmpty(backgroundColor) && Plate(backgroundColor) is { } plate)
        {
            bg = plate;
        }

        if (enforceContrast)
        {
            head = EnsureContrast(head, bg, 1.25);
            eye = EnsureContrast(eye, head, 4.5);
        }

        return new OklchRamp(bg, head, eye);
    }

    /// <summary>WCAG contrast ratio between two colours, measured on their in-gamut sRGB luminance.</summary>
    public static double Contrast(Oklch a, Oklch b)
    {
        var x = Luminance(a);
        var y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    /// <summary>
    /// Pushes <paramref name="fg"/>'s lightness away from <paramref name="bg"/> until the pair clears
    /// <paramref name="min"/>. Walks in the direction it is already leaning first, so a dark ink on a light
    /// head gets darker rather than flipping to light; if that direction runs out of range it tries the other
    /// way before giving up at pure black or white.
    /// </summary>
    public static Oklch EnsureContrast(Oklch fg, Oklch bg, double min)
    {
        if (Contrast(fg, bg) >= min)
        {
            return fg;
        }

        var lean = fg.L >= bg.L ? 1 : -1;
        foreach (var dir in new[] { lean, -lean })
        {
            var probe = fg;
            for (var i = 0; i < 60; i++)
            {
                probe = probe with { L = Math.Min(1, Math.Max(0, probe.L + dir * 0.02)) };
                if (Contrast(probe, bg) >= min)
                {
                    return probe;
                }

                if (probe.L == 0 || probe.L == 1)
                {
                    break;
                }
            }
        }

        // Unreachable for the authored ramps, but a palette override could get here.
        var black = fg with { L = 0, C = 0 };
        var white = fg with { L = 1, C = 0 };
        return Contrast(black, bg) >= Contrast(white, bg) ? black : white;
    }

    /// <summary>Encodes a colour as <c>#rrggbb</c>, reducing chroma if it falls outside the sRGB gamut.</summary>
    public static string ToHex(Oklch color)
    {
        var (r, g, b) = Resolve(color);
        return "#" + Channel(r) + Channel(g) + Channel(b);

        static string Channel(double v)
        {
            var s = v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - 0.055;
            return ((int)JsMath.Round(s * 255)).ToString("x2", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Decodes a <c>#rrggbb</c> colour to OKLCh.</summary>
    public static Oklch FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        var n = int.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var r = Decode((n >> 16) & 255);
        var g = Decode((n >> 8) & 255);
        var b = Decode(n & 255);

        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        var a = 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s;
        var bb = 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s;

        return new Oklch(
            0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s,
            JsMath.Hypot(a, bb),
            (Math.Atan2(bb, a) * 180) / Math.PI);

        static double Decode(int v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
    }

    /// <summary>Blends two colours in OKLab, the way <c>color-mix(in oklab, a, b t)</c> does.</summary>
    internal static Oklch Mix(Oklch a, Oklch b, double t)
    {
        var ax = a.C * Math.Cos(Rad(a.H));
        var ay = a.C * Math.Sin(Rad(a.H));
        var bx = b.C * Math.Cos(Rad(b.H));
        var by = b.C * Math.Sin(Rad(b.H));
        var x = ax + (bx - ax) * t;
        var y = ay + (by - ay) * t;
        return new Oklch(
            a.L + (b.L - a.L) * t,
            JsMath.Hypot(x, y),
            (Math.Atan2(y, x) * 180) / Math.PI);

        static double Rad(double v) => (v * Math.PI) / 180;
    }

    /// <summary><see cref="Mix"/> between two serialized colours, serialized.</summary>
    internal static string MixHex(string a, string b, double t) => ToHex(Mix(FromHex(a), FromHex(b), t));

    /// <summary>
    /// The sRGB blend CSS's <c>transition: fill</c> runs, byte by byte. The other blend, deliberately: this is
    /// how a fill travels between two finished colours, where <see cref="MixHex"/> is where a tint lands.
    /// </summary>
    internal static string FadeHex(string a, string b, double t)
    {
        var sb = new System.Text.StringBuilder("#");
        for (var i = 1; i < 7; i += 2)
        {
            var from = int.Parse(a.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var to = int.Parse(b.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var v = (int)JsMath.Round(from + (to - from) * t);
            sb.Append(v.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    /// <summary>
    /// The palette a tinting pose heads toward, given the one it is tinting from.
    /// </summary>
    /// <remarks>
    /// Derived per name rather than authored once, because the eye flips between near-black and near-white
    /// with the body's lightness and no fixed red clears 4.5:1 against both. The body meets its target only
    /// <see cref="Tint.Pull"/> of the way so the tone set survives the trip, and the eye endpoint is pushed
    /// until every point along the mix clears the floor, not merely both ends.
    /// </remarks>
    internal static (string Head, string Eye) Tinted(string head, string eye, Tint t)
    {
        var baseHead = FromHex(head);
        var baseEye = FromHex(eye);

        var hotHead = new Oklch(
            baseHead.L + (t.L - baseHead.L) * t.Pull,
            Math.Max(baseHead.C, t.C),
            t.H);
        hotHead = EnsureContrast(hotHead, DarkSurface, SurfaceFloor);

        var hotEye = EnsureContrast(baseEye, hotHead, TintFloor);

        var dir = hotEye.L >= hotHead.L ? 1 : -1;
        var headHex = ToHex(hotHead);
        for (var pass = 0; pass < 40; pass++)
        {
            var eyeHex = ToHex(hotEye);
            var worst = double.PositiveInfinity;
            for (var i = 0; i <= 10; i++)
            {
                var mixT = i / 10.0;
                worst = Math.Min(
                    worst,
                    Contrast(
                        FromHex(MixHex(eye, eyeHex, mixT)),
                        FromHex(MixHex(head, headHex, mixT))));
            }

            if (worst >= TintFloor)
            {
                return (headHex, eyeHex);
            }

            var l = Math.Min(1, Math.Max(0, hotEye.L + dir * 0.02));
            if (l == hotEye.L)
            {
                return (headHex, eyeHex);
            }

            hotEye = hotEye with { L = l };
        }

        return (headHex, ToHex(hotEye));
    }

    private static (double L, double C) ToneAt(double v)
    {
        foreach (var (edge, l, c) in Tones)
        {
            if (v < edge)
            {
                return (l, c);
            }
        }

        return (Tones[0].L, Tones[0].C);
    }

    /// <summary>
    /// A caller's <c>#rgb</c>/<c>#rrggbb</c> plate as the neutral grey of the same WCAG luminance, or
    /// <see langword="null"/> for a colour that cannot be measured here (a named colour, a <c>var()</c>).
    /// Grey, because luminance is all the floors read, and an achromatic OKLCh colour has luminance exactly <c>L³</c>.
    /// </summary>
    private static Oklch? Plate(string color)
    {
        var hex = ShortHex().Replace(color, "#$1$1$2$2$3$3");
        if (!LongHex().IsMatch(hex))
        {
            return null;
        }

        double y = 0;
        ReadOnlySpan<double> weights = [0.2126, 0.7152, 0.0722];
        for (var i = 0; i < 3; i++)
        {
            var s = int.Parse(hex.AsSpan(1 + 2 * i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            y += weights[i] * (s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4));
        }

        return new Oklch(Math.Cbrt(y), 0, 0);
    }

    /// <summary>OKLCh to linear-light sRGB. Components may fall outside <c>[0, 1]</c> (out of gamut).</summary>
    private static (double R, double G, double B) ToLinear(Oklch color)
    {
        var r = (color.H * Math.PI) / 180;
        var a = color.C * Math.Cos(r);
        var b = color.C * Math.Sin(r);

        var l_ = color.L + 0.3963377774 * a + 0.2158037573 * b;
        var m_ = color.L - 0.1055613458 * a - 0.0638541728 * b;
        var s_ = color.L - 0.0894841775 * a - 1.291485548 * b;

        var l = l_ * l_ * l_;
        var m = m_ * m_ * m_;
        var s = s_ * s_ * s_;

        return (
            4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s);
    }

    private static bool InGamut((double R, double G, double B) rgb) =>
        rgb.R >= -1e-4 && rgb.R <= 1 + 1e-4
        && rgb.G >= -1e-4 && rgb.G <= 1 + 1e-4
        && rgb.B >= -1e-4 && rgb.B <= 1 + 1e-4;

    /// <summary>Resolves to in-gamut linear sRGB, reducing chroma if needed: lowering it desaturates, where clipping channels shifts hue.</summary>
    private static (double R, double G, double B) Resolve(Oklch color)
    {
        var rgb = ToLinear(color);
        if (!InGamut(rgb))
        {
            double lo = 0;
            var hi = color.C;
            for (var i = 0; i < 12; i++)
            {
                var mid = (lo + hi) / 2;
                if (InGamut(ToLinear(color with { C = mid })))
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            rgb = ToLinear(color with { C = lo });
        }

        return (Clamp01(rgb.R), Clamp01(rgb.G), Clamp01(rgb.B));

        static double Clamp01(double v) => Math.Min(1, Math.Max(0, v));
    }

    /// <summary>WCAG relative luminance. The values out of <see cref="Resolve"/> are already linear-light.</summary>
    private static double Luminance(Oklch color)
    {
        var (r, g, b) = Resolve(color);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    // JavaScript's `\w` is ASCII-only, so it is spelled out rather than written as .NET's Unicode-aware `\w`.
    [GeneratedRegex("^#([A-Za-z0-9_])([A-Za-z0-9_])([A-Za-z0-9_])$")]
    private static partial Regex ShortHex();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex LongHex();
}
