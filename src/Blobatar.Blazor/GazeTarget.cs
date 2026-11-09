using Microsoft.AspNetCore.Components;

namespace Blobatar.Blazor;

/// <summary>
/// Where a blobatar's eyes look.
/// </summary>
/// <remarks>
/// <para>Five kinds of thing, mirroring upstream's <c>GazeTarget</c>: a <see cref="GazePoint"/> in client
/// coordinates (a caret, a card), a <see cref="GazeElement"/> whose centre is re-read as the page moves,
/// <see cref="Pointer"/> for the cursor, <see cref="Rest"/> for the blobatar's own centre, held, and
/// <see cref="None"/> for nothing.</para>
/// <para><see cref="Rest"/> and <see cref="None"/> are both "stop looking at that" and they are not the same
/// request: <see cref="Rest"/> keeps the idle glance stood down, so the stillness reads as a face choosing not
/// to look; <see cref="None"/> hands the blobatar back to its own life with the driver still attached.</para>
/// </remarks>
public abstract record GazeTarget
{
    private protected GazeTarget()
    {
    }

    /// <summary>The cursor.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "The pointing device, named as upstream and the CSS Pointer Events specification name it.")]
    public static GazeTarget Pointer { get; } = new GazeKeyword("pointer");

    /// <summary>Its own centre, held: deliberately not looking.</summary>
    public static GazeTarget Rest { get; } = new GazeKeyword("rest");

    /// <summary>Nothing: the eyes ease home and the idle glance comes back.</summary>
    public static GazeTarget None { get; } = new GazeKeyword("none");

    /// <summary>The flat arguments the interop module takes.</summary>
    internal abstract (string Kind, double X, double Y, ElementReference? Element) ToInterop();
}

/// <summary>A point in client (viewport) coordinates, such as a caret or a card.</summary>
/// <param name="X">Client x.</param>
/// <param name="Y">Client y.</param>
public sealed record GazePoint(double X, double Y) : GazeTarget
{
    internal override (string Kind, double X, double Y, ElementReference? Element) ToInterop() => ("point", X, Y, null);
}

/// <summary>An element's centre, re-read as the page scrolls, resizes or the element moves.</summary>
/// <param name="Element">A captured <c>@ref</c>.</param>
public sealed record GazeElement(ElementReference Element) : GazeTarget
{
    internal override (string Kind, double X, double Y, ElementReference? Element) ToInterop() => ("element", 0, 0, Element);
}

/// <summary>One of the three words the driver understands.</summary>
internal sealed record GazeKeyword(string Name) : GazeTarget
{
    internal override (string Kind, double X, double Y, ElementReference? Element) ToInterop() => (Name, 0, 0, null);
}
