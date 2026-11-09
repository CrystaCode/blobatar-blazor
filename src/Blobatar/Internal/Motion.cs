namespace Blobatar.Internal;

/// <summary>
/// The idle layer's contribution to the markup: a root class and the seeded timings the stylesheet reads.
/// </summary>
/// <remarks>
/// A grid where every blobatar breathes in unison does not read as a crowd of creatures; it reads as a
/// heartbeat. Seeded offsets are what make it a crowd. Delays are negated here, at the source, because a
/// positive <c>animation-delay</c> postpones the start rather than offsetting the phase. The <c>motion.*</c>
/// trait keys cost nothing in compatibility: traits are string-addressed, so adding them perturbed no
/// existing blobatar.
/// </remarks>
internal static class Motion
{
    /// <summary>
    /// The root class. <c>mo-expr</c> marks "wearing a non-idle expression" and exists so that adopting an
    /// expression and returning to idle can run on different transition clocks.
    /// </summary>
    public static string RootClass(BlobatarAnimation mode, bool expressive) =>
        "mo-root" + (mode == BlobatarAnimation.Always ? " mo-always" : string.Empty) + (expressive ? " mo-expr" : string.Empty);

    /// <summary>Per-blobatar timing, as custom properties for the stylesheet to read, in upstream's order.</summary>
    public static List<KeyValuePair<string, string>> MotionVars(TraitReader t)
    {
        var blink = JsMath.Round(t.Num("motion.blink", 3500, 6500));
        var saccade = JsMath.Round(t.Num("motion.saccade", 4200, 7600));
        var lookX = t.Num("motion.lookX", 1, 2.2);
        var lookY = t.Num("motion.lookY", 0.8, 1.7);
        var phase = JsMath.Round(t.Num("motion.phase", 0, 2800));
        var bob = JsMath.Round(t.Num("motion.bob", 0, 3400));
        var blinkPhase = JsMath.Round(t.Num("motion.blinkPhase", 0, blink));
        var saccadePhase = JsMath.Round(t.Num("motion.saccadePhase", 0, saccade));
        // Magnitude and sign of the look vector are drawn separately so the value cannot land near zero, and
        // the magnitude ships alongside the signed value because CSS has no portable abs().
        var lookMX = R2(lookX);
        var lookMY = R2(lookY);
        var signedX = lookMX * (t.Bool("motion.lookXFlip") ? -1 : 1);
        var signedY = lookMY * (t.Bool("motion.lookYFlip") ? -1 : 1);

        return
        [
            Var("--mo-phase", Ms(-phase)),
            Var("--mo-bob-phase", Ms(-bob)),
            Var("--mo-blink", Ms(blink)),
            Var("--mo-blink-phase", Ms(-blinkPhase)),
            Var("--mo-look-x", JsMath.ToJsString(signedX)),
            Var("--mo-look-mx", JsMath.ToJsString(lookMX)),
            Var("--mo-look-y", JsMath.ToJsString(signedY)),
            Var("--mo-look-my", JsMath.ToJsString(lookMY)),
            Var("--mo-saccade", Ms(saccade)),
            Var("--mo-saccade-phase", Ms(-saccadePhase)),
        ];

        static double R2(double v) => JsMath.Round(v * 100) / 100;
        static string Ms(double v) => JsMath.ToJsString(v) + "ms";
        static KeyValuePair<string, string> Var(string key, string value) => new(key, value);
    }

    /// <summary><c>--a:1;--b:2</c>: the custom properties as one inline style.</summary>
    public static string Serialize(IEnumerable<KeyValuePair<string, string>> vars) =>
        string.Join(";", vars.Select(kv => kv.Key + ":" + kv.Value));
}
