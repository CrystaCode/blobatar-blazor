using System.Security.Cryptography;
using System.Text;

namespace Blobatar.Tests.Golden;

/// <summary>
/// Upstream's <c>test/golden/gen2.txt</c>, parsed the way <c>test/golden/format.ts</c> parses it: three
/// tab-separated sections (<c>histogram</c>, <c>markup</c>, <c>hashes</c>), comments and blanks skipped,
/// the first tab on a line separating key from value.
/// </summary>
internal sealed class GoldenFixture
{
    private GoldenFixture(Dictionary<string, Dictionary<string, string>> sections)
    {
        Histogram = sections["histogram"];
        Markup = sections["markup"];
        Hashes = sections["hashes"];
    }

    public Dictionary<string, string> Histogram { get; }

    public Dictionary<string, string> Markup { get; }

    public Dictionary<string, string> Hashes { get; }

    public static GoldenFixture Load()
    {
        using var stream = typeof(GoldenFixture).Assembly.GetManifestResourceStream("gen2.txt")
            ?? throw new InvalidOperationException("gen2.txt is not embedded.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();

        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
        {
            ["histogram"] = new(StringComparer.Ordinal),
            ["markup"] = new(StringComparer.Ordinal),
            ["hashes"] = new(StringComparer.Ordinal),
        };

        Dictionary<string, string>? current = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.EndsWith('\r') ? raw[..^1] : raw;
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('['))
            {
                current = sections[line[1..^1]];
                continue;
            }

            if (current is null)
            {
                throw new InvalidOperationException($"fixture line outside any section: {line}");
            }

            var tab = line.IndexOf('\t', StringComparison.Ordinal);
            if (tab < 0)
            {
                throw new InvalidOperationException($"fixture line has no tab: {line}");
            }

            current[line[..tab]] = line[(tab + 1)..];
        }

        return new GoldenFixture(sections);
    }

    /// <summary>16 hex characters of SHA-256 over the markup, as upstream's tripwire computes it.</summary>
    public static string Hash(string markup)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(markup));
        return Convert.ToHexString(digest).ToLowerInvariant()[..16];
    }
}
