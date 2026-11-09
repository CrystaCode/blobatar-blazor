using Xunit;

namespace Blobatar.Tests.Golden;

/// <summary>Generation 2's frozen name → markup contract. A failure here is a port bug, never a fixture update.</summary>
public class GoldenTests
{
    private static readonly GoldenFixture Fixture = GoldenFixture.Load();

    [Fact]
    public void The_shape_distribution_is_unchanged()
    {
        var counts = GoldenCorpus.Histogram().ToArray();
        Assert.Equal(Fixture.Histogram.Keys.ToArray(), counts.Select(c => c.Shape).ToArray());
        foreach (var (shape, n) in counts)
        {
            Assert.Equal($"{shape} {Fixture.Histogram[shape]}", $"{shape} {n}");
        }

        Assert.Equal(GoldenCorpus.HistogramSeeds, counts.Sum(c => c.Count));
    }

    [Fact]
    public void The_recorded_renders_are_byte_identical()
    {
        var now = GoldenCorpus.Markup().ToArray();
        Assert.Equal(Fixture.Markup.Keys.ToArray(), now.Select(m => m.Label).ToArray());
        foreach (var (label, svg) in now)
        {
            var expected = Fixture.Markup[label];
            Assert.True(expected == svg, Describe(label, expected, svg));
        }
    }

    [Fact]
    public void Every_seed_and_option_combination_still_hashes_the_same()
    {
        var moved = new List<string>();
        var unrecorded = new List<string>();
        var total = 0;
        foreach (var (label, svg) in GoldenCorpus.Cases())
        {
            total++;
            if (!Fixture.Hashes.TryGetValue(label, out var was))
            {
                unrecorded.Add(label);
            }
            else if (was != GoldenFixture.Hash(svg))
            {
                moved.Add(label);
            }
        }

        Assert.True(unrecorded.Count == 0, $"not in the fixture: {Report(unrecorded, total)}");
        Assert.True(moved.Count == 0, $"gen2 moved: {Report(moved, total)}");
        Assert.Equal(total, Fixture.Hashes.Count);
    }

    private static string Report(List<string> labels, int total) =>
        $"{labels.Count} of {total}: {string.Join(", ", labels.Take(8).Select(l => l.Replace("\0", "␀", StringComparison.Ordinal)))}{(labels.Count > 8 ? ", …" : string.Empty)}";

    private static string Describe(string label, string expected, string actual)
    {
        var i = 0;
        while (i < expected.Length && i < actual.Length && expected[i] == actual[i])
        {
            i++;
        }

        var from = Math.Max(0, i - 40);
        return $"{label} differs at index {i}:\n  expected …{expected.Substring(from, Math.Min(100, expected.Length - from))}\n  actual   …{actual.Substring(from, Math.Min(100, actual.Length - from))}";
    }
}
