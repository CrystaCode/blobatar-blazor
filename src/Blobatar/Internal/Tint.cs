namespace Blobatar.Internal;

/// <summary>
/// Where a tinting pose is heading. Four numbers rather than an authored colour, because the endpoint is
/// derived per name: a tint says which way, and the blobatar's own palette says where that lands.
/// </summary>
/// <param name="H">Hue the body arrives at, in degrees. Reached outright, not approached.</param>
/// <param name="L">Lightness it heads toward.</param>
/// <param name="Pull">How far of the way to <paramref name="L"/> the body actually travels, 0 to 1.</param>
/// <param name="C">Chroma floor. The body never desaturates on the way.</param>
internal readonly record struct Tint(double H, double L, double Pull, double C)
{
    /// <summary>Red, because every reference for anger is, and only 60% of the way there in lightness so the tone set survives the trip.</summary>
    public static Tint Hot => new(27, 0.58, 0.6, 0.18);

    /// <summary>Warmth rather than heat.</summary>
    public static Tint Rose => new(358, 0.72, 0.55, 0.16);

    /// <summary>Pale by construction: a shy blobatar that goes as red as an angry one is an angry one.</summary>
    public static Tint Blush => new(12, 0.84, 0.4, 0.1);

    /// <summary>The only target off the warm half of the wheel.</summary>
    public static Tint Bile => new(142, 0.66, 0.6, 0.13);
}
