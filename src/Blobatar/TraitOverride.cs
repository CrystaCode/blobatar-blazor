using System.Collections.Immutable;
using System.Globalization;

namespace Blobatar;

/// <summary>
/// A configured value for one trait: either a single pinned position or a list of positions the
/// name chooses among.
/// </summary>
/// <remarks>
/// <para>Every value is the position in <c>[0, 1)</c> the hash would otherwise have produced for the key.
/// That is what makes overrides a complete configuration surface rather than a set of escape hatches:
/// the layout reads an override in the same units, through the same ranges, as a hashed value, so
/// nothing a seed can express is out of reach and no override can break a containment guarantee.</para>
/// <para>A <b>list</b> narrows a key without fixing it. <c>{ "shape": [0.11, 0.965] }</c> means "round
/// or sun, whichever this name comes out as": the key's own hash picks the index, so the choice is per
/// name, stable, uniform over the listed values, and independent of every other trait. An empty list
/// selects nothing and is the same as omitting the key.</para>
/// <para>Values are clamped rather than trusted: anything at or above 1 reads as <c>0.999999</c>, anything
/// below 0 (or not a number) reads as 0, so an exact <c>1</c> cannot index past the end of a table.</para>
/// </remarks>
public readonly struct TraitOverride : IEquatable<TraitOverride>
{
    private readonly ImmutableArray<double> _positions;

    private TraitOverride(double position)
    {
        Position = position;
        _positions = default;
    }

    private TraitOverride(ImmutableArray<double> positions)
    {
        Position = double.NaN;
        _positions = positions;
    }

    /// <summary>Whether this override narrows the key to a list rather than pinning one position.</summary>
    public bool IsList => !_positions.IsDefault;

    /// <summary>The pinned position when <see cref="IsList"/> is <see langword="false"/>; otherwise <see cref="double.NaN"/>.</summary>
    public double Position { get; }

    /// <summary>The listed positions when <see cref="IsList"/> is <see langword="true"/>; otherwise empty.</summary>
    public ImmutableArray<double> Positions => _positions.IsDefault ? [] : _positions;

    /// <summary>Pins a key to one position in <c>[0, 1)</c>.</summary>
    public static TraitOverride Pin(double position) => new(position);

    /// <summary>Narrows a key to a set of positions the name chooses among.</summary>
    public static TraitOverride Narrow(params double[] positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        return new(ImmutableArray.Create(positions));
    }

    /// <summary>Narrows a key to a set of positions the name chooses among.</summary>
    public static TraitOverride Narrow(IEnumerable<double> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        return new(positions.ToImmutableArray());
    }

    /// <summary>A single position pins the key.</summary>
    public static implicit operator TraitOverride(double position) => Pin(position);

    /// <summary>An array of positions narrows the key.</summary>
    public static implicit operator TraitOverride(double[] positions) => Narrow(positions ?? []);

    /// <summary>Equivalent of the implicit conversion from <see cref="double"/>, for languages without operator support.</summary>
    public static TraitOverride FromDouble(double position) => Pin(position);

    /// <summary>Equivalent of the implicit conversion from <c>double[]</c>, for languages without operator support.</summary>
    public static TraitOverride FromArray(double[] positions) => Narrow(positions ?? []);

    /// <summary>
    /// The value the trait reader sees for this override, clamped into <c>[0, 0.999999]</c>, or
    /// <see langword="null"/> when an empty list leaves the key to the seed.
    /// </summary>
    internal double? Resolve(uint state, string key)
    {
        double o;
        if (IsList)
        {
            var list = _positions;
            if (list.Length == 0)
            {
                return null;
            }

            o = list[(int)Math.Floor(SeedHash.Stream(state, key) * list.Length)];
        }
        else
        {
            o = Position;
        }

        // Written as upstream writes it: NaN fails `o > 0` and falls to 0, and exactly 1 is pulled under the top edge.
        return o > 0 ? (o < 1 ? o : 0.999999) : 0;
    }

    /// <inheritdoc />
    public bool Equals(TraitOverride other)
    {
        if (IsList != other.IsList)
        {
            return false;
        }

        return IsList
            ? _positions.AsSpan().SequenceEqual(other._positions.AsSpan())
            : Position.Equals(other.Position);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TraitOverride other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (!IsList)
        {
            return Position.GetHashCode();
        }

        var hash = default(HashCode);
        hash.Add(_positions.Length);
        foreach (var p in _positions)
        {
            hash.Add(p);
        }

        return hash.ToHashCode();
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(TraitOverride left, TraitOverride right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(TraitOverride left, TraitOverride right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsList
        ? "[" + string.Join(", ", _positions.Select(p => p.ToString(CultureInfo.InvariantCulture))) + "]"
        : Position.ToString(CultureInfo.InvariantCulture);
}
