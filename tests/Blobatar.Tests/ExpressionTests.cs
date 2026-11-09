using System.Text.RegularExpressions;
using Blobatar.Internal;
using Xunit;

namespace Blobatar.Tests;

/// <summary>
/// Ported from upstream <c>test/expression.test.ts</c>. Expressions multiply the seed space by the roster,
/// and the interesting failures live in that product: a seed already at its clearance ceiling meeting the
/// pose that tilts hardest. The other half is the contract, which is cheap to assert and expensive to
/// discover broken.
/// </summary>
public class ExpressionTests
{
    private static readonly string[] Seeds = Enumerable.Range(0, 4000).Select(i => $"seed-{i}").ToArray();

    private static readonly BlobatarExpression[] Named =
        Enum.GetValues<BlobatarExpression>().Where(e => e != BlobatarExpression.Idle).ToArray();

    private static readonly BlobatarExpression[] All = Enum.GetValues<BlobatarExpression>();

    private static BlobatarOptions Expr(BlobatarExpression e) => new() { Expression = e };

    private static double Reach(BlobatarEye e)
    {
        var t = (e.Rotation * Math.PI) / 180;
        return Math.Abs(e.Rx * Math.Cos(t)) + Math.Abs(e.Ry * Math.Sin(t));
    }

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
    public void Idle_is_byte_identical_to_no_expression_at_all()
    {
        foreach (var s in Seeds.Take(500))
        {
            Assert.Equal(BlobatarRenderer.Render(s), BlobatarRenderer.Render(s, Expr(BlobatarExpression.Idle)));
        }
    }

    [Fact]
    public void Idle_emits_no_custom_properties_and_no_pose_emits_a_channel_it_leaves_alone()
    {
        Assert.Empty(Poses.PoseVars(Pose.Identity));
        Assert.Equal(string.Empty, Poses.Bake(new LayoutData { Silhouette = Silhouettes.Round, Body = new BodyGeometry(), Face = default, Petals = [], Extra = [], Eyes = [] }, Pose.Identity).Wrap);

        static string[] Keys(BlobatarExpression e) => Poses.PoseVars(e.GetPose()).Select(kv => kv.Key).ToArray();
        Assert.DoesNotContain("--mo-shake", Keys(BlobatarExpression.Happy));
        Assert.Contains("--mo-shake", Keys(BlobatarExpression.Mad));
        foreach (var e in Named.Where(e => e != BlobatarExpression.Thinking))
        {
            Assert.DoesNotContain("--mo-rock", Keys(e));
            Assert.DoesNotContain("--mo-edy2", Keys(e));
        }

        Assert.Contains("--mo-rock", Keys(BlobatarExpression.Thinking));
        Assert.Contains("--mo-edy2", Keys(BlobatarExpression.Thinking));
        Assert.True(BlobatarExpression.Mad.GetPose().Heat > 0);
        Assert.DoesNotContain("--mo-heat", Keys(BlobatarExpression.Mad));
    }

    [Fact]
    public void A_named_expression_changes_the_markup()
    {
        var plain = BlobatarRenderer.Render("seed-1");
        foreach (var e in Named)
        {
            Assert.NotEqual(plain, BlobatarRenderer.Render("seed-1", Expr(e)));
        }
    }

    [Fact]
    public void The_animated_markup_does_not_depend_on_the_expression()
    {
        foreach (var s in Seeds.Take(50))
        {
            var baseline = BlobatarRenderer.Parts(s, new BlobatarOptions { Animate = BlobatarAnimation.Always }).Inner;
            foreach (var e in All)
            {
                Assert.Equal(baseline, BlobatarRenderer.Parts(s, new BlobatarOptions { Animate = BlobatarAnimation.Always, Expression = e }).Inner);
            }
        }
    }

    [Fact]
    public void A_non_idle_expression_still_marks_the_root_class()
    {
        Assert.Equal("mo-root", BlobatarRenderer.Parts("alain", new BlobatarOptions { Animate = BlobatarAnimation.Hover }).RootClass);
        Assert.Equal("mo-root", BlobatarRenderer.Parts("alain", new BlobatarOptions { Animate = BlobatarAnimation.Hover, Expression = BlobatarExpression.Idle }).RootClass);
        Assert.Equal("mo-root mo-always", BlobatarRenderer.Parts("alain", new BlobatarOptions { Animate = BlobatarAnimation.Always }).RootClass);
        foreach (var e in Named)
        {
            Assert.Equal("mo-root mo-expr", BlobatarRenderer.Parts("alain", new BlobatarOptions { Animate = BlobatarAnimation.Hover, Expression = e }).RootClass);
        }
    }

    [Fact]
    public void The_two_capsules_never_fuse_on_any_seed_in_any_expression()
    {
        foreach (var e in All)
        {
            foreach (var s in Seeds)
            {
                var eyes = BlobatarRenderer.Layout(s, Expr(e)).Eyes;
                Assert.True(Math.Abs(eyes[1].Cx - eyes[0].Cx) > Reach(eyes[0]) + Reach(eyes[1]), $"{e} {s}");
            }
        }
    }

    [Fact]
    public void Nothing_leaves_the_frame_including_the_body_offset()
    {
        var numbers = new Regex("-?\\d+\\.?\\d*", RegexOptions.CultureInvariant);
        var paths = new Regex(" d=\"([^\"]+)\"", RegexOptions.CultureInvariant);
        foreach (var e in Named)
        {
            var bdy = e.GetPose().Bdy;
            foreach (var s in Seeds.Take(1500))
            {
                var svg = BlobatarRenderer.Render(s, Expr(e));
                foreach (Match m in paths.Matches(svg))
                {
                    var n = numbers.Matches(m.Groups[1].Value).Select(x => double.Parse(x.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    for (var i = 1; i < n.Length; i += 2)
                    {
                        Assert.InRange(n[i] + bdy, -0.5, 100.5);
                    }
                }
            }
        }
    }

    [Fact]
    public void Eyes_stay_on_the_body_except_where_a_glance_already_allows_otherwise()
    {
        foreach (var e in Named)
        {
            foreach (var s in Seeds.Take(2000))
            {
                var l = BlobatarRenderer.Layout(s, Expr(e));
                var shrink = l.Shape is BlobatarShape.Organic or BlobatarShape.Cloud ? l.Body.Radii.Min() * 0.95 : 1;
                var rx = l.Body.Rx * shrink * 1.12;
                var ry = l.Body.Ry * shrink * 1.12;
                foreach (var eye in l.Eyes)
                {
                    foreach (var (x, y) in Corners(eye))
                    {
                        var d = Math.Pow(Math.Abs((x - l.Body.Cx) / rx), 2) + Math.Pow(Math.Abs((y - l.Body.Cy) / ry), 2);
                        Assert.True(d < 1, $"{e} {s}");
                    }
                }
            }
        }
    }

    private static EyeGeometry[] EyesOf(Pose p) =>
        Poses.Bake(
            new LayoutData
            {
                Silhouette = Silhouettes.Round,
                Body = new BodyGeometry(),
                Face = default,
                Petals = [],
                Extra = [],
                Eyes = [new EyeGeometry(40, 50, 4, 10, 4, 0), new EyeGeometry(60, 50, 4, 10, 4, 0)],
            },
            p).Layout.Eyes;

    [Fact]
    public void A_differential_moves_the_right_eye_and_leaves_the_left_alone()
    {
        var eyes = EyesOf(Pose.Identity with { Esx = 1.5, Esy = 0.4, Tilt = 20, Esx2 = 0.5, Esy2 = 0.1, Tilt2 = 10 });
        Assert.Equal(4 * 1.5, eyes[0].Rx, 9);
        Assert.Equal(10 * 0.4, eyes[0].Ry, 9);
        Assert.Equal(-20, eyes[0].Rot, 9);
        Assert.Equal(4 * 2.0, eyes[1].Rx, 9);
        Assert.Equal(10 * 0.5, eyes[1].Ry, 9);
        Assert.Equal(30, eyes[1].Rot, 9);
    }

    [Fact]
    public void Zero_differentials_are_exactly_the_symmetric_pose()
    {
        var sym = EyesOf(Pose.Identity with { Esx = 1.5, Esy = 0.4, Tilt = 20 });
        var asym = EyesOf(Pose.Identity with { Esx = 1.5, Esy = 0.4, Tilt = 20, Esx2 = 0, Esy2 = 0, Tilt2 = 0 });
        Assert.Equal(sym, asym);
        Assert.Equal(-sym[1].Rot, sym[0].Rot);
    }

    [Fact]
    public void The_rosters_asymmetry_survives_into_real_geometry()
    {
        foreach (var e in Named)
        {
            var p = e.GetPose();
            if (p.Esy2 == 0 && p.Esx2 == 0 && p.Tilt2 == 0)
            {
                continue;
            }

            var baseline = BlobatarRenderer.Layout("seed-7").Eyes;
            var posed = BlobatarRenderer.Layout("seed-7", Expr(e)).Eyes;
            var grow = posed.Select((eye, i) => eye.Ry / baseline[i].Ry).ToArray();
            Assert.Equal(Math.Abs(p.Esy2), Math.Abs(grow[0] - grow[1]), 6);
        }
    }

    [Fact]
    public void The_tightest_pair_in_the_space_still_clears_by_a_wide_margin()
    {
        foreach (var e in Named)
        {
            var min = double.PositiveInfinity;
            foreach (var s in Seeds)
            {
                var eyes = BlobatarRenderer.Layout(s, Expr(e)).Eyes;
                min = Math.Min(min, Math.Abs(eyes[1].Cx - eyes[0].Cx) - Reach(eyes[0]) - Reach(eyes[1]));
            }

            Assert.True(min > 2, $"{e}: {min}");
        }
    }

    [Fact]
    public void Every_blobatar_wears_the_same_strength_of_a_given_expression()
    {
        foreach (var e in Named)
        {
            var p = e.GetPose();
            var tilts = Seeds.Take(800).Select(s =>
            {
                var rot = BlobatarRenderer.Layout(s, Expr(e)).Eyes[0].Rotation;
                var baseline = p.Lock == 1 ? 0 : BlobatarRenderer.Layout(s).Eyes[0].Rotation;
                return Math.Round((rot - baseline) * 1e6) / 1e6;
            }).ToHashSet();
            Assert.True(tilts.Count == 1, $"{e}: {tilts.Count} distinct tilts");
        }
    }

    [Fact]
    public void A_locked_pose_sits_at_exactly_the_angle_it_names()
    {
        foreach (var e in Named)
        {
            var p = e.GetPose();
            if (p.Lock != 1)
            {
                continue;
            }

            foreach (var s in Seeds.Take(400))
            {
                var eyes = BlobatarRenderer.Layout(s, Expr(e)).Eyes;
                Assert.Equal(-p.Tilt, eyes[0].Rotation, 10);
                Assert.Equal(p.Tilt + p.Tilt2, eyes[1].Rotation, 10);
            }
        }
    }

    [Fact]
    public void The_seeded_lean_comes_back_the_moment_the_expression_clears()
    {
        foreach (var s in Seeds.Take(200))
        {
            var before = BlobatarRenderer.Layout(s).Eyes.Select(e => e.Rotation).ToArray();
            _ = BlobatarRenderer.Layout(s, Expr(BlobatarExpression.Mad));
            var after = BlobatarRenderer.Layout(s).Eyes.Select(e => e.Rotation).ToArray();
            Assert.Equal(before, after);
        }
    }

    /// <summary>The seesaw's phase, modelled exactly as <c>--mo-ph</c> computes it in the stylesheet.</summary>
    private static Pose AtPhase(Pose p, double ph) => p with
    {
        Edy = p.Edy + p.Edy2 * p.Rock * ((1 - ph) / 2),
        Edy2 = p.Edy2 * (1 - p.Rock + p.Rock * ph),
        Rock = 0,
    };

    [Fact]
    public void Frame_zero_of_the_seesaw_is_exactly_what_bake_emits()
    {
        var thinking = BlobatarExpression.Thinking.GetPose();
        Assert.Equal(thinking with { Rock = 0 }, AtPhase(thinking, 1));
    }

    [Fact]
    public void The_pair_trades_heights_without_moving_its_mean()
    {
        var thinking = BlobatarExpression.Thinking.GetPose();
        double Mean(double ph)
        {
            var eyes = EyesOf(AtPhase(thinking, ph));
            return (eyes[0].Cy + eyes[1].Cy) / 2;
        }

        foreach (var ph in new[] { 1, 0.5, 0, -0.5, -1 })
        {
            Assert.Equal(Mean(1), Mean(ph), 9);
        }
    }

    [Fact]
    public void The_stagger_really_reverses()
    {
        var thinking = BlobatarExpression.Thinking.GetPose();
        double Spread(double ph) => AtPhase(thinking, ph).Edy2;
        Assert.Equal(-Math.Sign(Spread(-1)), Math.Sign(Spread(1)));
        Assert.Equal(thinking.Edy2 * (1 - thinking.Rock), Spread(0), 9);
        Assert.True(Math.Abs(Spread(-1)) > 0.5);
    }

    [Fact]
    public void A_vertical_differential_moves_the_right_eye_only()
    {
        var eyes = EyesOf(Pose.Identity with { Edy = 2, Edy2 = -6 });
        Assert.Equal(52, eyes[0].Cy, 9);
        Assert.Equal(46, eyes[1].Cy, 9);
    }

    [Fact]
    public void Expression_names_round_trip()
    {
        foreach (var e in All)
        {
            Assert.Equal(e, BlobatarExpressionExtensions.Parse(e.ToName()));
        }

        Assert.True(BlobatarExpression.Mad.IsTinted());
        Assert.False(BlobatarExpression.Happy.IsTinted());
    }
}
