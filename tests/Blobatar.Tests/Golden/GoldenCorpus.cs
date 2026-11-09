using System.Globalization;

namespace Blobatar.Tests.Golden;

/// <summary>
/// What generation 2's golden fixture covers, ported from upstream's <c>test/golden/corpus.ts</c>: the
/// seed templates, the option sweep, the labelled renders, and the shape histogram. The contract is
/// name → markup, so bands, geometry ranges, tone edges, expression geometry and every rendering option
/// below are all pinned by it.
/// </summary>
internal static class GoldenCorpus
{
    private static readonly Func<int, string>[] Templates =
    [
        i => Invariant($"user-{i}"),
        i => Invariant($"alain{i}@example.com"),
        i => string.Concat(Enumerable.Repeat(unchecked((uint)(i * 2654435761L)).ToString("x8", CultureInfo.InvariantCulture), 4)),
        i => Invariant($"Team Rocket {i}"),
        i => Invariant($"café-{i}"),
        i => Invariant($"Ünïcødé {i}"),
        i => Invariant($"\U0001F98A{i}\U0001F331"),
        i => Invariant($"{i}"),
        i => Invariant($"00000000-0000-4000-8000-{i:D12}"),
        i => Invariant($"  Mixed Case {i}  "),
    ];

    public static IReadOnlyList<string> Seeds { get; } =
        Enumerable.Range(0, 1000).Select(i => Templates[i % Templates.Length](i)).ToArray();

    private static IEnumerable<string> SweepSeeds => Seeds.Take(12);

    private static readonly (string Label, BlobatarOptions Options)[] OptionCases =
    [
        ("bg:none", new BlobatarOptions { Background = BlobatarBackground.None }),
        ("bg:square", new BlobatarOptions { Background = BlobatarBackground.Square }),
        ("bg:circle", new BlobatarOptions { Background = BlobatarBackground.Circle }),
        ("bg:squircle", new BlobatarOptions { Background = BlobatarBackground.Squircle }),
        ("hue:0", new BlobatarOptions { Hue = 0 }),
        ("hue:210", new BlobatarOptions { Hue = 210 }),
        ("hue:359", new BlobatarOptions { Hue = 359 }),
        ("tone:0", new BlobatarOptions { Tone = 0 }),
        ("tone:0.5", new BlobatarOptions { Tone = 0.5 }),
        ("tone:0.999", new BlobatarOptions { Tone = 0.999 }),
        ("size:8", new BlobatarOptions { Size = 8 }),
        ("size:1024", new BlobatarOptions { Size = 1024 }),
        ("normalize:false", new BlobatarOptions { Normalize = false }),
        .. Enum.GetValues<BlobatarExpression>().Select(e => ("expression:" + e.ToName(), new BlobatarOptions { Expression = e })),
    ];

    /// <summary>Midpoints of gen2's bands, duplicated deliberately to catch moved bands.</summary>
    private static readonly (string Shape, double At)[] Shapes =
    [
        ("round", 0.11),
        ("organic", 0.35),
        ("boxy", 0.54),
        ("capsule", 0.65),
        ("nub", 0.745),
        ("cloud", 0.825),
        ("droplet", 0.888),
        ("hexagon", 0.933),
        ("sun", 0.965),
        ("triangle", 0.99),
    ];

    /// <summary>The labelled renders recorded verbatim in the fixture.</summary>
    public static IEnumerable<(string Label, string Svg)> Markup()
    {
        foreach (var (shape, at) in Shapes)
        {
            yield return ("shape:" + shape, BlobatarRenderer.Render("alain", new BlobatarOptions { Traits = new BlobatarTraits { ["shape"] = at } }));
        }

        yield return ("plain", BlobatarRenderer.Render("alain"));
        yield return ("backdrop", BlobatarRenderer.Render("alain", new BlobatarOptions { Background = BlobatarBackground.Squircle }));
        yield return ("posed", BlobatarRenderer.Render("alain", new BlobatarOptions { Expression = BlobatarExpression.Happy }));
        yield return ("tinted", BlobatarRenderer.Render("alain", new BlobatarOptions { Expression = BlobatarExpression.Mad }));
        yield return ("titled", BlobatarRenderer.Render("alain", new BlobatarOptions { Title = "Alain" }));
        yield return ("sized", BlobatarRenderer.Render("alain", new BlobatarOptions { Size = 64 }));
        yield return ("configured", BlobatarRenderer.Render("alain", new BlobatarOptions { Traits = new BlobatarTraits { ["shape"] = 0.96, ["eye.ratio"] = 0, ["hue"] = 0.5 } }));
        yield return ("astral", BlobatarRenderer.Render("\U0001F98A"));
    }

    /// <summary>Every seed, then every sweep seed under every option case, labelled as the fixture labels them.</summary>
    public static IEnumerable<(string Label, string Svg)> Cases()
    {
        foreach (var seed in Seeds)
        {
            yield return (seed, BlobatarRenderer.Render(seed));
        }

        foreach (var seed in SweepSeeds)
        {
            foreach (var (label, options) in OptionCases)
            {
                yield return (seed + "\0" + label, BlobatarRenderer.Render(seed, options));
            }
        }
    }

    public const int HistogramSeeds = 20_000;

    /// <summary>Shape counts over 20 000 seeds, sorted by name.</summary>
    public static IEnumerable<(string Shape, int Count)> Histogram()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < HistogramSeeds; i++)
        {
            var shape = BlobatarRenderer.ShapeOf(Invariant($"histogram-{i}")).ToName();
            counts[shape] = counts.GetValueOrDefault(shape) + 1;
        }

        return counts.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => (kv.Key, kv.Value));
    }

    private static string Invariant(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
