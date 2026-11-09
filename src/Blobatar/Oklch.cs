namespace Blobatar;

/// <summary>
/// A colour in OKLCh: perceptual lightness, chroma and hue.
/// </summary>
/// <param name="L">Lightness, 0 to 1.</param>
/// <param name="C">Chroma, 0 upward (about 0.37 is the most vivid sRGB can hold).</param>
/// <param name="H">Hue in degrees.</param>
/// <remarks>
/// The palette is authored in this space and resolved to sRGB hex at render time, because server-side
/// rasterizers largely do not understand <c>oklch()</c> and blobatars get rasterized server-side constantly.
/// </remarks>
public readonly record struct Oklch(double L, double C, double H);

/// <summary>The three palette slots as OKLCh colours, before hex encoding.</summary>
/// <param name="Background">The plate.</param>
/// <param name="Head">The body.</param>
/// <param name="Eye">The eyes.</param>
public sealed record OklchRamp(Oklch Background, Oklch Head, Oklch Eye);
