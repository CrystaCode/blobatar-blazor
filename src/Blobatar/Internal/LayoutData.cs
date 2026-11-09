namespace Blobatar.Internal;

/// <summary>The body a silhouette draws: a superellipse by default, patched by the silhouette before the face is measured.</summary>
internal sealed class BodyGeometry
{
    public double Cx { get; set; }

    public double Cy { get; set; }

    public double Rx { get; set; }

    public double Ry { get; set; }

    /// <summary>Superellipse squareness.</summary>
    public double N { get; set; }

    /// <summary>Degrees, clockwise.</summary>
    public double Rot { get; set; }

    /// <summary>Per-vertex radius multipliers for the spline shapes.</summary>
    public double[] Radii { get; set; } = [];

    /// <summary>Polygon only: how many sides.</summary>
    public int? Sides { get; set; }

    /// <summary>Polygon only: corner rounding, 0 to 1.</summary>
    public double? Round { get; set; }
}

/// <summary>The region the eyes must fit inside.</summary>
internal readonly record struct EllipseGeometry(double Cx, double Cy, double Rx, double Ry);

/// <summary>One decorative circle unioned with the core body.</summary>
internal readonly record struct PetalGeometry(double Cx, double Cy, double R);

/// <summary>One eye: a rotated superellipse.</summary>
internal readonly record struct EyeGeometry(double Cx, double Cy, double Rx, double Ry, double N, double Rot);

/// <summary>What a silhouette decorates with.</summary>
internal sealed class Decoration
{
    public List<PetalGeometry> Petals { get; } = [];

    /// <summary>Extra outlines unioned with the core body, already traced.</summary>
    public List<string> Extra { get; } = [];
}

/// <summary>The numeric layout of one blobatar, before serialization.</summary>
internal sealed class LayoutData
{
    public required Silhouette Silhouette { get; init; }

    public required BodyGeometry Body { get; init; }

    public required EllipseGeometry Face { get; init; }

    public required List<PetalGeometry> Petals { get; init; }

    public required List<string> Extra { get; init; }

    public required EyeGeometry[] Eyes { get; init; }

    /// <summary>The core path: the silhouette's own primitive, or a superellipse when it names none.</summary>
    public string BodyPath() =>
        Silhouette.Path is { } path
            ? path(Body)
            : PathPrimitives.Superellipse(Body.Cx, Body.Cy, Body.Rx, Body.Ry, Body.N, Body.Rot);

    /// <summary>A copy with different eyes, which is what a pose changes.</summary>
    public LayoutData WithEyes(EyeGeometry[] eyes) => new()
    {
        Silhouette = Silhouette,
        Body = Body,
        Face = Face,
        Petals = Petals,
        Extra = Extra,
        Eyes = eyes,
    };
}
