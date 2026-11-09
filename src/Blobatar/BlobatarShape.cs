namespace Blobatar;

/// <summary>
/// The ten silhouettes of the current generation.
/// </summary>
/// <remarks>
/// The shape is derived from the <c>shape</c> trait through a weighted band table, never set directly:
/// round and organic are the everyday shapes, while the louder silhouettes stay finds. To ask for one,
/// pin the trait with <see cref="BlobatarTraits.PinShape"/>, which writes the band's midpoint.
/// </remarks>
public enum BlobatarShape
{
    /// <summary>A soft squircle. The most common shape (about 22% of names).</summary>
    Round = 0,

    /// <summary>A lopsided pebble drawn as a closed spline. About 26% of names.</summary>
    Organic,

    /// <summary>A rounded, tilted square. About 12% of names.</summary>
    Boxy,

    /// <summary>A squat stadium: a box with two cap circles. About 10% of names.</summary>
    Capsule,

    /// <summary>A round body with one or two small nubs on its rim. About 9% of names.</summary>
    Nub,

    /// <summary>A pebble with lobes on its upper half. About 7% of names.</summary>
    Cloud,

    /// <summary>An ellipse tapering to a point above. About 5.5% of names.</summary>
    Droplet,

    /// <summary>A rounded hexagon. About 3.5% of names.</summary>
    Hexagon,

    /// <summary>A small body ringed by petals. About 3% of names.</summary>
    Sun,

    /// <summary>A rounded triangle resting on its base. About 2% of names.</summary>
    Triangle,
}

/// <summary>Helpers for <see cref="BlobatarShape"/>.</summary>
public static class BlobatarShapeExtensions
{
    /// <summary>
    /// The position in <c>[0, 1)</c> that selects this silhouette: the midpoint of its band, which is what
    /// upstream's editor writes and what its tests pin.
    /// </summary>
    public static double ToTraitPosition(this BlobatarShape shape) => shape switch
    {
        BlobatarShape.Round => 0.11,
        BlobatarShape.Organic => 0.35,
        BlobatarShape.Boxy => 0.54,
        BlobatarShape.Capsule => 0.65,
        BlobatarShape.Nub => 0.745,
        BlobatarShape.Cloud => 0.825,
        BlobatarShape.Droplet => 0.888,
        BlobatarShape.Hexagon => 0.933,
        BlobatarShape.Sun => 0.965,
        BlobatarShape.Triangle => 0.99,
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown silhouette."),
    };

    /// <summary>The lower-case name upstream uses for the silhouette, such as <c>"droplet"</c>.</summary>
    public static string ToName(this BlobatarShape shape) => shape switch
    {
        BlobatarShape.Round => "round",
        BlobatarShape.Organic => "organic",
        BlobatarShape.Boxy => "boxy",
        BlobatarShape.Capsule => "capsule",
        BlobatarShape.Nub => "nub",
        BlobatarShape.Cloud => "cloud",
        BlobatarShape.Droplet => "droplet",
        BlobatarShape.Hexagon => "hexagon",
        BlobatarShape.Sun => "sun",
        BlobatarShape.Triangle => "triangle",
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown silhouette."),
    };

    /// <summary>Parses a silhouette name, case-insensitively.</summary>
    public static bool TryParse(string? name, out BlobatarShape shape)
    {
        foreach (var candidate in Enum.GetValues<BlobatarShape>())
        {
            if (string.Equals(candidate.ToName(), name?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                shape = candidate;
                return true;
            }
        }

        shape = default;
        return false;
    }

    /// <summary>Parses a silhouette name, case-insensitively.</summary>
    /// <exception cref="ArgumentException">The name is not a silhouette.</exception>
    public static BlobatarShape Parse(string name) =>
        TryParse(name, out var shape) ? shape : throw new ArgumentException($"Unknown silhouette \"{name}\".", nameof(name));
}
