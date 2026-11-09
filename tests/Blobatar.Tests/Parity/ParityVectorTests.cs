using System.Text.Json;
using Blobatar.Internal;
using Xunit;

namespace Blobatar.Tests.Parity;

/// <summary>
/// The cross-language parity gate, against the reference vectors upstream exports for its Flutter port
/// (<c>packages/flutter/test/fixtures/reference-vectors.json</c>, generated from the TypeScript core).
/// Everything the fixture marks exact must match exactly; trig-derived layout floats are compared under
/// the fixture's documented relative tolerance of 1e-9.
/// </summary>
public class ParityVectorTests
{
    private static readonly JsonDocument Vectors = Load();

    private static JsonDocument Load()
    {
        using var stream = typeof(ParityVectorTests).Assembly.GetManifestResourceStream("reference-vectors.json")
            ?? throw new InvalidOperationException("reference-vectors.json is not embedded.");
        return JsonDocument.Parse(stream);
    }

    /// <summary>JavaScript keeps the state as a signed 32-bit integer; the port exposes the same bits unsigned.</summary>
    private static uint StateOf(JsonElement vector) => unchecked((uint)vector.GetProperty("state").GetInt64());

    private static bool CloseEnough(double a, double b) =>
        a == b || Math.Abs(a - b) <= 1e-9 * Math.Max(1, Math.Max(Math.Abs(a), Math.Abs(b)));

    [Fact]
    public void Normalization_agrees_byte_for_byte()
    {
        foreach (var v in Vectors.RootElement.GetProperty("hash").EnumerateArray())
        {
            var seed = v.GetProperty("seed").GetString()!;
            Assert.True(v.GetProperty("normalized").GetString() == SeedHash.Normalize(seed), $"normalize({Show(seed)})");
        }
    }

    [Fact]
    public void Seed_states_agree_exactly()
    {
        foreach (var v in Vectors.RootElement.GetProperty("hash").EnumerateArray())
        {
            var seed = v.GetProperty("seed").GetString()!;
            Assert.True(StateOf(v) == SeedHash.State(seed), $"state({Show(seed)})");
        }
    }

    [Fact]
    public void Trait_streams_agree_exactly()
    {
        foreach (var v in Vectors.RootElement.GetProperty("hash").EnumerateArray())
        {
            var seed = v.GetProperty("seed").GetString()!;
            var state = StateOf(v);
            foreach (var stream in v.GetProperty("streams").EnumerateObject())
            {
                Assert.True(stream.Value.GetDouble() == SeedHash.Stream(state, stream.Name), $"stream({Show(seed)}, {stream.Name})");
            }
        }
    }

    [Fact]
    public void Override_reads_agree_exactly()
    {
        foreach (var v in Vectors.RootElement.GetProperty("overrides").EnumerateArray())
        {
            var seed = v.GetProperty("seed").GetString()!;
            var traits = TraitsFrom(v.GetProperty("overrides"));
            var t = TraitReader.For(seed, true, traits);
            foreach (var value in v.GetProperty("values").EnumerateObject())
            {
                Assert.True(value.Value.GetDouble() == t[value.Name], $"{seed} {value.Name}: {t[value.Name]} != {value.Value.GetDouble()}");
            }
        }
    }

    [Fact]
    public void Palette_hex_agrees_exactly_and_ramp_within_tolerance()
    {
        foreach (var v in Vectors.RootElement.GetProperty("palette").EnumerateArray())
        {
            var hue = v.GetProperty("hue").GetDouble();
            var tone = v.GetProperty("tone").GetDouble();
            var hex = v.GetProperty("hex");
            var enforced = ColorRamp.Palette(hue, true, tone);
            Assert.True(hex.GetProperty("bg").GetString() == enforced.Background, $"bg hex h={hue} t={tone}");
            Assert.True(hex.GetProperty("head").GetString() == enforced.Head, $"head hex h={hue} t={tone}");
            Assert.True(hex.GetProperty("eye").GetString() == enforced.Eye, $"eye hex h={hue} t={tone}");

            var raw = v.GetProperty("rampUnenforced");
            var unenforced = ColorRamp.Ramp(hue, false, tone);
            AssertOklch(raw.GetProperty("bg"), unenforced.Background, $"bg h={hue} t={tone}");
            AssertOklch(raw.GetProperty("head"), unenforced.Head, $"head h={hue} t={tone}");
            AssertOklch(raw.GetProperty("eye"), unenforced.Eye, $"eye h={hue} t={tone}");
        }
    }

    [Fact]
    public void The_fixture_covers_every_silhouette_band()
    {
        var counts = Vectors.RootElement.GetProperty("meta").GetProperty("shapeCounts");
        Assert.Equal(10, counts.EnumerateObject().Count());
        foreach (var band in counts.EnumerateObject())
        {
            Assert.True(band.Value.GetInt32() >= 25, $"band {band.Name}");
        }
    }

    [Fact]
    public void All_layout_cases_agree_with_the_TypeScript_core()
    {
        var failures = new List<string>();
        foreach (var c in Vectors.RootElement.GetProperty("cases").EnumerateArray())
        {
            var reason = CheckCase(c, OptionsFrom(c.GetProperty("options")));
            if (reason is not null)
            {
                failures.Add($"seed={c.GetProperty("seed").GetString()} opts={c.GetProperty("options").GetRawText()}: {reason}");
            }
        }

        Assert.True(failures.Count == 0, $"{failures.Count} failures, first: {failures.FirstOrDefault()}");
    }

    [Fact]
    public void Expression_cases_agree_where_the_roster_is_unchanged()
    {
        // The fixture was exported at upstream 2.4.0. A pose retuned since then is not a port bug, so the
        // cases of an expression are only compared when its recorded pose still equals the current one.
        var compared = 0;
        var failures = new List<string>();
        foreach (var expression in Vectors.RootElement.GetProperty("expressions").EnumerateObject())
        {
            var kind = BlobatarExpressionExtensions.Parse(expression.Name);
            if (!PoseMatches(expression.Value.GetProperty("pose"), kind.GetPose()))
            {
                continue;
            }

            foreach (var c in expression.Value.GetProperty("cases").EnumerateArray())
            {
                compared++;
                var options = OptionsFrom(c.GetProperty("options")) with { Expression = kind };
                var reason = CheckCase(c, options);
                if (reason is not null)
                {
                    failures.Add($"{expression.Name} seed={c.GetProperty("seed").GetString()}: {reason}");
                }
            }
        }

        Assert.True(compared > 0, "no expression in the fixture matches the current roster");
        Assert.True(failures.Count == 0, $"{failures.Count} failures, first: {failures.FirstOrDefault()}");
    }

    private static bool PoseMatches(JsonElement recorded, Pose pose)
    {
        var expected = new Pose(
            Get("esx"), Get("esy"), Get("tilt"), Get("edy"), Get("edx"), Get("esx2"), Get("esy2"),
            Get("tilt2"), Get("edy2"), Get("lock"), Get("heat"), Get("shake"), Get("rock"), Get("bdy"));
        return expected == pose;

        double Get(string key) => recorded.TryGetProperty(key, out var p) ? p.GetDouble() : 0;
    }

    private static string? CheckCase(JsonElement m, BlobatarOptions options)
    {
        var seed = m.GetProperty("seed").GetString()!;
        var l = BlobatarRenderer.Layout(seed, options);
        if (l.Shape.ToName() != m.GetProperty("shape").GetString())
        {
            return $"{l.Shape.ToName()} != {m.GetProperty("shape").GetString()}";
        }

        var body = m.GetProperty("body");
        if (!CloseEnough(l.Body.Cx, body.GetProperty("cx").GetDouble())) return "body.cx";
        if (!CloseEnough(l.Body.Cy, body.GetProperty("cy").GetDouble())) return "body.cy";
        if (!CloseEnough(l.Body.Rx, body.GetProperty("rx").GetDouble())) return "body.rx";
        if (!CloseEnough(l.Body.Ry, body.GetProperty("ry").GetDouble())) return "body.ry";
        if (!CloseEnough(l.Body.Squareness, body.GetProperty("n").GetDouble())) return "body.n";
        if (!CloseEnough(l.Body.Rotation, body.GetProperty("rot").GetDouble())) return "body.rot";

        var radii = body.GetProperty("radii").EnumerateArray().Select(r => r.GetDouble()).ToArray();
        if (l.Body.Radii.Count != radii.Length) return "radii.length";
        for (var i = 0; i < radii.Length; i++)
        {
            if (!CloseEnough(l.Body.Radii[i], radii[i])) return $"radii[{i}]";
        }

        if (body.TryGetProperty("sides", out var sides) && l.Body.Sides != sides.GetInt32()) return "sides";
        if (body.TryGetProperty("round", out var round) && !CloseEnough(l.Body.Rounding ?? double.NaN, round.GetDouble())) return "round";

        var face = m.GetProperty("face");
        if (!CloseEnough(l.Face.Cx, face.GetProperty("cx").GetDouble())) return "face.cx";
        if (!CloseEnough(l.Face.Cy, face.GetProperty("cy").GetDouble())) return "face.cy";
        if (!CloseEnough(l.Face.Rx, face.GetProperty("rx").GetDouble())) return "face.rx";
        if (!CloseEnough(l.Face.Ry, face.GetProperty("ry").GetDouble())) return "face.ry";

        var eyes = m.GetProperty("eyes").EnumerateArray().ToArray();
        if (l.Eyes.Count != eyes.Length) return "eyes.length";
        for (var i = 0; i < eyes.Length; i++)
        {
            var e = eyes[i];
            if (!CloseEnough(l.Eyes[i].Cx, e.GetProperty("cx").GetDouble())) return $"eye{i}.cx";
            if (!CloseEnough(l.Eyes[i].Cy, e.GetProperty("cy").GetDouble())) return $"eye{i}.cy";
            if (!CloseEnough(l.Eyes[i].Rx, e.GetProperty("rx").GetDouble())) return $"eye{i}.rx";
            if (!CloseEnough(l.Eyes[i].Ry, e.GetProperty("ry").GetDouble())) return $"eye{i}.ry";
            if (!CloseEnough(l.Eyes[i].Squareness, e.GetProperty("n").GetDouble())) return $"eye{i}.n";
            if (!CloseEnough(l.Eyes[i].Rotation, e.GetProperty("rot").GetDouble())) return $"eye{i}.rot";
        }

        var petals = m.GetProperty("petals").EnumerateArray().ToArray();
        if (l.Petals.Count != petals.Length) return "petals.length";
        for (var i = 0; i < petals.Length; i++)
        {
            var p = petals[i];
            if (!CloseEnough(l.Petals[i].Cx, p.GetProperty("cx").GetDouble())) return $"petals[{i}].cx";
            if (!CloseEnough(l.Petals[i].Cy, p.GetProperty("cy").GetDouble())) return $"petals[{i}].cy";
            if (!CloseEnough(l.Petals[i].Radius, p.GetProperty("r").GetDouble())) return $"petals[{i}].r";
        }

        // Path data is rounded on both sides, so it must match exactly.
        var extra = m.GetProperty("extra").EnumerateArray().Select(x => x.GetString()).ToArray();
        if (l.Extra.Count != extra.Length) return "extra.length";
        for (var i = 0; i < extra.Length; i++)
        {
            if (l.Extra[i] != extra[i]) return $"extra[{i}]";
        }

        if (l.BodyPath != m.GetProperty("bodyPath").GetString()) return "bodyPath";
        var eyePaths = m.GetProperty("eyePaths").EnumerateArray().Select(x => x.GetString()).ToArray();
        for (var i = 0; i < eyePaths.Length; i++)
        {
            var e = l.Eyes[i];
            var actual = PathPrimitives.Superellipse(e.Cx, e.Cy, e.Rx, e.Ry, e.Squareness, e.Rotation);
            if (actual != eyePaths[i]) return $"eyePaths[{i}]: {actual} != {eyePaths[i]}";
        }

        var pal = m.GetProperty("palette");
        if (l.Palette.Background != pal.GetProperty("bg").GetString()) return "palette.bg";
        if (l.Palette.Head != pal.GetProperty("head").GetString()) return "palette.head";
        if (l.Palette.Eye != pal.GetProperty("eye").GetString()) return "palette.eye";
        return null;
    }

    private static BlobatarOptions OptionsFrom(JsonElement options)
    {
        var o = new BlobatarOptions();
        if (options.TryGetProperty("traits", out var traits))
        {
            o = o with { Traits = TraitsFrom(traits) };
        }

        if (options.TryGetProperty("hue", out var hue))
        {
            o = o with { Hue = hue.GetDouble() };
        }

        if (options.TryGetProperty("tone", out var tone))
        {
            o = o with { Tone = tone.GetDouble() };
        }

        if (options.TryGetProperty("contrast", out var contrast))
        {
            o = o with { Contrast = contrast.GetBoolean() };
        }

        if (options.TryGetProperty("normalize", out var normalize))
        {
            o = o with { Normalize = normalize.GetBoolean() };
        }

        return o;
    }

    private static BlobatarTraits TraitsFrom(JsonElement map)
    {
        var traits = new BlobatarTraits();
        foreach (var entry in map.EnumerateObject())
        {
            traits[entry.Name] = entry.Value.ValueKind == JsonValueKind.Array
                ? TraitOverride.Narrow(entry.Value.EnumerateArray().Select(x => x.GetDouble()))
                : TraitOverride.Pin(entry.Value.GetDouble());
        }

        return traits;
    }

    private static void AssertOklch(JsonElement expected, Oklch actual, string what)
    {
        Assert.True(CloseEnough(actual.L, expected.GetProperty("l").GetDouble()), $"{what}.l: {actual.L} != {expected.GetProperty("l").GetDouble()}");
        Assert.True(CloseEnough(actual.C, expected.GetProperty("c").GetDouble()), $"{what}.c: {actual.C} != {expected.GetProperty("c").GetDouble()}");
        Assert.True(CloseEnough(actual.H, expected.GetProperty("h").GetDouble()), $"{what}.h: {actual.H} != {expected.GetProperty("h").GetDouble()}");
    }

    private static string Show(string s) => "\"" + s.Replace("\n", "\\n", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\t", "\\t", StringComparison.Ordinal) + "\"";
}
