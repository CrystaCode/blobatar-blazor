using System.Collections;
using System.Globalization;

namespace Blobatar;

/// <summary>
/// Trait overrides: the one configuration seam of a blobatar.
/// </summary>
/// <remarks>
/// <para>Every axis of a blobatar is a named trait (see <see cref="BlobatarTraitKey"/>), and this map pins
/// any of them. Keys you leave out still come from the name, which is the useful middle ground: lock
/// the two things that carry your brand, and every user still gets their own creature.</para>
/// <code>
/// // Always a sun with wide eyes; colour and everything else still per name.
/// var traits = new BlobatarTraits().PinShape(BlobatarShape.Sun).Pin(BlobatarTraitKey.EyeRatio, 0);
///
/// // Round, cloud or sun, never the other seven. Which one is still per name.
/// var house = new BlobatarTraits().NarrowShapes(BlobatarShape.Round, BlobatarShape.Cloud, BlobatarShape.Sun);
///
/// // Collection-initializer form, with raw keys.
/// var raw = new BlobatarTraits { ["shape"] = 0.95, ["eye.ratio"] = 0, ["tone"] = new[] { 0.1, 0.49 } };
/// </code>
/// <para>Pin every trait and the name stops mattering, which is how you build one fixed blobatar:
/// <see cref="Fixed"/> plus any constant name.</para>
/// <para><see cref="BlobatarOptions.Hue"/> and <see cref="BlobatarOptions.Tone"/> state two of these traits in
/// friendlier units and take precedence over the <c>hue</c> and <c>tone</c> entries here.</para>
/// <para>Two maps are equal when they hold the same entries, so a freshly built map with the same content
/// compares equal to the previous one. Treat an instance as immutable once it has been handed to a renderer.</para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Named after the option it carries, as upstream names it; a trait map already reads as a collection.")]
public sealed class BlobatarTraits : IEquatable<BlobatarTraits>, IReadOnlyCollection<KeyValuePair<string, TraitOverride>>
{
    private readonly SortedDictionary<string, TraitOverride> _map = new(StringComparer.Ordinal);

    /// <summary>Creates an empty map: every trait comes from the name.</summary>
    public BlobatarTraits()
    {
    }

    /// <summary>Creates a map from existing entries.</summary>
    public BlobatarTraits(IEnumerable<KeyValuePair<string, TraitOverride>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        foreach (var (key, value) in entries)
        {
            _map[key] = value;
        }
    }

    /// <summary>Pins every known trait key (<see cref="BlobatarTraitKey.All"/>) to one position, so the name no longer matters.</summary>
    public static BlobatarTraits Fixed(double position)
    {
        var traits = new BlobatarTraits();
        foreach (var key in BlobatarTraitKey.All)
        {
            traits._map[key] = position;
        }

        return traits;
    }

    /// <inheritdoc />
    public int Count => _map.Count;

    /// <summary>Gets or sets the override for a key. Setting a key replaces any previous override.</summary>
    public TraitOverride this[string key]
    {
        get => _map[key];
        set
        {
            ArgumentNullException.ThrowIfNull(key);
            _map[key] = value;
        }
    }

    /// <summary>Adds or replaces an override. Enables the <c>{ { "shape", 0.95 } }</c> initializer form.</summary>
    public void Add(string key, TraitOverride value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _map[key] = value;
    }

    /// <summary>Whether <paramref name="key"/> is overridden.</summary>
    public bool ContainsKey(string key) => _map.ContainsKey(key);

    /// <summary>Gets the override for <paramref name="key"/>, if any.</summary>
    public bool TryGetValue(string key, out TraitOverride value) => _map.TryGetValue(key, out value);

    /// <summary>Removes an override, returning the key to the name.</summary>
    public bool Remove(string key) => _map.Remove(key);

    /// <summary>Removes every override.</summary>
    public void Clear() => _map.Clear();

    /// <summary>Pins a trait to one position in <c>[0, 1)</c>.</summary>
    public BlobatarTraits Pin(string key, double position)
    {
        ArgumentNullException.ThrowIfNull(key);
        _map[key] = TraitOverride.Pin(position);
        return this;
    }

    /// <summary>Narrows a trait to a set of positions the name chooses among.</summary>
    public BlobatarTraits Narrow(string key, params double[] positions)
    {
        ArgumentNullException.ThrowIfNull(key);
        _map[key] = TraitOverride.Narrow(positions);
        return this;
    }

    /// <summary>Pins the silhouette.</summary>
    public BlobatarTraits PinShape(BlobatarShape shape) => Pin(BlobatarTraitKey.Shape, shape.ToTraitPosition());

    /// <summary>Narrows the silhouette to the given set; the name picks one of them.</summary>
    public BlobatarTraits NarrowShapes(params BlobatarShape[] shapes)
    {
        ArgumentNullException.ThrowIfNull(shapes);
        var positions = new double[shapes.Length];
        for (var i = 0; i < shapes.Length; i++)
        {
            positions[i] = shapes[i].ToTraitPosition();
        }

        return Narrow(BlobatarTraitKey.Shape, positions);
    }

    /// <summary>Pins the tone swatch.</summary>
    public BlobatarTraits PinTone(BlobatarTone tone) => Pin(BlobatarTraitKey.Tone, tone.ToPosition());

    /// <summary>Narrows the tone to the given swatches; the name picks one of them.</summary>
    public BlobatarTraits NarrowTones(params BlobatarTone[] tones)
    {
        ArgumentNullException.ThrowIfNull(tones);
        var positions = new double[tones.Length];
        for (var i = 0; i < tones.Length; i++)
        {
            positions[i] = tones[i].ToPosition();
        }

        return Narrow(BlobatarTraitKey.Tone, positions);
    }

    /// <summary>The value the trait reader sees for <paramref name="key"/>, or <see langword="null"/> to fall back to the hash.</summary>
    internal double? Resolve(uint state, string key) =>
        _map.TryGetValue(key, out var o) ? o.Resolve(state, key) : null;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, TraitOverride>> GetEnumerator() => _map.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Equals(BlobatarTraits? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_map.Count != other._map.Count)
        {
            return false;
        }

        using var a = _map.GetEnumerator();
        using var b = other._map.GetEnumerator();
        while (a.MoveNext() && b.MoveNext())
        {
            if (!string.Equals(a.Current.Key, b.Current.Key, StringComparison.Ordinal) || a.Current.Value != b.Current.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as BlobatarTraits);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var (key, value) in _map)
        {
            hash.Add(key, StringComparer.Ordinal);
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() =>
        "{ " + string.Join(", ", _map.Select(e => string.Create(CultureInfo.InvariantCulture, $"\"{e.Key}\": {e.Value}"))) + " }";
}
