namespace Blobatar.Internal;

/// <summary>
/// One silhouette: everything the layout needs to draw it and nothing about when to draw it.
/// </summary>
/// <remarks>
/// How often a shape comes up is a property of the generation's band table (<see cref="BlobStyle"/>),
/// not of the silhouette. Shapes that are parameterizations of another share its primitive: boxy is
/// round with a squarer exponent and a tilt, hexagon is triangle with six sides, cloud is organic with lobes.
/// </remarks>
internal sealed class Silhouette
{
    public required BlobatarShape Shape { get; init; }

    /// <summary>How much of the frame the core body takes.</summary>
    public required double Core { get; init; }

    /// <summary>Patches the body before the face is measured.</summary>
    public Action<TraitReader, BodyGeometry>? Body { get; init; }

    /// <summary>The region the eyes must fit inside. Omitted, it is the body itself.</summary>
    public Func<BodyGeometry, EllipseGeometry>? Face { get; init; }

    /// <summary>Adds petals and extra outlines.</summary>
    public Action<TraitReader, BodyGeometry, Decoration>? Decorate { get; init; }

    /// <summary>The core path. Omitted, a superellipse.</summary>
    public Func<BodyGeometry, string>? Path { get; init; }
}

/// <summary>The ten silhouettes of generation 2. Five path calls serve all ten.</summary>
internal static class Silhouettes
{
    private static string Poly(BodyGeometry b) =>
        PathPrimitives.Polygon(b.Cx, b.Cy, b.Rx, b.Ry, b.Sides!.Value, b.Round!.Value, b.Rot);

    private static string Spline(BodyGeometry b) =>
        PathPrimitives.BlobPath(b.Cx, b.Cy, b.Rx, b.Ry, b.Radii, b.Rot);

    private static EllipseGeometry Shrunk(BodyGeometry b, double k) => new(b.Cx, b.Cy, b.Rx * k, b.Ry * k);

    private static EllipseGeometry SplineFace(BodyGeometry b) => Shrunk(b, b.Radii.Min() * 0.95);

    private static EllipseGeometry PolyFace(BodyGeometry b) => Shrunk(b, 0.84);

    public static readonly Silhouette Round = new() { Shape = BlobatarShape.Round, Core = 1 };

    public static readonly Silhouette Organic = new()
    {
        Shape = BlobatarShape.Organic,
        Core = 0.98,
        Path = Spline,
        Face = SplineFace,
    };

    /// <summary>Round, squared off and tilted. Same path, different parameters.</summary>
    public static readonly Silhouette Boxy = new()
    {
        Shape = BlobatarShape.Boxy,
        Core = 0.86,
        Body = (t, b) =>
        {
            b.N = t.Num("body.n", 3.4, 6);
            b.Rot = t.Num("body.rot", -20, 20);
        },
    };

    public static readonly Silhouette Capsule = new()
    {
        Shape = BlobatarShape.Capsule,
        Core = 1.02,
        Body = (t, b) => b.Ry *= t.Num("capsule.squat", 0.55, 0.68),
        Face = b => Shrunk(b, 0.94),
        Decorate = (_, b, deco) =>
        {
            foreach (var s in new[] { -1, 1 })
            {
                deco.Petals.Add(new PetalGeometry(b.Cx + s * (b.Rx - b.Ry), b.Cy, b.Ry));
            }
        },
        Path = b => PathPrimitives.Box(b.Cx, b.Cy, b.Rx - b.Ry, b.Ry),
    };

    public static readonly Silhouette Nub = new()
    {
        Shape = BlobatarShape.Nub,
        Core = 0.88,
        Decorate = (t, b, deco) =>
        {
            var count = t.Int("nub.n", 1, 2);
            for (var i = 0; i < count; i++)
            {
                var a = t.Num(BlobatarTraitKey.NubAngleAt(i), 0, 2 * Math.PI);
                deco.Petals.Add(new PetalGeometry(
                    b.Cx + Math.Cos(a) * b.Rx * 0.88,
                    b.Cy + Math.Sin(a) * b.Rx * 0.88,
                    b.Rx * t.Num(BlobatarTraitKey.NubRadiusAt(i), 0.24, 0.4)));
            }
        },
    };

    /// <summary>Organic, with lobes on the upper half.</summary>
    public static readonly Silhouette Cloud = new()
    {
        Shape = BlobatarShape.Cloud,
        Core = 0.78,
        Face = SplineFace,
        Path = Spline,
        Decorate = (t, b, deco) =>
        {
            var count = t.Int("cloud.n", 4, 6);
            for (var i = 0; i < count; i++)
            {
                var a = Math.PI + (Math.PI * (i + 0.5)) / count;
                deco.Petals.Add(new PetalGeometry(
                    b.Cx + Math.Cos(a) * b.Rx * 0.8,
                    b.Cy + Math.Sin(a) * b.Rx * 0.5,
                    b.Rx * t.Num(BlobatarTraitKey.CloudRadiusAt(i), 0.44, 0.62)));
            }
        },
    };

    public static readonly Silhouette Droplet = new()
    {
        Shape = BlobatarShape.Droplet,
        Core = 0.78,
        // Shifted down by what the taper adds above, so head and point together sit centred in the frame.
        // `n` is pinned to a true ellipse, which is the curve the taper is tangent to.
        Body = (_, b) =>
        {
            b.Cy += 0.22 * b.Ry;
            b.N = 2;
        },
        Face = b => new EllipseGeometry(b.Cx, b.Cy + b.Ry * 0.05, b.Rx * 0.88, b.Ry * 0.88),
        Decorate = (t, b, deco) =>
            deco.Extra.Add(PathPrimitives.Taper(b.Cx, b.Cy, b.Rx, b.Ry, t.Num("droplet.tip", 1.4, 1.65))),
    };

    public static readonly Silhouette Hexagon = new()
    {
        Shape = BlobatarShape.Hexagon,
        Core = 1.05,
        Path = Poly,
        Face = PolyFace,
        Body = (t, b) =>
        {
            b.Sides = 6;
            b.Rot = t.Num("body.rot", -12, 12);
            b.Round = t.Num("poly.round", 0.24, 0.5);
        },
    };

    public static readonly Silhouette Sun = new()
    {
        Shape = BlobatarShape.Sun,
        Core = 0.7,
        Decorate = (t, b, deco) =>
        {
            var count = t.Int("sun.n", 6, 9);
            var dist = b.Rx * t.Num("sun.dist", 1.0, 1.08);
            var pr = b.Rx * t.Num("sun.r", 0.2, 0.26);
            var off = t.Num("sun.rot", 0, 2 * Math.PI);
            for (var i = 0; i < count; i++)
            {
                var a = off + (2 * Math.PI * i) / count;
                deco.Petals.Add(new PetalGeometry(b.Cx + Math.Cos(a) * dist, b.Cy + Math.Sin(a) * dist, pr));
            }
        },
    };

    /// <summary>Hexagon with three sides, and a tighter tilt so it rests on its base.</summary>
    public static readonly Silhouette Triangle = new()
    {
        Shape = BlobatarShape.Triangle,
        Core = 1.15,
        Path = Poly,
        Body = (t, b) =>
        {
            b.Sides = 3;
            b.Rot = t.Num("body.rot", -5, 5);
            b.Round = t.Num("poly.round", 0.24, 0.5);
        },
        Face = b => new EllipseGeometry(b.Cx, b.Cy + b.Ry * 0.1, b.Rx * 0.54, b.Ry * 0.36),
    };
}
