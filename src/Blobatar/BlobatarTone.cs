namespace Blobatar;

/// <summary>
/// The six authored swatches the body colour is drawn from.
/// </summary>
/// <remarks>
/// Hue is the only colour value the name controls freely; lightness and chroma come from one of these
/// swatches, which is what makes every blobatar look like it came from the same designer. The set is
/// banded over <c>[0, 1)</c> with half-open edges, so pale and mid tones dominate and ink stays rare.
/// </remarks>
public enum BlobatarTone
{
    /// <summary>Light and softly coloured (positions below 0.2).</summary>
    Pastel = 0,

    /// <summary>Very light, nearly neutral (0.2 to 0.36).</summary>
    Pale,

    /// <summary>Medium lightness, clearly coloured (0.36 to 0.62).</summary>
    Mid,

    /// <summary>Darker and saturated (0.62 to 0.8).</summary>
    Deep,

    /// <summary>Light and vivid (0.8 to 0.93).</summary>
    Bright,

    /// <summary>Near-black, with light eyes (0.93 and above).</summary>
    Ink,
}

/// <summary>Helpers for <see cref="BlobatarTone"/>.</summary>
public static class BlobatarToneExtensions
{
    /// <summary>
    /// The position in <c>[0, 1)</c> that selects this swatch: the midpoint of its band, as upstream's editor writes it.
    /// </summary>
    public static double ToPosition(this BlobatarTone tone) => tone switch
    {
        BlobatarTone.Pastel => 0.1,
        BlobatarTone.Pale => 0.28,
        BlobatarTone.Mid => 0.49,
        BlobatarTone.Deep => 0.71,
        BlobatarTone.Bright => 0.865,
        BlobatarTone.Ink => 0.965,
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "Unknown tone."),
    };

    /// <summary>The lower-case name upstream's editor uses for the swatch, such as <c>"ink"</c>.</summary>
    public static string ToName(this BlobatarTone tone) => tone switch
    {
        BlobatarTone.Pastel => "pastel",
        BlobatarTone.Pale => "pale",
        BlobatarTone.Mid => "mid",
        BlobatarTone.Deep => "deep",
        BlobatarTone.Bright => "bright",
        BlobatarTone.Ink => "ink",
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, "Unknown tone."),
    };

    /// <summary>The swatch a <c>[0, 1]</c> tone position falls in. An exact 1 falls back to <see cref="BlobatarTone.Pastel"/>, as upstream's half-open bands do.</summary>
    public static BlobatarTone FromPosition(double position) => position switch
    {
        < 0.2 => BlobatarTone.Pastel,
        < 0.36 => BlobatarTone.Pale,
        < 0.62 => BlobatarTone.Mid,
        < 0.8 => BlobatarTone.Deep,
        < 0.93 => BlobatarTone.Bright,
        < 1.0 => BlobatarTone.Ink,
        _ => BlobatarTone.Pastel,
    };
}
