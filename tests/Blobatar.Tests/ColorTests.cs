using Blobatar.Internal;
using Xunit;

namespace Blobatar.Tests;

/// <summary>Ported from upstream <c>test/color.test.ts</c>: the contrast guarantee, at every hue.</summary>
public class ColorTests
{
    private static readonly int[] Hues = Enumerable.Range(0, 360).ToArray();
    private static readonly double[] Tones = [0.1, 0.3, 0.5, 0.7, 0.88, 0.97];
    private static readonly Oklch Ground = new(0.145, 0, 0);

    private static readonly (string Name, Tint Tint)[] Tints =
    [
        ("hot", Tint.Hot), ("rose", Tint.Rose), ("blush", Tint.Blush), ("bile", Tint.Bile),
    ];

    private static Oklch Slot(OklchRamp r, string slot) => slot switch
    {
        "bg" => r.Background,
        "head" => r.Head,
        "eye" => r.Eye,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };

    [Fact]
    public void Every_floor_clears_across_every_hue()
    {
        foreach (var (fg, bg, min) in ColorRamp.Floors)
        {
            foreach (var h in Hues)
            {
                var r = ColorRamp.Ramp(h);
                Assert.True(ColorRamp.Contrast(Slot(r, fg), Slot(r, bg)) >= min - 1e-9, $"{fg}/{bg} at hue {h}");
            }
        }
    }

    [Fact]
    public void The_authored_ramp_clears_the_floors_before_enforcement_runs()
    {
        foreach (var h in Hues)
        {
            var r = ColorRamp.Ramp(h, enforceContrast: false);
            foreach (var (fg, bg, min) in ColorRamp.Floors)
            {
                Assert.True(ColorRamp.Contrast(Slot(r, fg), Slot(r, bg)) >= min, $"{fg}/{bg} at hue {h}");
            }
        }
    }

    [Fact]
    public void Every_hue_resolves_to_a_valid_6_digit_hex()
    {
        foreach (var h in Hues)
        {
            var p = ColorRamp.Palette(h);
            foreach (var hex in new[] { p.Background, p.Head, p.Eye })
            {
                Assert.Matches("^#[0-9a-f]{6}$", hex);
            }
        }
    }

    [Fact]
    public void Hues_stay_distinguishable_rather_than_collapsing_to_grey() =>
        Assert.True(Hues.Select(h => ColorRamp.Palette(h).Head).ToHashSet().Count > 200);

    [Fact]
    public void Real_seeds_all_satisfy_the_guarantee()
    {
        for (var i = 0; i < 500; i++)
        {
            var r = ColorRamp.Ramp(TraitReader.For($"user-{i}").Num("hue", 0, 360));
            foreach (var (fg, bg, min) in ColorRamp.Floors)
            {
                Assert.True(ColorRamp.Contrast(Slot(r, fg), Slot(r, bg)) >= min - 1e-9);
            }
        }
    }

    [Theory]
    [InlineData("#000000", "#000000")]
    [InlineData("#0a0a0b", "#0a0a0b")]
    [InlineData("#ffffff", "#ffffff")]
    [InlineData("#F5F5F5", "#f5f5f5")]
    [InlineData("#3b82f6", "#3b82f6")]
    [InlineData("#888", "#888888")]
    public void The_floors_hold_against_a_plate_at_every_hue_and_tone(string bg, string full)
    {
        var plate = ColorRamp.FromHex(full);
        foreach (var h in Hues)
        {
            foreach (var tone in Tones)
            {
                var r = ColorRamp.Ramp(h, true, tone, bg);
                Assert.True(ColorRamp.Contrast(r.Head, plate) >= 1.25 - 1e-6, $"head/plate {bg} hue {h} tone {tone}");
                Assert.True(ColorRamp.Contrast(r.Eye, r.Head) >= 4.5 - 1e-9, $"eye/head {bg} hue {h} tone {tone}");
            }
        }

        // The plate is measured as a grey of the same luminance.
        Assert.InRange(ColorRamp.Contrast(ColorRamp.Ramp(0, true, 0, bg).Background, plate), 0.999, 1.001);
    }

    [Fact]
    public void A_plate_comes_back_out_of_the_palette_as_given() =>
        Assert.Equal("#F5F5F5", ColorRamp.Palette(0, true, 0, "#F5F5F5").Background);

    [Fact]
    public void A_plate_the_ramp_already_clears_changes_nothing_else()
    {
        foreach (var h in Hues)
        {
            var plain = ColorRamp.Palette(h, true, 0.5);
            var plated = ColorRamp.Palette(h, true, 0.5, "#ffffff");
            Assert.Equal((plain.Head, plain.Eye), (plated.Head, plated.Eye));
        }
    }

    [Theory]
    [InlineData("var(--plate)")]
    [InlineData("rebeccapurple")]
    [InlineData("#12345678")]
    [InlineData("rgb(0 0 0)")]
    public void An_unmeasurable_colour_is_painted_as_given_and_moves_nothing(string bg)
    {
        Assert.Equal(ColorRamp.Ramp(0, true, 0.97), ColorRamp.Ramp(0, true, 0.97, bg));
        Assert.Equal(bg, ColorRamp.Palette(0, true, 0.97, bg).Background);
    }

    [Fact]
    public void Contrast_false_leaves_the_body_alone()
    {
        foreach (var h in Hues)
        {
            Assert.Equal(ColorRamp.Ramp(h, false, 0.97).Head, ColorRamp.Ramp(h, false, 0.97, "#000000").Head);
        }
    }

    [Fact]
    public void Eye_polarity_follows_the_body_across_every_tone()
    {
        foreach (var h in Hues)
        {
            foreach (var tone in Tones)
            {
                var r = ColorRamp.Ramp(h, true, tone);
                Assert.Equal(r.Head.L >= 0.5, r.Eye.L < 0.5);
                Assert.True(ColorRamp.Contrast(r.Eye, r.Head) >= 4.5 - 1e-9);
            }
        }
    }

    [Fact]
    public void The_tone_set_spans_pale_to_dark()
    {
        var ls = Tones.Select(t => ColorRamp.Ramp(0, false, t).Head.L).ToArray();
        Assert.True(ls.Min() < 0.4);
        Assert.True(ls.Max() > 0.85);
        Assert.Equal(Tones.Length, ls.Distinct().Count());
    }

    [Fact]
    public void Every_tone_stays_visible_on_a_dark_host_surface()
    {
        foreach (var h in Hues)
        {
            foreach (var tone in Tones)
            {
                Assert.True(ColorRamp.Contrast(ColorRamp.Ramp(h, true, tone).Head, Ground) >= 1.5 - 1e-9, $"hue {h} tone {tone}");
            }
        }
    }

    [Fact]
    public void Pale_tones_survive_enforcement_rather_than_being_darkened_away()
    {
        foreach (var h in Hues)
        {
            Assert.True(ColorRamp.Ramp(h, true, 0.3).Head.L > 0.85);
        }
    }

    [Fact]
    public void The_eye_clears_the_body_at_every_heat_hue_tone_and_target()
    {
        foreach (var (name, tint) in Tints)
        {
            foreach (var h in Hues)
            {
                foreach (var tone in Tones)
                {
                    var p = ColorRamp.Palette(h, true, tone);
                    var (hotHead, hotEye) = ColorRamp.Tinted(p.Head!, p.Eye!, tint);
                    for (var i = 0; i <= 10; i++)
                    {
                        var t = i / 10.0;
                        var contrast = ColorRamp.Contrast(
                            ColorRamp.FromHex(ColorRamp.MixHex(p.Eye!, hotEye, t)),
                            ColorRamp.FromHex(ColorRamp.MixHex(p.Head!, hotHead, t)));
                        Assert.True(contrast >= 4.5, $"{name} hue {h} tone {tone} heat {t}: {contrast}");
                        Assert.True(ColorRamp.Contrast(ColorRamp.FromHex(ColorRamp.MixHex(p.Head!, hotHead, t)), Ground) >= 1.5, $"{name} hue {h} tone {tone} heat {t} on dark");
                    }
                }
            }
        }
    }

    [Fact]
    public void Heat_0_is_the_palette_untouched()
    {
        foreach (var (name, tint) in Tints)
        {
            foreach (var h in Hues)
            {
                var p = ColorRamp.Palette(h);
                var (hotHead, hotEye) = ColorRamp.Tinted(p.Head!, p.Eye!, tint);
                Assert.True(p.Head == ColorRamp.MixHex(p.Head!, hotHead, 0), name);
                Assert.True(p.Eye == ColorRamp.MixHex(p.Eye!, hotEye, 0), name);
            }
        }
    }

    [Fact]
    public void The_tone_set_survives_the_trip_rather_than_collapsing_onto_one_colour()
    {
        foreach (var (name, tint) in Tints)
        {
            var heads = Tones.Select(t =>
            {
                var p = ColorRamp.Palette(200, true, t);
                return ColorRamp.FromHex(ColorRamp.Tinted(p.Head!, p.Eye!, tint).Head).L;
            }).ToArray();
            Assert.Equal(Tones.Length, heads.Select(l => Math.Round(l, 3)).Distinct().Count());
            Assert.True(heads.Max() - heads.Min() > 0.2, name);
        }
    }

    [Fact]
    public void A_tinted_body_actually_arrives_at_its_target_hue()
    {
        foreach (var (name, tint) in Tints)
        {
            foreach (var tone in Tones)
            {
                foreach (var h in Hues)
                {
                    var p = ColorRamp.Palette(h, true, tone);
                    var c = ColorRamp.FromHex(ColorRamp.Tinted(p.Head!, p.Eye!, tint).Head);
                    var off = Math.Abs(((c.H - tint.H + 540) % 360) - 180);
                    Assert.True(off < 6, $"{name} hue {h} tone {tone}: off by {off}");
                    Assert.True(c.C > 0.06, $"{name} hue {h} tone {tone}: chroma {c.C}");
                }
            }
        }
    }

    [Fact]
    public void EnsureContrast_rescues_a_pair_that_starts_far_too_close()
    {
        var bg = new Oklch(0.6, 0.1, 240);
        var fg = new Oklch(0.62, 0.1, 240);
        Assert.True(ColorRamp.Contrast(fg, bg) < 1.2);
        Assert.True(ColorRamp.Contrast(ColorRamp.EnsureContrast(fg, bg, 4.5), bg) >= 4.5);
    }

    [Fact]
    public void EnsureContrast_keeps_the_direction_it_is_already_leaning()
    {
        var bg = new Oklch(0.9, 0.05, 30);
        var fg = new Oklch(0.85, 0.05, 30);
        Assert.True(ColorRamp.EnsureContrast(fg, bg, 4.5).L < fg.L);
    }

    [Fact]
    public void EnsureContrast_flips_direction_when_the_lean_runs_out_of_range()
    {
        var bg = new Oklch(0.05, 0.02, 30);
        var fg = new Oklch(0.04, 0.02, 30);
        Assert.True(ColorRamp.Contrast(ColorRamp.EnsureContrast(fg, bg, 7), bg) >= 7);
    }

    [Fact]
    public void EnsureContrast_leaves_an_already_passing_pair_untouched()
    {
        var bg = new Oklch(0.95, 0.02, 30);
        var fg = new Oklch(0.1, 0.02, 30);
        Assert.Equal(fg, ColorRamp.EnsureContrast(fg, bg, 4.5));
    }

    [Fact]
    public void Hex_round_trips_through_oklch()
    {
        foreach (var hex in new[] { "#000000", "#ffffff", "#d0d897", "#0f1006", "#3b82f6" })
        {
            Assert.Equal(hex, ColorRamp.ToHex(ColorRamp.FromHex(hex)));
        }
    }
}
