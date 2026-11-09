namespace Blobatar;

/// <summary>
/// The blobatar in the pieces a renderer that owns the outer <c>&lt;svg&gt;</c> element needs.
/// </summary>
/// <param name="RootClass">Goes on the root <c>&lt;g&gt;</c> the renderer draws. <see langword="null"/> when not animating.</param>
/// <param name="Backdrop">The plate, as geometry: it belongs outside the root group so it neither breathes nor lifts.</param>
/// <param name="Inner">Everything below the root group. When animating, nothing in here varies with the expression.</param>
/// <param name="Variables">The custom properties for the outer element's <c>style</c>: seeded timings, fills, and the pose. <see langword="null"/> when not animating.</param>
/// <remarks>
/// <para>This is the adapter seam, the equivalent of upstream's <c>blobatar/internal</c>. A framework component
/// renders <c>&lt;svg&gt;</c>, an optional <c>&lt;title&gt;</c>, the backdrop <c>&lt;path&gt;</c>, and a
/// <c>&lt;g class="RootClass"&gt;</c> whose content is <see cref="Inner"/>.</para>
/// <para>The split is load-bearing for the morph: an expression change must touch only the class and the
/// custom properties, which land on attributes a renderer can diff in place. If <see cref="Inner"/> were
/// re-inserted, the browser would have no previous computed value to transition from and every idle
/// animation beneath would restart from phase zero.</para>
/// </remarks>
public sealed record BlobatarParts(
    string? RootClass,
    BlobatarBackdrop? Backdrop,
    string Inner,
    IReadOnlyList<KeyValuePair<string, string>>? Variables)
{
    /// <summary>The custom properties as one inline style (<c>--mo-phase:-1234ms;--mo-head:#d0d897</c>), or <see langword="null"/> when there are none.</summary>
    public string? SerializeVariables() =>
        Variables is null ? null : string.Join(";", Variables.Select(kv => kv.Key + ":" + kv.Value));
}

/// <summary>The plate behind the figure, as geometry.</summary>
/// <param name="PathData">SVG path data for the plate's outline.</param>
/// <param name="Fill">The plate's fill colour.</param>
public sealed record BlobatarBackdrop(string PathData, string Fill);
