namespace Blobatar;

/// <summary>
/// Reads traits for one name: every value is addressed by a string key and drawn from the seed's hash,
/// or from an override when the caller pinned that key.
/// </summary>
/// <remarks>
/// <para>Keys form an append-only namespace: reading a new key in a later version leaves every other
/// trait, and therefore every existing blobatar, untouched. What is <b>not</b> free to change is the
/// contents of a <see cref="Pick{T}"/> list or the numeric range a key is read into, since both are part
/// of the frozen name-to-look mapping of a generation.</para>
/// <para>Overrides are checked per read rather than merged up front: the map is usually absent and tiny
/// when present.</para>
/// </remarks>
public sealed class TraitReader
{
    private readonly BlobatarTraits? _overrides;

    private TraitReader(uint state, BlobatarTraits? overrides)
    {
        State = state;
        _overrides = overrides;
    }

    /// <summary>Creates a reader for a name.</summary>
    /// <param name="seed">The name.</param>
    /// <param name="normalize">Whether to NFC-normalize, trim and lowercase the name first.</param>
    /// <param name="overrides">Trait overrides, or <see langword="null"/> for none.</param>
    public static TraitReader For(string seed, bool normalize = true, BlobatarTraits? overrides = null) =>
        new(SeedHash.State(seed, normalize), overrides);

    /// <summary>Creates a reader over an existing hash state.</summary>
    public static TraitReader FromState(uint state, BlobatarTraits? overrides = null) => new(state, overrides);

    /// <summary>The seed state every key is derived from.</summary>
    public uint State { get; }

    /// <summary>Uniform value in <c>[0, 1)</c> for <paramref name="key"/>.</summary>
    public double this[string key] => Read(key);

    /// <summary>Uniform value in <c>[0, 1)</c> for <paramref name="key"/>.</summary>
    public double Read(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var overridden = _overrides?.Resolve(State, key);
        return overridden ?? SeedHash.Stream(State, key);
    }

    /// <summary>Uniform value in <c>[min, max)</c>.</summary>
    public double Num(string key, double min, double max) => min + Read(key) * (max - min);

    /// <summary>Uniform integer in <c>[min, max]</c>, inclusive at both ends.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Mirrors the upstream reader name; the method draws an integer and says so.")]
    public int Int(string key, int min, int max) => min + (int)Math.Floor(Read(key) * (max - min + 1));

    /// <summary>Uniform choice. Appending to <paramref name="options"/> remaps existing names, so option lists are frozen per generation.</summary>
    public T Pick<T>(string key, IReadOnlyList<T> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options[(int)Math.Floor(Read(key) * options.Count)];
    }

    /// <summary><see langword="true"/> with probability <paramref name="probability"/>.</summary>
    public bool Bool(string key, double probability = 0.5) => Read(key) < probability;

    /// <summary>Symmetric jitter in <c>[-amount, amount)</c>.</summary>
    public double Jitter(string key, double amount) => (Read(key) * 2 - 1) * amount;
}
