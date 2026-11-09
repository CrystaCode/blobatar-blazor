namespace Blobatar;

/// <summary>
/// Everything a render can be asked for beyond the name. Every option is optional.
/// </summary>
/// <remarks>
/// A record with value equality, so two option sets that render the same blobatar compare equal, which
/// is what lets a component cache its last render without reflection.
/// </remarks>
public sealed record BlobatarOptions
{
    /// <summary>The default options: everything from the name, transparent, still, idle.</summary>
    public static BlobatarOptions Default { get; } = new();

    /// <summary>Emits <c>width</c>/<c>height</c> attributes. Omit to let CSS size it; the viewBox always scales.</summary>
    public int? Size { get; init; }

    /// <summary>The plate behind the figure. <see cref="BlobatarBackground.None"/> by default: the body is the blobatar.</summary>
    public BlobatarBackground Background { get; init; } = BlobatarBackground.None;

    /// <summary>
    /// The plate's fill colour. Has no effect unless a <see cref="Background"/> is drawn.
    /// Unlike <see cref="Palette"/>, this keeps the contrast guarantee: a <c>#rgb</c>/<c>#rrggbb</c> value takes
    /// the authored plate's place in the contrast floors, so the body is pushed off the colour actually drawn;
    /// any other CSS colour is painted as given. <see cref="Palette"/>'s background wins if both are given.
    /// </summary>
    public string? BackgroundColor { get; init; }

    /// <summary>Overrides specific palette slots. Overridden colours bypass the contrast guarantee.</summary>
    public BlobatarPalette? Palette { get; init; }

    /// <summary>Locks the hue in degrees, so the name drives shape only. 360 is 0: hue is a circle.</summary>
    public double? Hue { get; init; }

    /// <summary>
    /// Locks the tone as a 0–1 position in the swatch set, pale to ink. The swatches are banded with half-open
    /// edges, so an exact <c>1</c> sits on the top edge and renders what <c>0</c> renders; reach for ink with
    /// <c>0.999</c> or <see cref="BlobatarToneExtensions.ToPosition"/>.
    /// </summary>
    public double? Tone { get; init; }

    /// <summary>
    /// Pins or narrows individual traits, so the name drives only what you leave out. See <see cref="BlobatarTraits"/>.
    /// <see cref="Hue"/> and <see cref="Tone"/> win over the <c>hue</c> and <c>tone</c> entries here.
    /// </summary>
    public BlobatarTraits? Traits { get; init; }

    /// <summary>Applies NFC normalization, trimming and lowercasing to the name. Default <see langword="true"/>; turn off for case-sensitive identifiers.</summary>
    public bool Normalize { get; init; } = true;

    /// <summary>Enforces the minimum contrast ratios. Default <see langword="true"/>.</summary>
    public bool Contrast { get; init; } = true;

    /// <summary>
    /// An accessible name, emitted as a <c>&lt;title&gt;</c> inside the SVG. Names who the blobatar stands
    /// for, not what it looks like. Without one the image is decorative.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>The pose the blobatar holds. <see cref="BlobatarExpression.Idle"/> renders byte-identical markup to no expression at all.</summary>
    public BlobatarExpression Expression { get; init; } = BlobatarExpression.Idle;

    /// <summary>
    /// The idle animation mode. Honoured by renderers that own the outer element (the Blazor component and
    /// <see cref="BlobatarRenderer.Parts"/>); <see cref="BlobatarRenderer.Render"/> always returns still markup.
    /// Requires the <c>motion.css</c> stylesheet.
    /// </summary>
    public BlobatarAnimation Animate { get; init; } = BlobatarAnimation.None;
}
