using System.Text;
using Blobatar.Internal;

namespace Blobatar;

/// <summary>
/// Seed hashing: turns a name into the 32-bit state every trait is drawn from.
/// </summary>
/// <remarks>
/// <para>Two guarantees this type exists to provide, inherited from upstream:</para>
/// <list type="number">
/// <item><description><b>Avalanche.</b> <c>"alain"</c> and <c>"alaim"</c> produce visually unrelated
/// blobatars. The murmur3 finalizer gives every trait full avalanche over the seed.</description></item>
/// <item><description><b>Streaming.</b> The seed is hashed once; each trait key continues from that state.
/// Trait values are therefore independent of one another, so a trait added in a later version cannot
/// disturb an existing blobatar.</description></item>
/// </list>
/// <para>Hashing runs over UTF-8 bytes, so non-ASCII and astral-plane names behave consistently.</para>
/// </remarks>
public static class SeedHash
{
    private const byte Separator = 0xff;

    /// <summary>
    /// Normalizes a seed so that inputs a human considers equal hash equally: NFC first, so precomposed
    /// and decomposed accents agree; then trim; then lowercase. <c>Alain@x.com</c> and <c>alain@x.com</c>
    /// are the same person and get the same blobatar.
    /// </summary>
    /// <param name="seed">The raw name.</param>
    /// <returns>The normalized seed, byte-identical to what the JavaScript library would hash.</returns>
    public static string Normalize(string seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        return JsString.ToLowerCase(JsString.Trim(JsString.NormalizeNfc(seed)));
    }

    /// <summary>Hashes a seed once into the reusable state that <see cref="Stream"/> continues from.</summary>
    /// <param name="seed">The name.</param>
    /// <param name="normalize">Whether to apply <see cref="Normalize"/> first. Pass <see langword="false"/> for case-sensitive identifiers.</param>
    public static uint State(string seed, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(seed);
        var s = normalize ? Normalize(seed) : seed;
        return Feed(1779033703u ^ unchecked((uint)s.Length), Encoding.UTF8.GetBytes(s));
    }

    /// <summary>Derives one uniform value in <c>[0, 1)</c> for <paramref name="key"/>, independent of every other key.</summary>
    /// <param name="state">A state from <see cref="State"/>.</param>
    /// <param name="key">The trait key, such as <c>"eye.gap"</c>.</param>
    public static double Stream(uint state, string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var h = Feed(state, [Separator]);
        h = Feed(h, Encoding.UTF8.GetBytes(key));
        return Finalize(h) / 4294967296.0;
    }

    /// <summary>Mixes bytes into a 32-bit state.</summary>
    private static uint Feed(uint h, ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
        {
            h = JsMath.Imul(h ^ b, 3432918353u);
            h = JsMath.RotateLeft(h, 13);
        }

        return h;
    }

    /// <summary>murmur3 fmix32: a bijection on 32-bit values with full avalanche.</summary>
    private static uint Finalize(uint h)
    {
        h = JsMath.Imul(h ^ (h >> 16), 2246822507u);
        h = JsMath.Imul(h ^ (h >> 13), 3266489909u);
        return h ^ (h >> 16);
    }
}
