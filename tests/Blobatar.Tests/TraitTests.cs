using Xunit;

namespace Blobatar.Tests;

/// <summary>Ported from upstream <c>test/traits.test.ts</c>: trait overrides are the whole configuration surface.</summary>
public class TraitTests
{
    private const string Seed = "alain";

    private static BlobatarOptions With(BlobatarTraits traits) => new() { Traits = traits };

    [Fact]
    public void An_override_replaces_exactly_its_own_key()
    {
        var baseline = TraitReader.For(Seed);
        var over = TraitReader.For(Seed, true, new BlobatarTraits { ["eye.gap"] = 0.82 });

        Assert.Equal(0.82, over["eye.gap"]);
        foreach (var k in new[] { "shape", "body.r", "eye.rx", "hue", "tone" })
        {
            Assert.Equal(baseline[k], over[k]);
        }
    }

    [Fact]
    public void Zero_is_a_value_not_an_absence()
    {
        var t = TraitReader.For(Seed, true, new BlobatarTraits { ["eye.rx"] = 0 });
        Assert.Equal(0, t["eye.rx"]);
        Assert.Equal(0.075, t.Num("eye.rx", 0.075, 0.105));
    }

    [Fact]
    public void The_derived_readers_use_the_override()
    {
        var t = TraitReader.For(Seed, true, new BlobatarTraits { ["a"] = 0, ["b"] = 0.5, ["c"] = 0.999999 });
        Assert.Equal(10, t.Num("a", 10, 20));
        Assert.Equal(5, t.Int("b", 0, 10));
        Assert.Equal("z", t.Pick("c", new[] { "x", "y", "z" }));
        Assert.Equal(-4, t.Jitter("a", 4));
        Assert.True(t.Bool("a"));
        Assert.False(t.Bool("c"));
    }

    [Fact]
    public void Out_of_range_values_are_clamped_instead_of_trusted()
    {
        var t = TraitReader.For(Seed, true, new BlobatarTraits
        {
            ["high"] = 1,
            ["way"] = 99,
            ["low"] = -3,
            ["nan"] = double.NaN,
            ["inf"] = double.PositiveInfinity,
        });

        foreach (var k in new[] { "high", "way", "inf" })
        {
            Assert.Equal("z", t.Pick(k, new[] { "x", "y", "z" }));
            Assert.Equal(8, t.Int(k, 6, 8));
            Assert.True(t[k] < 1);
        }

        foreach (var k in new[] { "low", "nan" })
        {
            Assert.Equal(0, t[k]);
            Assert.Equal("x", t.Pick(k, new[] { "x", "y", "z" }));
        }
    }

    [Fact]
    public void A_one_element_list_is_the_number() =>
        Assert.Equal(
            BlobatarRenderer.Render(Seed, With(new BlobatarTraits { ["shape"] = 0.95 })),
            BlobatarRenderer.Render(Seed, With(new BlobatarTraits { ["shape"] = new[] { 0.95 } })));

    [Fact]
    public void An_empty_list_is_an_absent_key() =>
        Assert.Equal(BlobatarRenderer.Render(Seed), BlobatarRenderer.Render(Seed, With(new BlobatarTraits { ["shape"] = Array.Empty<double>() })));

    [Fact]
    public void Every_seed_lands_on_a_listed_value()
    {
        double[] listed = [0.11, 0.825, 0.965];
        for (var i = 0; i < 600; i++)
        {
            Assert.Contains(TraitReader.For($"user{i}", true, new BlobatarTraits { ["shape"] = listed })["shape"], listed);
        }
    }

    [Fact]
    public void The_seed_still_chooses_roughly_evenly_among_what_is_listed()
    {
        var traits = new BlobatarTraits { ["shape"] = new[] { 0.11, 0.825, 0.965 } };
        var tally = new Dictionary<BlobatarShape, int>();
        for (var i = 0; i < 600; i++)
        {
            var shape = BlobatarRenderer.ShapeOf($"user{i}", With(traits));
            tally[shape] = tally.GetValueOrDefault(shape) + 1;
        }

        Assert.Equal(new[] { BlobatarShape.Round, BlobatarShape.Cloud, BlobatarShape.Sun }, tally.Keys.OrderBy(k => k).ToArray());
        Assert.All(tally.Values, n => Assert.InRange(n, 121, 299));
    }

    [Fact]
    public void The_choice_is_stable_and_independent_of_every_other_key()
    {
        double[] listed = [0.11, 0.825, 0.965];
        var baseline = TraitReader.For(Seed, true, new BlobatarTraits { ["shape"] = listed })["shape"];
        Assert.Equal(baseline, TraitReader.For(Seed, true, new BlobatarTraits { ["shape"] = listed })["shape"]);
        Assert.Equal(baseline, TraitReader.For(Seed, true, new BlobatarTraits { ["shape"] = listed, ["hue"] = 0.5, ["eye.gap"] = 0.9 })["shape"]);
    }

    [Fact]
    public void A_listed_value_is_clamped_like_a_written_one()
    {
        var t = TraitReader.For(Seed, true, new BlobatarTraits { ["high"] = new[] { 1.0, 1.0 }, ["low"] = new[] { -3, double.NaN } });
        Assert.Equal("z", t.Pick("high", new[] { "x", "y", "z" }));
        Assert.True(t["high"] < 1);
        Assert.Equal(0, t["low"]);
    }

    [Fact]
    public void Narrowing_one_key_leaves_the_rest_of_the_blobatar_on_the_seed()
    {
        var options = With(new BlobatarTraits { ["shape"] = new[] { 0.11, 0.965 } });
        var a = BlobatarRenderer.Layout("one", options);
        var b = BlobatarRenderer.Layout("two", options);
        Assert.NotEqual(a.Body.Rx, b.Body.Rx);
        Assert.NotEqual(a.Palette.Head, b.Palette.Head);
    }

    [Fact]
    public void The_same_seed_and_overrides_always_render_the_same_markup()
    {
        var options = With(new BlobatarTraits { ["shape"] = 0.95, ["eye.ratio"] = 0.1 });
        Assert.Equal(BlobatarRenderer.Render(Seed, options), BlobatarRenderer.Render(Seed, options));
    }

    [Fact]
    public void An_empty_map_renders_byte_identical_markup_to_no_map_at_all() =>
        Assert.Equal(BlobatarRenderer.Render(Seed), BlobatarRenderer.Render(Seed, With(new BlobatarTraits())));

    [Fact]
    public void A_sparse_map_leaves_the_rest_of_the_blobatar_on_the_seed()
    {
        var options = With(new BlobatarTraits { ["shape"] = 0.95 });
        var a = BlobatarRenderer.Layout("one", options);
        var b = BlobatarRenderer.Layout("two", options);
        Assert.Equal(BlobatarShape.Sun, a.Shape);
        Assert.Equal(BlobatarShape.Sun, b.Shape);
        Assert.NotEqual(a.Body.Rx, b.Body.Rx);
        Assert.NotEqual(a.Palette.Head, b.Palette.Head);
    }

    [Theory]
    [InlineData(0.11, BlobatarShape.Round)]
    [InlineData(0.35, BlobatarShape.Organic)]
    [InlineData(0.54, BlobatarShape.Boxy)]
    [InlineData(0.65, BlobatarShape.Capsule)]
    [InlineData(0.745, BlobatarShape.Nub)]
    [InlineData(0.825, BlobatarShape.Cloud)]
    [InlineData(0.888, BlobatarShape.Droplet)]
    [InlineData(0.933, BlobatarShape.Hexagon)]
    [InlineData(0.965, BlobatarShape.Sun)]
    [InlineData(0.99, BlobatarShape.Triangle)]
    public void Every_shape_in_the_vocabulary_is_reachable_by_band_midpoint(double at, BlobatarShape shape)
    {
        Assert.Equal(shape, BlobatarRenderer.Layout(Seed, With(new BlobatarTraits { ["shape"] = at })).Shape);
        Assert.Equal(at, shape.ToTraitPosition());
        Assert.Equal(shape, BlobatarRenderer.ShapeOf(Seed, With(new BlobatarTraits().PinShape(shape))));
    }

    [Fact]
    public void Pinning_every_trait_makes_the_seed_irrelevant()
    {
        var full = BlobatarTraits.Fixed(0.42);
        var a = BlobatarRenderer.Render("one", With(full));
        var b = BlobatarRenderer.Render("two", With(full));
        Assert.Equal(a, b);
        Assert.Contains("<path", a, StringComparison.Ordinal);
    }

    [Fact]
    public void Hue_and_tone_win_over_the_traits_that_back_them()
    {
        var withBoth = BlobatarRenderer.Layout(Seed, new BlobatarOptions { Hue = 200, Tone = 0.9, Traits = new BlobatarTraits { ["hue"] = 0, ["tone"] = 0 } });
        var friendly = BlobatarRenderer.Layout(Seed, new BlobatarOptions { Hue = 200, Tone = 0.9 });
        Assert.Equal(friendly.Palette, withBoth.Palette);
    }

    [Fact]
    public void An_override_at_the_very_top_of_its_range_is_clamped_not_wrapped()
    {
        var options = With(new BlobatarTraits { ["shape"] = 0.14, ["body.r"] = 1, ["eyes"] = 0.999999 });
        Assert.Equal(BlobatarShape.Round, BlobatarRenderer.Layout(Seed, options).Shape);
        Assert.Contains("<path", BlobatarRenderer.Render(Seed, options), StringComparison.Ordinal);
    }

    [Fact]
    public void Tone_pins_select_six_distinct_palettes()
    {
        var heads = Enum.GetValues<BlobatarTone>()
            .Select(tone => BlobatarRenderer.Layout(Seed, new BlobatarOptions { Tone = tone.ToPosition() }).Palette.Head)
            .ToHashSet();
        Assert.Equal(6, heads.Count);
    }

    [Fact]
    public void Trait_maps_compare_by_content()
    {
        var a = new BlobatarTraits { ["shape"] = 0.95, ["tone"] = new[] { 0.1, 0.49 } };
        var b = new BlobatarTraits().Pin("shape", 0.95).Narrow("tone", 0.1, 0.49);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, new BlobatarTraits { ["shape"] = 0.95 });
        Assert.Equal(new BlobatarOptions { Traits = a }, new BlobatarOptions { Traits = b });
    }

    [Fact]
    public void The_key_list_matches_upstream()
    {
        string[] expected =
        [
            "shape", "hue", "tone", "body.r", "body.ratio", "body.x", "body.y", "body.n", "body.rot", "body.pts",
            "body.r0", "body.r1", "body.r2", "body.r3", "body.r4", "body.r5", "body.r6", "body.r7",
            "gaze.x", "gaze.y", "eye.rx", "eye.ratio", "eye.scale", "eye.stretch", "eye.gap", "eye.n", "eye.lean", "eye.lean2", "eye.dy",
            "sun.n", "sun.dist", "sun.r", "sun.rot", "cloud.n", "cloud.r0", "cloud.r1", "cloud.r2", "cloud.r3", "cloud.r4", "cloud.r5",
            "nub.n", "nub.a0", "nub.a1", "nub.r0", "nub.r1", "poly.round", "capsule.squat", "droplet.tip",
        ];
        Assert.Equal(expected, BlobatarTraitKey.All);
    }
}
