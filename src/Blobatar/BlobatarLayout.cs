namespace Blobatar;

/// <summary>
/// The numeric layout and resolved palette of a blobatar, before serialization.
/// </summary>
/// <param name="Shape">The silhouette the name resolved to.</param>
/// <param name="Body">The core body.</param>
/// <param name="Face">The region the eyes were fitted into.</param>
/// <param name="Eyes">The two eyes, left then right, posed if an expression was set.</param>
/// <param name="Petals">Decorative circles unioned with the body (capsule caps, nubs, cloud lobes, sun petals).</param>
/// <param name="Extra">Extra outlines unioned with the body, as SVG path data (the droplet's taper).</param>
/// <param name="BodyPath">The core body as SVG path data.</param>
/// <param name="Palette">The colours the static render paints, tinted if the expression tints.</param>
/// <remarks>
/// Everything is in viewBox units of a 100×100 frame. Kept separate from rendering so geometric invariants
/// (eyes inside the body, body inside the frame) can be checked directly, and so a caller who pinned an
/// extreme trait can see where the containment fit actually landed it.
/// </remarks>
public sealed record BlobatarLayout(
    BlobatarShape Shape,
    BlobatarBody Body,
    BlobatarEllipse Face,
    IReadOnlyList<BlobatarEye> Eyes,
    IReadOnlyList<BlobatarPetal> Petals,
    IReadOnlyList<string> Extra,
    string BodyPath,
    BlobatarPalette Palette);

/// <summary>The core body of a blobatar.</summary>
/// <param name="Cx">Centre x.</param>
/// <param name="Cy">Centre y.</param>
/// <param name="Rx">Horizontal radius.</param>
/// <param name="Ry">Vertical radius.</param>
/// <param name="Squareness">The superellipse exponent: 2 is an ellipse, higher is squarer.</param>
/// <param name="Rotation">Degrees, clockwise.</param>
/// <param name="Radii">Per-vertex radius multipliers, used by the spline silhouettes.</param>
/// <param name="Sides">Polygon silhouettes only: the number of sides.</param>
/// <param name="Rounding">Polygon silhouettes only: corner rounding, 0 to 1.</param>
public sealed record BlobatarBody(
    double Cx,
    double Cy,
    double Rx,
    double Ry,
    double Squareness,
    double Rotation,
    IReadOnlyList<double> Radii,
    int? Sides,
    double? Rounding);

/// <summary>An axis-aligned ellipse.</summary>
/// <param name="Cx">Centre x.</param>
/// <param name="Cy">Centre y.</param>
/// <param name="Rx">Horizontal radius.</param>
/// <param name="Ry">Vertical radius.</param>
public readonly record struct BlobatarEllipse(double Cx, double Cy, double Rx, double Ry);

/// <summary>One eye: a rotated superellipse.</summary>
/// <param name="Cx">Centre x.</param>
/// <param name="Cy">Centre y.</param>
/// <param name="Rx">Half-width.</param>
/// <param name="Ry">Half-height.</param>
/// <param name="Squareness">The superellipse exponent.</param>
/// <param name="Rotation">Degrees, clockwise.</param>
public readonly record struct BlobatarEye(double Cx, double Cy, double Rx, double Ry, double Squareness, double Rotation);

/// <summary>One decorative circle unioned with the body.</summary>
/// <param name="Cx">Centre x.</param>
/// <param name="Cy">Centre y.</param>
/// <param name="Radius">Radius.</param>
public readonly record struct BlobatarPetal(double Cx, double Cy, double Radius);
