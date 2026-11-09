using Xunit;

namespace Blobatar.Tests;

/// <summary>Ported from upstream <c>test/hash.test.ts</c>.</summary>
public class HashTests
{
    private static readonly string[] Keys =
    [
        "hue", "head.y", "head.rx", "head.ry", "head.n", "hair", "hair.lift", "hair.pad",
        "eyes", "eye.y", "eye.gap", "eye.r", "eye.squash", "eye.skew", "ears", "ear.r",
        "brows", "brow.gap", "brow.w", "brow.th", "brow.rot", "mouth", "mouth.y", "mouth.w",
    ];

    private static double[] Vector(string seed)
    {
        var t = TraitReader.For(seed);
        return Keys.Select(k => t[k]).ToArray();
    }

    [Fact]
    public void Same_seed_produces_identical_output() =>
        Assert.Equal(BlobatarRenderer.Render("alain"), BlobatarRenderer.Render("alain"));

    [Fact]
    public void Different_seeds_produce_different_output() =>
        Assert.NotEqual(BlobatarRenderer.Render("alain"), BlobatarRenderer.Render("bob"));

    [Fact]
    public void Case_whitespace_and_NFC_form_are_equivalent()
    {
        var baseline = BlobatarRenderer.Render("Alain@Example.com");
        Assert.Equal(baseline, BlobatarRenderer.Render("alain@example.com"));
        Assert.Equal(baseline, BlobatarRenderer.Render("  ALAIN@EXAMPLE.COM  "));
    }

    [Fact]
    public void Decomposed_and_precomposed_accents_agree()
    {
        var precomposed = "caf" + (char)0x00E9;
        var decomposed = "cafe" + (char)0x0301;
        Assert.Equal(BlobatarRenderer.Render(precomposed), BlobatarRenderer.Render(decomposed));
        Assert.Equal(precomposed, SeedHash.Normalize(decomposed));
    }

    [Fact]
    public void Normalize_false_hashes_the_raw_string()
    {
        var raw = new BlobatarOptions { Normalize = false };
        Assert.NotEqual(BlobatarRenderer.Render("Alain", raw), BlobatarRenderer.Render("alain", raw));
    }

    [Theory]
    [InlineData("日本語")]
    [InlineData("Ελλάδα")]
    [InlineData("\U0001F98A\U0001F43B")]
    [InlineData("أحمد")]
    [InlineData("\U0001F1EB\U0001F1F7")]
    public void Handles_multi_byte_and_astral_plane_seeds(string seed)
    {
        var svg = BlobatarRenderer.Render(seed);
        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("NaN", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void Emoji_differing_only_in_the_low_surrogate_are_distinguished() =>
        Assert.NotEqual(Vector("\U0001F98A"), Vector("\U0001F98B"));

    [Fact]
    public void Astral_seeds_are_stable_across_repeated_calls() =>
        Assert.Equal(BlobatarRenderer.Render("\U0001F98A\U0001F43B"), BlobatarRenderer.Render("\U0001F98A\U0001F43B"));

    [Fact]
    public void A_one_character_change_redraws_most_traits()
    {
        var a = Vector("alain");
        var b = Vector("alaim");
        var moved = a.Where((v, i) => Math.Abs(v - b[i]) > 0.2).Count();
        Assert.True(moved > Keys.Length * 0.6, $"only {moved} of {Keys.Length} traits moved");
    }

    [Fact]
    public void Sequential_seeds_do_not_cluster()
    {
        var buckets = Enumerable.Range(0, 100)
            .Select(i => (int)Math.Floor(TraitReader.For($"user-{i}")["hue"] * 10))
            .ToHashSet();
        Assert.Equal(10, buckets.Count);
    }

    [Fact]
    public void Adding_a_new_trait_key_leaves_existing_keys_untouched()
    {
        var before = Vector("alain");
        var t = TraitReader.For("alain");
        _ = t["freckles"];
        _ = t.Num("freckles.size", 1, 3);
        Assert.Equal(before, Vector("alain"));
    }

    [Fact]
    public void Keys_are_independent_of_read_order()
    {
        var t = TraitReader.For("alain");
        var reversed = Enumerable.Reverse(Enumerable.Reverse(Keys).Select(k => t[k]).ToArray()).ToArray();
        Assert.Equal(Vector("alain"), reversed);
    }

    [Fact]
    public void Floats_stay_in_range_and_cover_the_space()
    {
        var values = Enumerable.Range(0, 5000).Select(i => TraitReader.For($"s{i}")["hue"]).ToArray();
        Assert.True(values.Min() >= 0);
        Assert.True(values.Max() < 1);
        Assert.InRange(values.Average(), 0.45, 0.55);
    }

    [Fact]
    public void Pick_is_uniform_across_options()
    {
        var counts = new int[4];
        for (var i = 0; i < 4000; i++)
        {
            counts[TraitReader.For($"s{i}").Int("eyes", 0, 3)]++;
        }

        Assert.All(counts, c => Assert.True(c > 800));
    }

    [Fact]
    public void Trim_follows_JavaScript_not_dotnet()
    {
        // U+FEFF is JavaScript whitespace; U+0085 is .NET whitespace but not JavaScript's.
        Assert.Equal("a", SeedHash.Normalize((char)0xFEFF + "a" + (char)0xFEFF));
        Assert.Equal("a" + (char)0x0085, SeedHash.Normalize("A" + (char)0x0085));
    }

    [Fact]
    public void Lowercasing_applies_the_full_mapping_where_JavaScript_does()
    {
        // İ (U+0130) lowercases to i + combining dot above, and a word-final capital sigma to ς.
        Assert.Equal("i" + (char)0x0307, SeedHash.Normalize(((char)0x0130).ToString()));
        Assert.Equal((char)0x03BF + "" + (char)0x03C2, SeedHash.Normalize((char)0x039F + "" + (char)0x03A3));
        Assert.Equal(((char)0x03C3).ToString(), SeedHash.Normalize(((char)0x03A3).ToString()));
    }
}
