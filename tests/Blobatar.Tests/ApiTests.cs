using Xunit;

namespace Blobatar.Tests;

/// <summary>The public surface: data URIs, options equality, the adapter seam, and the enum helpers.</summary>
public class ApiTests
{
    [Fact]
    public void A_data_uri_is_the_markup_percent_encoded_with_single_quotes()
    {
        var svg = BlobatarRenderer.Render("alain", new BlobatarOptions { Title = "Alain <3" });
        var uri = BlobatarRenderer.RenderDataUri("alain", new BlobatarOptions { Title = "Alain <3" });
        Assert.StartsWith("data:image/svg+xml,", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("\"", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("<", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("#", uri, StringComparison.Ordinal);
        Assert.Equal(svg.Replace('"', '\''), Uri.UnescapeDataString(uri["data:image/svg+xml,".Length..]));
    }

    [Fact]
    public void Options_compare_by_value()
    {
        var a = new BlobatarOptions { Size = 48, Hue = 200, Traits = new BlobatarTraits { ["shape"] = 0.95 }, Palette = new BlobatarPalette(Head: "#fff") };
        var b = new BlobatarOptions { Size = 48, Hue = 200, Traits = new BlobatarTraits { ["shape"] = 0.95 }, Palette = new BlobatarPalette(Head: "#fff") };
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, b with { Expression = BlobatarExpression.Happy });
    }

    [Fact]
    public void Parts_carry_the_motion_layer_only_when_animating()
    {
        var still = BlobatarRenderer.Parts("alain");
        Assert.Null(still.RootClass);
        Assert.Null(still.Variables);
        Assert.Null(still.Backdrop);
        Assert.DoesNotContain("mo-", still.Inner, StringComparison.Ordinal);

        var moving = BlobatarRenderer.Parts("alain", new BlobatarOptions { Animate = BlobatarAnimation.Hover, Background = BlobatarBackground.Circle, Expression = BlobatarExpression.Mad });
        Assert.Equal("mo-root mo-expr", moving.RootClass);
        Assert.NotNull(moving.Backdrop);
        Assert.Contains("class=\"mo-eyes\"", moving.Inner, StringComparison.Ordinal);
        Assert.Contains("--mo-wrap:-1", moving.Inner, StringComparison.Ordinal);
        var vars = moving.SerializeVariables()!;
        Assert.StartsWith("--mo-phase:-", vars, StringComparison.Ordinal);
        Assert.Contains("--mo-head:#", vars, StringComparison.Ordinal);
        Assert.Contains("--mo-eye:#", vars, StringComparison.Ordinal);
        Assert.Contains("--mo-esx:1.85", vars, StringComparison.Ordinal);
        Assert.Contains("--mo-shake:0.55", vars, StringComparison.Ordinal);
        Assert.DoesNotContain("--mo-heat", vars, StringComparison.Ordinal);
        Assert.Contains("--mo-saccade-phase:-", vars, StringComparison.Ordinal);
    }

    [Fact]
    public void The_tinted_fills_in_the_variables_match_the_static_render()
    {
        var moving = BlobatarRenderer.Parts("alain", new BlobatarOptions { Animate = BlobatarAnimation.Always, Expression = BlobatarExpression.Mad });
        var head = moving.Variables!.First(kv => kv.Key == "--mo-head").Value;
        var layout = BlobatarRenderer.Layout("alain", new BlobatarOptions { Expression = BlobatarExpression.Mad });
        Assert.Equal(layout.Palette.Head, head);
        // The markup itself keeps the base fill: the stylesheet paints the tint from the variable.
        Assert.Contains("fill=\"" + BlobatarRenderer.Layout("alain").Palette.Head + "\"", moving.Inner, StringComparison.Ordinal);
    }

    [Fact]
    public void The_backdrop_takes_the_background_colour_and_keeps_the_contrast_guarantee()
    {
        // A pale body (tone 0.3) sits under 1.25:1 against pure white, so the plate has to push it.
        var plain = BlobatarRenderer.Layout("alain", new BlobatarOptions { Background = BlobatarBackground.Circle, Tone = 0.3 });
        var white = BlobatarRenderer.Layout("alain", new BlobatarOptions { Background = BlobatarBackground.Circle, Tone = 0.3, BackgroundColor = "#ffffff" });
        var none = BlobatarRenderer.Layout("alain", new BlobatarOptions { Tone = 0.3, BackgroundColor = "#ffffff" });
        Assert.Equal("#ffffff", white.Palette.Background);
        Assert.NotEqual(plain.Palette.Head, white.Palette.Head);
        Assert.Equal(plain.Palette with { Background = BlobatarRenderer.Layout("alain", new BlobatarOptions { Tone = 0.3 }).Palette.Background }, none.Palette);
        Assert.Contains("fill=\"#000000\"", BlobatarRenderer.Render("alain", new BlobatarOptions { Background = BlobatarBackground.Square, BackgroundColor = "#000000" }), StringComparison.Ordinal);
    }

    [Fact]
    public void A_palette_override_wins_over_everything()
    {
        var svg = BlobatarRenderer.Render("alain", new BlobatarOptions { Background = BlobatarBackground.Square, BackgroundColor = "#000000", Palette = new BlobatarPalette("#111111", "#222222", "#333333") });
        Assert.Contains("fill=\"#111111\"", svg, StringComparison.Ordinal);
        Assert.Contains("fill=\"#222222\"", svg, StringComparison.Ordinal);
        Assert.Contains("fill=\"#333333\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void The_title_is_escaped_and_the_size_emitted()
    {
        var svg = BlobatarRenderer.Render("alain", new BlobatarOptions { Title = "A & B <c>", Size = 64 });
        Assert.Contains("<title>A &amp; B &lt;c&gt;</title>", svg, StringComparison.Ordinal);
        Assert.Contains(" width=\"64\" height=\"64\"", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<title>", BlobatarRenderer.Render("alain", new BlobatarOptions { Title = "" }), StringComparison.Ordinal);
    }

    [Fact]
    public void Enum_helpers_round_trip_their_names()
    {
        foreach (var s in Enum.GetValues<BlobatarShape>())
        {
            Assert.Equal(s, BlobatarShapeExtensions.Parse(s.ToName()));
        }

        foreach (var b in Enum.GetValues<BlobatarBackground>())
        {
            Assert.Equal(b, BlobatarBackgroundExtensions.Parse(b.ToName()));
        }

        foreach (var t in Enum.GetValues<BlobatarTone>())
        {
            Assert.Equal(t, BlobatarToneExtensions.FromPosition(t.ToPosition()));
        }

        Assert.Equal(BlobatarTone.Pastel, BlobatarToneExtensions.FromPosition(1));
        Assert.Null(BlobatarAnimation.None.ToName());
        Assert.Equal("always", BlobatarAnimation.Always.ToName());
        Assert.False(BlobatarShapeExtensions.TryParse("blob", out _));
    }

    [Fact]
    public void Trait_overrides_convert_from_numbers_and_arrays()
    {
        TraitOverride pin = 0.5;
        TraitOverride list = new[] { 0.1, 0.2 };
        Assert.False(pin.IsList);
        Assert.Equal(0.5, pin.Position);
        Assert.True(list.IsList);
        Assert.Equal(new[] { 0.1, 0.2 }, list.Positions);
        Assert.Equal(pin, TraitOverride.Pin(0.5));
        Assert.Equal(list, TraitOverride.Narrow(0.1, 0.2));
        Assert.NotEqual(pin, list);
    }

    [Fact]
    public void Shape_of_agrees_with_the_full_layout()
    {
        for (var i = 0; i < 500; i++)
        {
            var name = $"user-{i}";
            Assert.Equal(BlobatarRenderer.Layout(name).Shape, BlobatarRenderer.ShapeOf(name));
        }
    }

    [Fact]
    public void Null_names_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => BlobatarRenderer.Render(null!));
        Assert.Throws<ArgumentNullException>(() => BlobatarRenderer.Layout(null!));
    }
}
