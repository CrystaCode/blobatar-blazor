using System.Collections.Immutable;
using System.Globalization;

namespace Blobatar;

/// <summary>
/// Every trait key the current generation reads, as constants.
/// </summary>
/// <remarks>
/// <para>Keys are the addresses of <see cref="BlobatarTraits"/> overrides. They are stable across minor
/// versions; the numeric range each is read into is frozen per generation alongside the shape bands and
/// the tone set, because a pinned position is relative to that range.</para>
/// <para>A key only matters when the silhouette reads it: <see cref="BodyRotation"/> is read by boxy,
/// triangle and hexagon bodies, the <c>sun.*</c> keys by suns, and so on. Pinning a key no silhouette in
/// play reads changes nothing. The editor label upstream uses for each axis is given in its summary.</para>
/// </remarks>
public static class BlobatarTraitKey
{
    /// <summary>"silhouette": which of the ten shapes. Band midpoints are in <see cref="BlobatarShapeExtensions.ToTraitPosition"/>.</summary>
    public const string Shape = "shape";

    /// <summary>"hue": the colour wheel position; <see cref="BlobatarOptions.Hue"/> states it in degrees and wins.</summary>
    public const string Hue = "hue";

    /// <summary>"tone": the swatch, pale to ink; <see cref="BlobatarOptions.Tone"/> wins. Midpoints are in <see cref="BlobatarToneExtensions.ToPosition"/>.</summary>
    public const string Tone = "tone";

    /// <summary>Body "size": the core radius, 31 to 38 units before the silhouette's own scale.</summary>
    public const string BodyRadius = "body.r";

    /// <summary>Body "proportion": the vertical radius as 0.92 to 1.08 of the horizontal one.</summary>
    public const string BodyRatio = "body.ratio";

    /// <summary>Horizontal body jitter, up to 1.5 units either way.</summary>
    public const string BodyX = "body.x";

    /// <summary>Vertical body jitter, up to 1.5 units either way.</summary>
    public const string BodyY = "body.y";

    /// <summary>Body "squareness": the superellipse exponent, 1.9 to 2.5 (3.4 to 6 on a boxy body).</summary>
    public const string BodySquareness = "body.n";

    /// <summary>Body "tilt" in degrees: boxy (±20), hexagon (±12) and triangle (±5) only.</summary>
    public const string BodyRotation = "body.rot";

    /// <summary>How many spline vertices an organic or cloud body has, 6 to 8.</summary>
    public const string BodyPoints = "body.pts";

    /// <summary>Horizontal gaze: where the eye pair sits across the face, ±9% of the face width.</summary>
    public const string GazeX = "gaze.x";

    /// <summary>Vertical gaze: where the eye pair sits down the face, -20% to +8% of the face height.</summary>
    public const string GazeY = "gaze.y";

    /// <summary>Eye "size": the capsule's half-width as 7.5% to 10.5% of the body radius.</summary>
    public const string EyeRadius = "eye.rx";

    /// <summary>Eye "roundness": the capsule's height-to-width ratio, 1.9 to 3.2.</summary>
    public const string EyeRatio = "eye.ratio";

    /// <summary>The right eye's size relative to the left, 0.78 to 1.24.</summary>
    public const string EyeScale = "eye.scale";

    /// <summary>The right eye's extra height relative to the left, 0.85 to 1.18.</summary>
    public const string EyeStretch = "eye.stretch";

    /// <summary>Eye "separation": the clearance between the two capsules, 10% to 24% of the body radius.</summary>
    public const string EyeGap = "eye.gap";

    /// <summary>Eye "squareness": the capsule's superellipse exponent, 3.5 to 6.</summary>
    public const string EyeSquareness = "eye.n";

    /// <summary>Eye "lean": the shared tilt, up to ±12 degrees bounded by the clearance.</summary>
    public const string EyeLean = "eye.lean";

    /// <summary>Extra lean on the right eye, up to ±3.5 degrees.</summary>
    public const string EyeLean2 = "eye.lean2";

    /// <summary>Vertical offset of the right eye, up to ±4% of the face height.</summary>
    public const string EyeDy = "eye.dy";

    /// <summary>Sun "petals": 6 to 9. Sun only.</summary>
    public const string SunCount = "sun.n";

    /// <summary>Sun "petal distance": 1.0 to 1.08 body radii. Sun only.</summary>
    public const string SunDistance = "sun.dist";

    /// <summary>Sun "petal size": 0.2 to 0.26 body radii. Sun only.</summary>
    public const string SunRadius = "sun.r";

    /// <summary>Sun "petal rotation": where the first petal sits. Sun only.</summary>
    public const string SunRotation = "sun.rot";

    /// <summary>Cloud "lobes": 4 to 6. Cloud only.</summary>
    public const string CloudCount = "cloud.n";

    /// <summary>Nub count: 1 or 2. Nub only.</summary>
    public const string NubCount = "nub.n";

    /// <summary>Polygon "corner rounding": 0.24 to 0.5. Triangle and hexagon only.</summary>
    public const string PolygonRounding = "poly.round";

    /// <summary>Capsule "squat": the body height as 0.55 to 0.68 of its width. Capsule only.</summary>
    public const string CapsuleSquat = "capsule.squat";

    /// <summary>Droplet "tip length": how far the apex sits above the centre, 1.4 to 1.65 body heights. Droplet only.</summary>
    public const string DropletTip = "droplet.tip";

    /// <summary>The per-vertex radius jitter of an organic or cloud body, <c>body.r0</c> to <c>body.r7</c>.</summary>
    public static string BodyRadiusAt(int index) => "body.r" + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>The radius of one cloud lobe, <c>cloud.r0</c> to <c>cloud.r5</c>. Cloud only.</summary>
    public static string CloudRadiusAt(int index) => "cloud.r" + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>The angle of one nub, <c>nub.a0</c> or <c>nub.a1</c>. Nub only.</summary>
    public static string NubAngleAt(int index) => "nub.a" + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>The size of one nub, <c>nub.r0</c> or <c>nub.r1</c>. Nub only.</summary>
    public static string NubRadiusAt(int index) => "nub.r" + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Every key the layout reads, in upstream's order. Pinning all of them fixes the blobatar completely.
    /// The <c>motion.*</c> keys that seed the idle animation are not included: they move nothing in a still render.
    /// </summary>
    public static ImmutableArray<string> All { get; } =
    [
        Shape, Hue, Tone,
        BodyRadius, BodyRatio, BodyX, BodyY, BodySquareness, BodyRotation, BodyPoints,
        BodyRadiusAt(0), BodyRadiusAt(1), BodyRadiusAt(2), BodyRadiusAt(3), BodyRadiusAt(4), BodyRadiusAt(5), BodyRadiusAt(6), BodyRadiusAt(7),
        GazeX, GazeY,
        EyeRadius, EyeRatio, EyeScale, EyeStretch, EyeGap, EyeSquareness, EyeLean, EyeLean2, EyeDy,
        SunCount, SunDistance, SunRadius, SunRotation,
        CloudCount, CloudRadiusAt(0), CloudRadiusAt(1), CloudRadiusAt(2), CloudRadiusAt(3), CloudRadiusAt(4), CloudRadiusAt(5),
        NubCount, NubAngleAt(0), NubAngleAt(1), NubRadiusAt(0), NubRadiusAt(1),
        PolygonRounding, CapsuleSquat, DropletTip,
    ];
}
