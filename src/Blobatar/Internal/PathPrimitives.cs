using System.Text;

namespace Blobatar.Internal;

/// <summary>
/// The path primitives, and the two number formatters every emitted coordinate goes through.
/// </summary>
/// <remarks>
/// <para>The superellipse <c>|x/a|^n + |y/b|^n = 1</c> covers the symmetric vocabulary: <c>n=2</c> is an
/// ellipse, <c>n≈4</c> a squircle, <c>n→large</c> a rectangle. One shape function and one continuous knob,
/// so "head shape" is a numeric trait rather than a set of hand-drawn alternatives. A closed Catmull-Rom
/// spline carries the organic shapes, a rounded polygon the flat-sided ones, and a box and a taper are
/// the two helpers the capsule and the droplet union with their core.</para>
/// <para>Every coordinate is rounded to two decimals and written the way JavaScript writes a number,
/// which is what makes the output byte-identical to upstream.</para>
/// </remarks>
internal static class PathPrimitives
{
    /// <summary>Two decimals, as JavaScript prints them: <c>88.1</c>, <c>50</c>, and <c>0</c> for negative zero.</summary>
    public static string R2(double v) => JsMath.ToJsString(JsMath.Round(v * 100) / 100);

    /// <summary>Three decimals, which is what every pose channel rounds to.</summary>
    public static string R3(double v) => JsMath.ToJsString(JsMath.Round(v * 1000) / 1000);

    /// <summary>
    /// Approximates each quadrant of a superellipse with one cubic Bézier.
    /// </summary>
    /// <remarks>
    /// The control offset is chosen so the curve passes exactly through the superellipse's 45° point:
    /// <c>B(0.5) = a(4+3k)/8</c> must equal <c>a·2^(-1/n)</c>. At <c>n=2</c> this yields 0.5523, the
    /// standard circle constant. Above <c>n≈5.55</c> the offset would exceed the radius and bulge outside
    /// the bounding box, so it is clamped to 1: past that point a superellipse is visually a rounded
    /// rectangle anyway. Rotation is baked into the coordinates so the SVG needs no transform.
    /// </remarks>
    public static string Superellipse(double cx, double cy, double rx, double ry, double n = 4, double rot = 0)
    {
        var k = Math.Min(1, (8 * Math.Pow(2, -1 / n) - 4) / 3);
        var a = rx;
        var b = ry;
        var ak = a * k;
        var bk = b * k;

        // Anchor, control, control: walking the four quadrants.
        (double X, double Y)[] pts =
        [
            (a, 0),
            (a, bk), (ak, b), (0, b),
            (-ak, b), (-a, bk), (-a, 0),
            (-a, -bk), (-ak, -b), (0, -b),
            (ak, -b), (a, -bk), (a, 0),
        ];

        var t = (rot * Math.PI) / 180;
        var cos = Math.Cos(t);
        var sin = Math.Sin(t);

        var d = new StringBuilder(160);
        d.Append('M').Append(At(0));
        for (var i = 1; i < 13; i += 3)
        {
            d.Append('C').Append(At(i)).Append(' ').Append(At(i + 1)).Append(' ').Append(At(i + 2));
        }

        return d.Append('Z').ToString();

        string At(int i)
        {
            var (x, y) = pts[i];
            return R2(cx + x * cos - y * sin) + " " + R2(cy + x * sin + y * cos);
        }
    }

    /// <summary>A quadratic arc, stroked. Upstream keeps it for smiles; the current generation draws none, but the primitive stays with its siblings.</summary>
    public static string Arc(double cx, double cy, double w, double depth) =>
        "M" + R2(cx - w) + " " + R2(cy) + "Q" + R2(cx) + " " + R2(cy + depth) + " " + R2(cx + w) + " " + R2(cy);

    /// <summary>
    /// An organic closed curve: radii sampled around a circle, joined by a closed Catmull-Rom spline
    /// converted to cubic Béziers.
    /// </summary>
    /// <remarks>
    /// <paramref name="radii"/> are multipliers of the base radius, one per vertex, so a seed perturbing them
    /// by ±16% produces lopsided pebbles without any noise function. Catmull-Rom interpolates its points
    /// exactly, which is what makes the radii mean what they say and keeps containment predictable.
    /// </remarks>
    public static string BlobPath(double cx, double cy, double rx, double ry, double[] radii, double rot = 0)
    {
        var n = radii.Length;
        var t0 = (rot * Math.PI) / 180;
        var p = new (double X, double Y)[n];
        for (var i = 0; i < n; i++)
        {
            var m = radii[i];
            var a = t0 + (2 * Math.PI * i) / n;
            p[i] = (cx + rx * m * Math.Cos(a), cy + ry * m * Math.Sin(a));
        }

        var d = new StringBuilder(n * 60);
        var (sx, sy) = At(0);
        d.Append('M').Append(R2(sx)).Append(' ').Append(R2(sy));

        for (var i = 0; i < n; i++)
        {
            var (x0, y0) = At(i - 1);
            var (x1, y1) = At(i);
            var (x2, y2) = At(i + 1);
            var (x3, y3) = At(i + 2);
            d.Append('C').Append(R2(x1 + (x2 - x0) / 6)).Append(' ').Append(R2(y1 + (y2 - y0) / 6))
                .Append(' ').Append(R2(x2 - (x3 - x1) / 6)).Append(' ').Append(R2(y2 - (y3 - y1) / 6))
                .Append(' ').Append(R2(x2)).Append(' ').Append(R2(y2));
        }

        return d.Append('Z').ToString();

        (double X, double Y) At(int i) => p[((i % n) + n) % n];
    }

    /// <summary>
    /// A regular polygon with rounded corners.
    /// </summary>
    /// <remarks>
    /// Corners are cut back along both adjoining edges by <paramref name="round"/> and joined with a
    /// quadratic through the vertex itself, which keeps the whole outline inside the polygon's convex hull:
    /// a quadratic never leaves the triangle of its three points. At <c>round = 1</c> the cuts meet at the
    /// edge midpoints and the straight runs vanish. A vertex sits at the top, so a triangle rests on its base.
    /// <paramref name="rx"/> and <paramref name="ry"/> are the circumradius on each axis.
    /// </remarks>
    public static string Polygon(double cx, double cy, double rx, double ry, int sides, double round = 0.3, double rot = 0)
    {
        // Halved because the cut is taken from both ends of every edge: at round = 1 the two cuts meet exactly.
        var k = round > 0 ? (round < 1 ? round / 2 : 0.5) : 0;
        // −90° so a vertex sits at the top.
        var t0 = (rot * Math.PI) / 180 - Math.PI / 2;
        var v = new (double X, double Y)[sides];
        for (var i = 0; i < sides; i++)
        {
            var a = t0 + (2 * Math.PI * i) / sides;
            v[i] = (cx + rx * Math.Cos(a), cy + ry * Math.Sin(a));
        }

        var d = new StringBuilder(sides * 40);
        d.Append('M').Append(Cut(0, -1));
        for (var i = 0; i < sides; i++)
        {
            var (x, y) = At(i);
            d.Append('Q').Append(R2(x)).Append(' ').Append(R2(y)).Append(' ').Append(Cut(i, i + 1));
            // The straight run to the next corner's cut, omitted when the cuts meet.
            if (k < 0.5)
            {
                d.Append('L').Append(Cut(i + 1, i));
            }
        }

        return d.Append('Z').ToString();

        (double X, double Y) At(int i) => v[((i % sides) + sides) % sides];

        string Cut(int i, int j)
        {
            var (x0, y0) = At(i);
            var (x1, y1) = At(j);
            return R2(x0 + (x1 - x0) * k) + " " + R2(y0 + (y1 - y0) * k);
        }
    }

    /// <summary>
    /// The straight run of a capsule, as a plain box. Drawn with the two cap circles, the union is an exact
    /// stadium with no crease, which a superellipse could not provide.
    /// </summary>
    public static string Box(double cx, double cy, double rx, double ry)
    {
        var l = R2(cx - rx);
        var r = R2(cx + rx);
        return "M" + l + " " + R2(cy - ry) + "H" + r + "V" + R2(cy + ry) + "H" + l + "Z";
    }

    /// <summary>
    /// The taper of a droplet: the two tangents from an apex to the body ellipse, eased through the apex
    /// with a quadratic so the drawn tip stops just short of it.
    /// </summary>
    /// <remarks>
    /// <paramref name="tip"/> is how far the apex sits above the centre in units of <paramref name="ry"/>.
    /// In the circle the ellipse is an affine image of, the tangent points sit at <c>acos(1/tip)</c> from the
    /// apex direction; affine maps preserve tangency, so scaling those points by the radii is exact.
    /// </remarks>
    public static string Taper(double cx, double cy, double rx, double ry, double tip)
    {
        var t = Math.Max(1.05, tip);
        var tx = rx * Math.Sqrt(1 - 1 / (t * t));
        var ty = cy - ry / t;
        var apex = cy - t * ry;
        var px = tx * 0.14;
        var py = ty + 0.86 * (apex - ty);
        return "M" + R2(cx - tx) + " " + R2(ty)
            + "L" + R2(cx - px) + " " + R2(py)
            + "Q" + R2(cx) + " " + R2(apex) + " " + R2(cx + px) + " " + R2(py)
            + "L" + R2(cx + tx) + " " + R2(ty) + "Z";
    }
}
