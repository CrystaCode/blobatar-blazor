using System.Globalization;
using System.Text.RegularExpressions;
using Blobatar.Internal;
using Xunit;

namespace Blobatar.Tests;

/// <summary>
/// Ported from upstream <c>test/geometry.test.ts</c>: the invariants that replace eyeballing a tuning
/// grid. Taste is judged in aggregate; these reject broken geometry.
/// </summary>
public class GeometryTests
{
    private static readonly string[] Seeds = Enumerable.Range(0, 1000).Select(i => $"seed-{i}").ToArray();
    private static readonly Regex Numbers = new("-?\\d+\\.?\\d*", RegexOptions.CultureInvariant);
    private static readonly Regex Geometry = new(" d=\"([^\"]+)\"|<circle ([^>]+)>", RegexOptions.CultureInvariant);

    private static double[] NumbersIn(string s) =>
        Numbers.Matches(s).Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).ToArray();

    private static double Inside(double px, double py, double cx, double cy, double rx, double ry, double n) =>
        Math.Pow(Math.Abs((px - cx) / rx), n) + Math.Pow(Math.Abs((py - cy) / ry), n);

    private static (double X, double Y)[] Corners(BlobatarEye e)
    {
        var t = (e.Rotation * Math.PI) / 180;
        var c = Math.Cos(t);
        var s = Math.Sin(t);
        return new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }
            .Select(sign => (e.Cx + sign.Item1 * e.Rx * c - sign.Item2 * e.Ry * s, e.Cy + sign.Item1 * e.Rx * s + sign.Item2 * e.Ry * c))
            .ToArray();
    }

    [Fact]
    public void All_geometry_stays_inside_the_viewBox()
    {
        foreach (var s in Seeds)
        {
            var svg = BlobatarRenderer.Render(s);
            foreach (Match m in Geometry.Matches(svg))
            {
                var src = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                foreach (var n in NumbersIn(src))
                {
                    Assert.InRange(n, -0.01, 100.01);
                }
            }
        }
    }

    [Fact]
    public void Superellipse_coordinates_stay_finite_for_the_whole_n_range()
    {
        for (var n = 1.6; n <= 8; n += 0.1)
        {
            var d = PathPrimitives.Superellipse(50, 50, 30, 30, n);
            Assert.DoesNotContain("NaN", d, StringComparison.Ordinal);
            Assert.All(NumbersIn(d), v => Assert.True(double.IsFinite(v)));
        }
    }

    [Fact]
    public void The_45_degree_control_constant_matches_the_circle_case_exactly() =>
        Assert.Contains("55.23", PathPrimitives.Superellipse(0, 0, 100, 100, 2), StringComparison.Ordinal);

    [Fact]
    public void Control_points_never_overshoot_the_bounding_box()
    {
        for (var n = 1.6; n <= 8; n += 0.1)
        {
            Assert.All(NumbersIn(PathPrimitives.Superellipse(50, 50, 40, 40, n)), v => Assert.InRange(v, 9.9, 90.1));
        }
    }

    [Fact]
    public void BlobPath_interpolates_its_vertices_exactly()
    {
        var d = PathPrimitives.BlobPath(50, 50, 20, 20, [1, 1, 1, 1], 0);
        Assert.StartsWith("M70 50", d, StringComparison.Ordinal);
        Assert.Contains("50 70", d, StringComparison.Ordinal);
        Assert.Contains("30 50", d, StringComparison.Ordinal);
    }

    [Fact]
    public void BlobPath_closes_and_stays_within_its_radii()
    {
        var d = PathPrimitives.BlobPath(50, 50, 20, 20, [1.1, 0.9, 1.05, 0.95, 1.12, 0.88], 0);
        Assert.EndsWith("Z", d, StringComparison.Ordinal);
        Assert.All(NumbersIn(d), v => Assert.InRange(v, 50 - 20 * 1.5, 50 + 20 * 1.5));
    }

    [Fact]
    public void Eyes_sit_inside_the_face()
    {
        foreach (var s in Seeds)
        {
            var l = BlobatarRenderer.Layout(s);
            foreach (var e in l.Eyes)
            {
                foreach (var (x, y) in Corners(e))
                {
                    Assert.True(Inside(x, y, l.Face.Cx, l.Face.Cy, l.Face.Rx, l.Face.Ry, 2) < 1, s);
                }
            }
        }
    }

    [Fact]
    public void Eyes_never_fuse_into_each_other()
    {
        foreach (var s in Seeds)
        {
            var eyes = BlobatarRenderer.Layout(s).Eyes;
            static double Reach(BlobatarEye e)
            {
                var t = (e.Rotation * Math.PI) / 180;
                return Math.Abs(e.Rx * Math.Cos(t)) + Math.Abs(e.Ry * Math.Sin(t));
            }

            Assert.True(Math.Abs(eyes[1].Cx - eyes[0].Cx) > Reach(eyes[0]) + Reach(eyes[1]), s);
        }
    }

    [Fact]
    public void Decoration_stays_attached_to_the_body()
    {
        foreach (var s in Seeds)
        {
            var l = BlobatarRenderer.Layout(s);
            foreach (var p in l.Petals)
            {
                var d = Math.Sqrt(Math.Pow(p.Cx - l.Body.Cx, 2) + Math.Pow(p.Cy - l.Body.Cy, 2));
                Assert.True(d < l.Body.Rx * 0.95 + p.Radius, s);
            }

            foreach (var d in l.Extra)
            {
                // The taper starts at a tangent point, which has to sit on the body ellipse, and that ellipse must be a true one.
                var start = d[1..d.IndexOf('L', StringComparison.Ordinal)].Split(' ').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
                Assert.Equal(1, Inside(start[0], start[1], l.Body.Cx, l.Body.Cy, l.Body.Rx, l.Body.Ry, 2), 2);
                Assert.Equal(2, l.Body.Squareness);
            }
        }
    }

    [Fact]
    public void Every_shape_in_the_vocabulary_is_reachable()
    {
        var shapes = Enumerable.Range(0, 6000).Select(i => BlobatarRenderer.ShapeOf($"seed-{i}")).ToHashSet();
        Assert.Equal(Enum.GetValues<BlobatarShape>().ToHashSet(), shapes);
    }

    [Fact]
    public void The_everyday_shapes_stay_everyday_and_the_loud_ones_stay_rare()
    {
        var shapes = Enumerable.Range(0, 6000).Select(i => BlobatarRenderer.ShapeOf($"seed-{i}")).ToArray();
        double Share(BlobatarShape s) => shapes.Count(x => x == s) / (double)shapes.Length;
        Assert.True(Share(BlobatarShape.Round) + Share(BlobatarShape.Organic) > 0.4);
        Assert.True(Share(BlobatarShape.Triangle) < 0.04);
        Assert.True(Share(BlobatarShape.Sun) + Share(BlobatarShape.Hexagon) + Share(BlobatarShape.Droplet) < 0.16);
    }

    /// <summary>The corners a hashed sweep barely samples and an editor's sliders reach in one drag.</summary>
    private static IEnumerable<BlobatarTraits> OverrideMaps()
    {
        foreach (var v in new[] { 0, 0.5, 0.999999 })
        {
            yield return BlobatarTraits.Fixed(v);
            foreach (var k in BlobatarTraitKey.All)
            {
                yield return BlobatarTraits.Fixed(v).Pin(k, 0);
                yield return BlobatarTraits.Fixed(v).Pin(k, 0.999999);
            }
        }

        foreach (var at in new[] { 0.1, 0.35, 0.55, 0.65, 0.75, 0.82, 0.89, 0.93, 0.96, 0.99 })
        {
            foreach (var v in new[] { 0, 0.5, 0.999999 })
            {
                yield return BlobatarTraits.Fixed(v).Pin("shape", at);
            }
        }

        uint s = 1;
        for (var i = 0; i < 200; i++)
        {
            var m = new BlobatarTraits();
            foreach (var k in BlobatarTraitKey.All)
            {
                s = unchecked(s * 1664525u + 1013904223u);
                m[k] = s / 4294967296.0;
            }

            yield return m;
        }
    }

    [Fact]
    public void Under_trait_overrides_eyes_sit_inside_the_face()
    {
        foreach (var m in OverrideMaps())
        {
            var l = BlobatarRenderer.Layout("cfg", new BlobatarOptions { Traits = m });
            foreach (var e in l.Eyes)
            {
                foreach (var (x, y) in Corners(e))
                {
                    Assert.True(Inside(x, y, l.Face.Cx, l.Face.Cy, l.Face.Rx, l.Face.Ry, 2) < 1, m.ToString());
                }
            }
        }
    }

    [Fact]
    public void Under_trait_overrides_all_geometry_stays_inside_the_viewBox()
    {
        foreach (var m in OverrideMaps())
        {
            var svg = BlobatarRenderer.Render("cfg", new BlobatarOptions { Traits = m });
            Assert.DoesNotContain("NaN", svg, StringComparison.Ordinal);
            foreach (Match g in Geometry.Matches(svg))
            {
                var src = g.Groups[1].Success ? g.Groups[1].Value : g.Groups[2].Value;
                foreach (var n in NumbersIn(src))
                {
                    Assert.InRange(n, -0.01, 100.01);
                }
            }
        }
    }

    [Fact]
    public void Polygon_sharp_corners_land_exactly_on_the_vertices()
    {
        var d = PathPrimitives.Polygon(50, 50, 20, 20, 4, round: 0);
        foreach (var v in new[] { "50 30", "70 50", "50 70", "30 50" })
        {
            Assert.Contains(v, d, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Polygon_vertex_sits_at_the_top_so_a_triangle_rests_on_its_base()
    {
        var d = PathPrimitives.Polygon(50, 50, 20, 20, 3, round: 0);
        Assert.Contains("50 30", d, StringComparison.Ordinal);
        Assert.Contains("60", d, StringComparison.Ordinal);
        Assert.DoesNotContain("50 70", d, StringComparison.Ordinal);
    }

    [Fact]
    public void Polygon_outline_never_leaves_the_bounding_box()
    {
        foreach (var sides in new[] { 3, 4, 5, 6, 8 })
        {
            for (var round = 0.0; round <= 1.0001; round += 0.1)
            {
                foreach (var rot in new[] { 0, 17, 90, -33 })
                {
                    var d = PathPrimitives.Polygon(50, 50, 30, 20, sides, round, rot);
                    Assert.DoesNotContain("NaN", d, StringComparison.Ordinal);
                    Assert.EndsWith("Z", d, StringComparison.Ordinal);
                    var n = NumbersIn(d);
                    for (var i = 0; i < n.Length; i++)
                    {
                        var (lo, hi) = i % 2 == 0 ? (19.9, 80.1) : (29.9, 70.1);
                        Assert.InRange(n[i], lo, hi);
                    }
                }
            }
        }
    }

    [Fact]
    public void Full_rounding_drops_the_straight_runs_instead_of_emitting_empty_ones()
    {
        Assert.DoesNotContain("L", PathPrimitives.Polygon(50, 50, 20, 20, 6, round: 1), StringComparison.Ordinal);
        Assert.Contains("L", PathPrimitives.Polygon(50, 50, 20, 20, 6, round: 0.9), StringComparison.Ordinal);
    }

    [Fact]
    public void Numbers_are_formatted_as_JavaScript_formats_them()
    {
        Assert.Equal("0", PathPrimitives.R2(-0.001));
        Assert.Equal("0", PathPrimitives.R2(-0.0));
        Assert.Equal("88.1", PathPrimitives.R2(88.1));
        Assert.Equal("50", PathPrimitives.R2(50.004));
        Assert.Equal("2.5", PathPrimitives.R2(2.5));
        Assert.Equal("0.13", PathPrimitives.R2(0.125));
        Assert.Equal("-0.12", PathPrimitives.R2(-0.125));
        Assert.Equal("-2.2", PathPrimitives.R3(-2.2));
        Assert.Equal(3, JsMath.Round(2.5));
        Assert.Equal(-2, JsMath.Round(-2.5));
        Assert.Equal("1e+21", JsMath.ToJsString(1e21));
        Assert.Equal("1000000000000000", JsMath.ToJsString(1e15));
        Assert.Equal("0.000001", JsMath.ToJsString(1e-6));
        Assert.Equal("1e-7", JsMath.ToJsString(1e-7));
    }
}
