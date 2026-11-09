namespace Blobatar;

/// <summary>
/// The idle animation mode.
/// </summary>
/// <remarks>
/// <para>Off by default. When on, the blobatar idles: a soft breathe, a bob, a blink, and the occasional
/// glance to one side, every timing seeded from the name so a grid reads as a crowd rather than a drill
/// team. The motion itself is the <c>motion.css</c> stylesheet; the renderer only emits the classes and
/// custom properties it reads.</para>
/// <para>Turning this on changes the rendering mode: a static blobatar is a single <c>&lt;img&gt;</c>, an
/// animated one is inline SVG, because content inside an <c>&lt;img&gt;</c> is an isolated document that
/// <c>:hover</c> cannot reach.</para>
/// </remarks>
public enum BlobatarAnimation
{
    /// <summary>Still. Renders as a single <c>&lt;img&gt;</c>.</summary>
    None = 0,

    /// <summary>Animates while hovered. The right default for a grid, where ambient motion everywhere is noise.</summary>
    Hover,

    /// <summary>Animates continuously. For the single-blobatar case: a profile header, an onboarding screen.</summary>
    Always,
}

/// <summary>Helpers for <see cref="BlobatarAnimation"/>.</summary>
public static class BlobatarAnimationExtensions
{
    /// <summary>The value upstream's <c>animate</c> prop takes for this mode, or <see langword="null"/> for <see cref="BlobatarAnimation.None"/>.</summary>
    public static string? ToName(this BlobatarAnimation animation) => animation switch
    {
        BlobatarAnimation.None => null,
        BlobatarAnimation.Hover => "hover",
        BlobatarAnimation.Always => "always",
        _ => throw new ArgumentOutOfRangeException(nameof(animation), animation, "Unknown animation mode."),
    };
}
