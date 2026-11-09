namespace Blobatar;

/// <summary>
/// The plate drawn behind the figure.
/// </summary>
/// <remarks>
/// Transparent by default: the body is the blobatar. A plate is drawn outside the motion root, so it
/// neither breathes nor lifts on hover, and its colour comes from the palette's <c>bg</c> slot or from
/// <see cref="BlobatarOptions.BackgroundColor"/>.
/// </remarks>
public enum BlobatarBackground
{
    /// <summary>No plate. The default.</summary>
    None = 0,

    /// <summary>The full 100×100 frame.</summary>
    Square,

    /// <summary>A circle inscribed in the frame.</summary>
    Circle,

    /// <summary>A squircle inscribed in the frame. What upstream's <c>background: true</c> draws.</summary>
    Squircle,
}

/// <summary>Helpers for <see cref="BlobatarBackground"/>.</summary>
public static class BlobatarBackgroundExtensions
{
    /// <summary>The lower-case name the upstream HTTP endpoint spells the plate with, such as <c>"squircle"</c> or <c>"none"</c>.</summary>
    public static string ToName(this BlobatarBackground background) => background switch
    {
        BlobatarBackground.None => "none",
        BlobatarBackground.Square => "square",
        BlobatarBackground.Circle => "circle",
        BlobatarBackground.Squircle => "squircle",
        _ => throw new ArgumentOutOfRangeException(nameof(background), background, "Unknown background."),
    };

    /// <summary>Parses a plate name, case-insensitively.</summary>
    public static bool TryParse(string? name, out BlobatarBackground background)
    {
        foreach (var candidate in Enum.GetValues<BlobatarBackground>())
        {
            if (string.Equals(candidate.ToName(), name?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                background = candidate;
                return true;
            }
        }

        background = default;
        return false;
    }

    /// <summary>Parses a plate name, case-insensitively.</summary>
    /// <exception cref="ArgumentException">The name is not a plate.</exception>
    public static BlobatarBackground Parse(string name) =>
        TryParse(name, out var background) ? background : throw new ArgumentException($"Unknown background \"{name}\".", nameof(name));
}
