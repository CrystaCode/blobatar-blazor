namespace Blobatar.Internal;

/// <summary>
/// The expression roster and the pose composition: how a <see cref="Pose"/> becomes geometry (the static
/// path) or custom properties (the animated path).
/// </summary>
/// <remarks>
/// The roster is frozen per generation exactly like the shape bands and the tone set. Every pose is
/// separated from its nearest neighbour by three channels at once, never by its tint alone, so the set
/// still works in greyscale.
/// </remarks>
internal static class Poses
{
    /// <summary>The pose an expression resolves to.</summary>
    public static Pose PoseOf(BlobatarExpression expression) => expression switch
    {
        BlobatarExpression.Idle => Pose.Identity,
        // Wide flat arcs riding high: the universal smiling squint, at full volume.
        BlobatarExpression.Happy => new(Esx: 1.72, Esy: 0.3, Tilt: 8, Edy: -1.5, Edx: 1.5, Esx2: 0.08, Esy2: 0.05, Tilt2: -16, Edy2: 0, Lock: 1, Heat: 0, Shake: 0, Rock: 0, Bdy: -2.2),
        // Small eyes, low and drifted apart, over a body that sinks.
        BlobatarExpression.Sad => new(Esx: 0.6, Esy: 0.56, Tilt: 26, Edy: 3.6, Edx: 1.9, Esx2: -0.05, Esy2: -0.07, Tilt2: -7, Edy2: 0, Lock: 1, Heat: 0, Shake: 0, Rock: 0, Bdy: 2.6),
        // A hard `\ /` of flat bars over a body that compresses, leans and runs hot.
        BlobatarExpression.Mad => new(Esx: 1.85, Esy: 0.26, Tilt: -33, Edy: 0.4, Edx: 0.6, Esx2: 0, Esy2: -0.03, Tilt2: 5, Edy2: 0, Lock: 1, Heat: 0.62, Shake: 0.55, Rock: 0, Bdy: 0.8),
        // Eyes enlarged rather than squashed: the one direction the first roster never went.
        BlobatarExpression.Surprised => new(Esx: 1.34, Esy: 1.2, Tilt: -6, Edy: -1.05, Edx: 0.5, Esx2: 0.05, Esy2: 0.07, Tilt2: 3, Edy2: 0, Lock: 1, Heat: 0, Shake: 0, Rock: 0, Bdy: -1.4),
        // One eye a flat arc, the other open: the pose the per-eye differentials were built for.
        BlobatarExpression.Wink => new(Esx: 1.32, Esy: 0.76, Tilt: 5, Edy: -0.6, Edx: 0.8, Esx2: 0.26, Esy2: -0.56, Tilt2: -11, Edy2: 0, Lock: 1, Heat: 0, Shake: 0, Rock: 0, Bdy: -1.1),
        // Flat bars with no angle in them, sitting low over a sunk body.
        BlobatarExpression.Sleepy => new(Esx: 1.14, Esy: 0.22, Tilt: 0, Edy: 2.4, Edx: 0.3, Esx2: -0.04, Esy2: 0.03, Tilt2: 4, Edy2: 0, Lock: 1, Heat: 0, Shake: 0, Rock: 0, Bdy: 1.2),
        // Half-lidded, lifted, and leaning in parallel: a head cocked rather than a brow.
        BlobatarExpression.Smug => new(Esx: 1.3, Esy: 0.42, Tilt: 18, Edy: -0.5, Edx: 0.5, Esx2: 0.06, Esy2: -0.06, Tilt2: -36, Edy2: 0, Lock: 1, Heat: 0, Shake: 0, Rock: 0, Bdy: -1),
        // One eye narrowed, the other open: the quieter differential-first pose.
        BlobatarExpression.Unsure => new(Esx: 0.95, Esy: 1.02, Tilt: 4, Edy: -0.2, Edx: 0.3, Esx2: 0.24, Esy2: -0.44, Tilt2: -18, Edy2: 0, Lock: 1, Heat: 0, Shake: 0, Rock: 0, Bdy: 0),
        // Small eyes held high and pulled together, over a body that trembles.
        BlobatarExpression.Scared => new(Esx: 0.78, Esy: 0.96, Tilt: -12, Edy: -1.5, Edx: -0.8, Esx2: -0.04, Esy2: 0.05, Tilt2: 4, Edy2: 0, Lock: 1, Heat: 0, Shake: 0.35, Rock: 0, Bdy: -0.6),
        // Tall narrow eyes, drawn together, lifted, and rose.
        BlobatarExpression.Love => new(Esx: 0.86, Esy: 1.28, Tilt: -14, Edy: -0.5, Edx: -0.35, Esx2: 0.05, Esy2: 0.06, Tilt2: 6, Edy2: 0, Lock: 1, Heat: 0.6, Shake: 0, Rock: 0, Bdy: -1.6),
        // Small squeezed eyes, low and wide apart, over a body that sinks and blushes.
        BlobatarExpression.Shy => new(Esx: 0.62, Esy: 0.5, Tilt: 10, Edy: 1.4, Edx: -0.2, Esx2: -0.05, Esy2: -0.04, Tilt2: -8, Edy2: 0, Lock: 1, Heat: 0.55, Shake: 0, Rock: 0, Bdy: 0.9),
        // Flat bars slumped into a `/ \`, over a body that sinks, greens and trembles.
        BlobatarExpression.Sick => new(Esx: 1.25, Esy: 0.34, Tilt: 20, Edy: 1.8, Edx: 0.8, Esx2: 0.05, Esy2: -0.05, Tilt2: -6, Edy2: 0, Lock: 1, Heat: 0.6, Shake: 0.18, Rock: 0, Bdy: 1.4),
        // Mid-lidded, level eyes at two different heights, trading places.
        BlobatarExpression.Thinking => new(Esx: 1.15, Esy: 0.62, Tilt: 0, Edy: 4.2, Edx: 0.4, Esx2: 0.02, Esy2: 0.06, Tilt2: 0, Edy2: -8.4, Lock: 1, Heat: 0, Shake: 0, Rock: 0.8, Bdy: -0.4),
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unknown expression."),
    };

    /// <summary>The tint a pose wears, for the four that move the palette.</summary>
    public static Tint? TintOf(BlobatarExpression expression) => expression switch
    {
        BlobatarExpression.Mad => Tint.Hot,
        BlobatarExpression.Love => Tint.Rose,
        BlobatarExpression.Shy => Tint.Blush,
        BlobatarExpression.Sick => Tint.Bile,
        _ => null,
    };

    /// <summary>
    /// The animated path: the pose as registered custom properties. Only channels that actually move are
    /// emitted, since every property is registered with its identity as the initial value, so an omitted
    /// declaration <em>is</em> the identity and <c>idle</c> is free. <c>heat</c> is never emitted: colour is
    /// resolved here and travels as the finished <c>--mo-head</c>/<c>--mo-eye</c>.
    /// </summary>
    public static List<KeyValuePair<string, string>> PoseVars(Pose p)
    {
        var id = Pose.Identity;
        var vars = new List<KeyValuePair<string, string>>(8);
        Emit("esx", p.Esx, id.Esx);
        Emit("esy", p.Esy, id.Esy);
        Emit("tilt", p.Tilt, id.Tilt);
        Emit("edy", p.Edy, id.Edy);
        Emit("edx", p.Edx, id.Edx);
        Emit("esx2", p.Esx2, id.Esx2);
        Emit("esy2", p.Esy2, id.Esy2);
        Emit("tilt2", p.Tilt2, id.Tilt2);
        Emit("edy2", p.Edy2, id.Edy2);
        Emit("lock", p.Lock, id.Lock);
        Emit("shake", p.Shake, id.Shake);
        Emit("rock", p.Rock, id.Rock);
        Emit("bdy", p.Bdy, id.Bdy);
        return vars;

        void Emit(string key, double value, double identity)
        {
            if (value != identity)
            {
                vars.Add(new KeyValuePair<string, string>("--mo-" + key, PathPrimitives.R3(value)));
            }
        }
    }

    /// <summary>Whether an expression is non-idle on the animated path: it moves a channel or tints.</summary>
    public static bool IsExpressive(BlobatarExpression expression) =>
        expression != BlobatarExpression.Idle && (PoseVars(PoseOf(expression)).Count > 0 || TintOf(expression) is not null);

    /// <summary>The palette a tinting pose wears: the resolved one, mixed <see cref="Pose.Heat"/> of the way toward the pair derived for its target.</summary>
    public static BlobatarPalette TintWith(BlobatarPalette palette, Pose p, Tint t)
    {
        var (head, eye) = ColorRamp.Tinted(palette.Head!, palette.Eye!, t);
        return palette with
        {
            Head = ColorRamp.MixHex(palette.Head!, head, p.Heat),
            Eye = ColorRamp.MixHex(palette.Eye!, eye, p.Heat),
        };
    }

    /// <summary>The palette the static render paints: tinted when the expression tints, otherwise as resolved.</summary>
    public static BlobatarPalette Tinted(BlobatarPalette palette, BlobatarExpression expression) =>
        TintOf(expression) is { } tint ? TintWith(palette, PoseOf(expression), tint) : palette;

    /// <summary>
    /// The static path: eye channels baked into geometry, body channels handed back as one <c>transform</c>
    /// for the caller to wrap the figure in.
    /// </summary>
    /// <remarks>
    /// Baking is exact rather than approximate because the CSS composes in the same order the geometry does:
    /// the superellipse scales by its radii and then rotates, and the stylesheet applies the pose scale
    /// innermost and the tilt outside it. The offsets commute with both, since rotation and scale are about
    /// each eye's own centre.
    /// </remarks>
    public static (LayoutData Layout, string Wrap) Bake(LayoutData l, Pose p)
    {
        var eyes = new EyeGeometry[l.Eyes.Length];
        for (var i = 0; i < eyes.Length; i++)
        {
            var e = l.Eyes[i];
            var right = i != 0;
            var wrap = right ? 1 : -1;
            eyes[i] = new EyeGeometry(
                Cx: e.Cx + p.Edx * wrap,
                Cy: e.Cy + p.Edy + (right ? p.Edy2 : 0),
                Rx: e.Rx * (p.Esx + (right ? p.Esx2 : 0)),
                Ry: e.Ry * (p.Esy + (right ? p.Esy2 : 0)),
                N: e.N,
                Rot: e.Rot * (1 - p.Lock) + (p.Tilt + (right ? p.Tilt2 : 0)) * wrap);
        }

        return (l.WithEyes(eyes), p.Bdy != 0 ? "translate(0 " + PathPrimitives.R3(p.Bdy) + ")" : string.Empty);
    }
}
