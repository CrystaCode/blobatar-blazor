namespace Blobatar;

/// <summary>
/// A pose the blobatar holds until you change it.
/// </summary>
/// <remarks>
/// <para>An expression moves parts the blobatar already has (eye scale, tilt, offset, a rigid body shift,
/// a tremor and a tint) and never adds a mark, so a blob grows no mouth when it is happy. Every pose is
/// separated from its nearest neighbour by three channels rather than one, never by tint alone, so the
/// roster still works in greyscale.</para>
/// <para>Without animation the pose is rendered statically, baked into the geometry. With animation the
/// pose travels as CSS custom properties and the stylesheet morphs between poses. Setting an expression
/// never turns animation on for you.</para>
/// <para>A state, not an event: nothing returns to <see cref="Idle"/> on its own and there are no timers.</para>
/// </remarks>
public enum BlobatarExpression
{
    /// <summary>The default. Byte-identical to passing no expression.</summary>
    Idle = 0,

    /// <summary>Tall arcs, lifted, tilted in parallel.</summary>
    Happy,

    /// <summary>Small eyes dropped low, brows in.</summary>
    Sad,

    /// <summary>Wide flat bars in a <c>\ /</c>, warm-tinted, trembling.</summary>
    Mad,

    /// <summary>The only pose that grows the eyes: wide and lifted.</summary>
    Surprised,

    /// <summary>One eye shut, the other open.</summary>
    Wink,

    /// <summary>Level lids low over a sunk body.</summary>
    Sleepy,

    /// <summary>Narrow and cocked: a head tilt, not a brow.</summary>
    Smug,

    /// <summary>One eye squeezed, the pair barely moved.</summary>
    Unsure,

    /// <summary>Small, converged, shivering.</summary>
    Scared,

    /// <summary>Narrow and drawn together, rose-tinted.</summary>
    Love,

    /// <summary>Small, low, converged, pale blush.</summary>
    Shy,

    /// <summary>Wide bars slumped into a <c>/ \</c>, green-tinted, faint tremor.</summary>
    Sick,

    /// <summary>Eyes at two heights, trading places: a loader with a face. The one pose that keeps moving.</summary>
    Thinking,
}

/// <summary>Helpers for <see cref="BlobatarExpression"/>.</summary>
public static class BlobatarExpressionExtensions
{
    /// <summary>The lower-case name upstream exports the pose under, such as <c>"happy"</c>.</summary>
    public static string ToName(this BlobatarExpression expression) => expression switch
    {
        BlobatarExpression.Idle => "idle",
        BlobatarExpression.Happy => "happy",
        BlobatarExpression.Sad => "sad",
        BlobatarExpression.Mad => "mad",
        BlobatarExpression.Surprised => "surprised",
        BlobatarExpression.Wink => "wink",
        BlobatarExpression.Sleepy => "sleepy",
        BlobatarExpression.Smug => "smug",
        BlobatarExpression.Unsure => "unsure",
        BlobatarExpression.Scared => "scared",
        BlobatarExpression.Love => "love",
        BlobatarExpression.Shy => "shy",
        BlobatarExpression.Sick => "sick",
        BlobatarExpression.Thinking => "thinking",
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unknown expression."),
    };

    /// <summary>The pose this expression resolves to: the fourteen channel values.</summary>
    public static Pose GetPose(this BlobatarExpression expression) => Internal.Poses.PoseOf(expression);

    /// <summary>Whether the pose moves the palette toward a tint (mad, love, shy and sick do).</summary>
    public static bool IsTinted(this BlobatarExpression expression) => Internal.Poses.TintOf(expression) is not null;

    /// <summary>Parses an expression name, case-insensitively.</summary>
    public static bool TryParse(string? name, out BlobatarExpression expression)
    {
        foreach (var candidate in Enum.GetValues<BlobatarExpression>())
        {
            if (string.Equals(candidate.ToName(), name?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                expression = candidate;
                return true;
            }
        }

        expression = default;
        return false;
    }

    /// <summary>Parses an expression name, case-insensitively.</summary>
    /// <exception cref="ArgumentException">The name is not an expression.</exception>
    public static BlobatarExpression Parse(string name) =>
        TryParse(name, out var expression) ? expression : throw new ArgumentException($"Unknown expression \"{name}\".", nameof(name));
}
