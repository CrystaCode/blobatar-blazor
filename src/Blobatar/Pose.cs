namespace Blobatar;

/// <summary>
/// What an expression resolves to: the fourteen channels a pose may move, and nothing else.
/// </summary>
/// <param name="Esx">Eye width, as a factor about each eye's own centre.</param>
/// <param name="Esy">Eye height, as a factor about each eye's own centre.</param>
/// <param name="Tilt">
/// Eye tilt in degrees, mirrored per side: the left eye rotates by <c>-Tilt</c>, the right by <c>+Tilt</c>.
/// On a portrait capsule a positive tilt is the angry direction; flattened past square into a landscape bar,
/// the same rotation reads the opposite way.
/// </param>
/// <param name="Edy">Eye pair vertical offset in viewBox units, positive = down.</param>
/// <param name="Edx">Eye convergence in viewBox units, positive = apart.</param>
/// <param name="Esx2">Extra width on the right eye only.</param>
/// <param name="Esy2">Extra height on the right eye only.</param>
/// <param name="Tilt2">Extra tilt on the right eye only, added before the per-side mirroring.</param>
/// <param name="Edy2">Extra vertical offset on the right eye only, positive = down. The static half of the <c>thinking</c> seesaw.</param>
/// <param name="Lock">How much of the seeded eye lean the pose overrides, 0 to 1. At 1 the eyes sit at exactly the angle the pose names.</param>
/// <param name="Heat">How far the palette shifts toward its tint, 0 to 1. Resolved to colour before it reaches any stylesheet.</param>
/// <param name="Shake">Tremor amplitude, 0 to 1. Held, not fired: an amplitude on a loop that always runs.</param>
/// <param name="Rock">Seesaw amplitude, 0 to 1. How much of <paramref name="Edy2"/> takes part in the swing.</param>
/// <param name="Bdy">Whole-creature vertical offset in viewBox units, positive = down. A rigid translate; the body never deforms.</param>
/// <remarks>
/// Petals and path data are excluded on purpose: a sun's petals are silhouette, and interpolating path
/// data would put geometry on the main thread every frame. The <c>*2</c> channels are the second eye's
/// differential rather than its value, so an identity of 0 is a symmetric face.
/// </remarks>
public readonly record struct Pose(
    double Esx,
    double Esy,
    double Tilt,
    double Edy,
    double Edx,
    double Esx2,
    double Esy2,
    double Tilt2,
    double Edy2,
    double Lock,
    double Heat,
    double Shake,
    double Rock,
    double Bdy)
{
    /// <summary>The identity pose: what <see cref="BlobatarExpression.Idle"/> resolves to, and the initial value of every motion custom property.</summary>
    public static Pose Identity => new(1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
