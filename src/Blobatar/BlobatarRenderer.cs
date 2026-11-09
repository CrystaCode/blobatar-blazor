using System.Diagnostics.CodeAnalysis;
using Blobatar.Internal;

namespace Blobatar;

/// <summary>
/// Renders deterministic geometric blobatars from any string.
/// </summary>
/// <remarks>
/// <para>The same name always produces the same output within a generation: the numeric ranges of the
/// layout, the silhouette bands, the tone set and the expression roster are all part of that contract,
/// and the test suite pins it against upstream's golden fixture byte for byte.</para>
/// <code>
/// var svg = BlobatarRenderer.Render("alain@example.com");
/// var src = BlobatarRenderer.RenderDataUri(user.Id, new BlobatarOptions { Size = 48, Background = BlobatarBackground.Squircle });
/// </code>
/// <para>Names are normalized (NFC, trimmed, lowercased) before hashing, so <c>Alain@Example.com</c> and
/// <c>alain@example.com</c> agree. Nothing uses element ids, gradients or filters, so hundreds of blobatars
/// on one page cannot collide.</para>
/// </remarks>
public static class BlobatarRenderer
{
    /// <summary>The upstream library version this port reproduces.</summary>
    public const string UpstreamVersion = "2.8.0";

    /// <summary>The generation (frozen name-to-look mapping) this port renders.</summary>
    public const int Generation = 2;

    /// <summary>Renders a blobatar as SVG markup: <c>&lt;svg xmlns="…" viewBox="0 0 100 100"&gt;…&lt;/svg&gt;</c>.</summary>
    /// <param name="name">Who the blobatar is for: a username, an email, an id. Any string.</param>
    /// <param name="options">Rendering options, or <see langword="null"/> for the defaults. <see cref="BlobatarOptions.Animate"/> is ignored here; the markup is always still.</param>
    public static string Render(string name, BlobatarOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        return SvgRenderer.Render(name, options ?? BlobatarOptions.Default);
    }

    /// <summary>Renders a blobatar as a percent-encoded <c>data:image/svg+xml,…</c> URI for <c>&lt;img src&gt;</c> or <c>background-image</c>.</summary>
    /// <param name="name">Who the blobatar is for.</param>
    /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
    [SuppressMessage("Design", "CA1055:URI-like return values should not be strings", Justification = "A data: URI is only ever used as an attribute string.")]
    public static string RenderDataUri(string name, BlobatarOptions? options = null) => DataUri.Encode(Render(name, options));

    /// <summary>
    /// The numeric layout and resolved palette, before serialization: for callers that need the geometry
    /// (bulk shape filtering, custom drawing) rather than the markup.
    /// </summary>
    /// <param name="name">Who the blobatar is for.</param>
    /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
    public static BlobatarLayout Layout(string name, BlobatarOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        return SvgRenderer.Layout(name, options ?? BlobatarOptions.Default);
    }

    /// <summary>
    /// The blobatar split into the pieces a component that owns the <c>&lt;svg&gt;</c> element needs, which is
    /// how animation is rendered. See <see cref="BlobatarParts"/>.
    /// </summary>
    /// <param name="name">Who the blobatar is for.</param>
    /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
    public static BlobatarParts Parts(string name, BlobatarOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        return SvgRenderer.Parts(name, options ?? BlobatarOptions.Default);
    }

    /// <summary>The silhouette a name resolves to. Costs a hash and a table lookup, so it is cheap to run over thousands of names.</summary>
    /// <param name="name">Who the blobatar is for.</param>
    /// <param name="options">Options whose <see cref="BlobatarOptions.Normalize"/> and <see cref="BlobatarOptions.Traits"/> affect the answer, or <see langword="null"/>.</param>
    public static BlobatarShape ShapeOf(string name, BlobatarOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        return SvgRenderer.ShapeOf(name, options ?? BlobatarOptions.Default);
    }
}
