namespace Blobatar.Blazor;

/// <summary>
/// The pointer-driven gaze layer: the eyes follow a target.
/// </summary>
/// <remarks>
/// <para>Set on an animated <c>&lt;Blobatar&gt;</c> (it needs the inline <c>&lt;svg&gt;</c>), with the
/// <c>gaze.css</c> stylesheet loaded beside <c>motion.css</c>. On a still blobatar it does nothing: an
/// <c>&lt;img&gt;</c> has no eyes.</para>
/// <para>The excursion is what opts a blobatar in. The stylesheet registers <c>--mo-track-travel</c> at
/// <c>0px</c>, so with nothing else done every face on the page holds still. <see cref="Travel"/> writes it
/// inline on the <c>&lt;svg&gt;</c>; a CSS rule matching <c>.mo-eyes</c> wins over that, so pick one route.</para>
/// <para>A driver is armed by attaching and aimed by a target: with <see cref="Target"/> left
/// <see langword="null"/> the eyes stay home until <c>LookAtAsync</c> is called.</para>
/// </remarks>
public sealed record GazeOptions
{
    /// <summary>
    /// The excursion in viewBox units. The blobatar is 100 across, so 12, which the hero on blobatar.dev uses,
    /// moves the eyes 12% of the face and reads as a deliberate look at the pointer; 3 is a glance you have to
    /// watch for. Omit it and the stylesheet owns the property, which is the better route for a whole field of
    /// blobatars or anything responsive.
    /// </summary>
    public double? Travel { get; init; }

    /// <summary>
    /// A blobatar whose eyes follow the pointer wherever it is on the page: <see cref="Travel"/> set to
    /// <paramref name="travel"/> and <see cref="Target"/> set to <see cref="GazeTarget.Pointer"/>.
    /// </summary>
    /// <param name="travel">The excursion in viewBox units; 12 is what the hero on blobatar.dev uses.</param>
    /// <example>
    /// <code>
    /// &lt;Blobatar Name="alain" Size="240" Animate="BlobatarAnimation.Always" Gaze="GazeOptions.FollowPointer(12)" /&gt;
    /// </code>
    /// </example>
    public static GazeOptions FollowPointer(double travel) => new() { Travel = travel, Target = GazeTarget.Pointer };

    /// <summary>Pursuit time constant in milliseconds. Defaults to the driver's 110.</summary>
    public double? Settle { get; init; }

    /// <summary>Saccade threshold in units of the excursion. Defaults to the driver's 1.6.</summary>
    public double? Snap { get; init; }

    /// <summary>
    /// Where to look, declaratively. Re-applied whenever it changes by value. Omitted means "I will aim it
    /// myself" through <c>LookAtAsync</c>; <see cref="GazeTarget.None"/> is a target like any other.
    /// </summary>
    public GazeTarget? Target { get; init; }
}
