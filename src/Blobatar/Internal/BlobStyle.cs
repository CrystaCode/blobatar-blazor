using System.Text;

namespace Blobatar.Internal;

/// <summary>
/// Generation 2's style: the band table that weights the ten silhouettes, the shared body and eye
/// layout, and the SVG serialization of a layout.
/// </summary>
/// <remarks>
/// The bands, the layout ranges and the tone set together form the frozen name-to-look mapping of the
/// generation. Round and organic are the everyday shapes; the louder silhouettes stay finds.
/// </remarks>
internal static class BlobStyle
{
    /// <summary><c>(silhouette, upper edge of its band in [0, 1))</c>, in order.</summary>
    private static readonly (Silhouette Shape, double UpTo)[] Bands =
    [
        (Silhouettes.Round, 0.22), (Silhouettes.Organic, 0.48), (Silhouettes.Boxy, 0.6), (Silhouettes.Capsule, 0.7), (Silhouettes.Nub, 0.79),
        (Silhouettes.Cloud, 0.86), (Silhouettes.Droplet, 0.915), (Silhouettes.Hexagon, 0.95), (Silhouettes.Sun, 0.98), (Silhouettes.Triangle, 1),
    ];

    /// <summary>The silhouette a <c>shape</c> trait position selects.</summary>
    public static Silhouette Pick(double v)
    {
        foreach (var (shape, upTo) in Bands)
        {
            if (v < upTo)
            {
                return shape;
            }
        }

        return Bands[^1].Shape;
    }

    /// <summary>The numeric layout for a set of traits.</summary>
    public static LayoutData Layout(TraitReader t)
    {
        var shape = Pick(t["shape"]);
        var r = t.Num("body.r", 31, 38) * shape.Core;
        var count = t.Int("body.pts", 6, 8);
        var radii = new double[count];
        for (var i = 0; i < count; i++)
        {
            radii[i] = 1 + t.Jitter(BlobatarTraitKey.BodyRadiusAt(i), 0.16);
        }

        var body = new BodyGeometry
        {
            Cx = 50 + t.Jitter("body.x", 1.5),
            Cy = 50 + t.Jitter("body.y", 1.5),
            Rx = r,
            Ry = r * t.Num("body.ratio", 0.92, 1.08),
            N = t.Num("body.n", 1.9, 2.5),
            Rot = 0,
            Radii = radii,
        };
        shape.Body?.Invoke(t, body);

        // The body itself when the shape names no face: a silhouette convex around its own centre wants exactly that.
        var face = shape.Face?.Invoke(body) ?? new EllipseGeometry(body.Cx, body.Cy, body.Rx, body.Ry);
        var deco = new Decoration();
        shape.Decorate?.Invoke(t, body, deco);

        return new LayoutData
        {
            Silhouette = shape,
            Body = body,
            Face = face,
            Petals = deco.Petals,
            Extra = deco.Extra,
            Eyes = FaceFit(t, body, face),
        };
    }

    /// <summary>Fits the eye cluster against the silhouette's face region on both axes.</summary>
    private static EyeGeometry[] FaceFit(TraitReader t, BodyGeometry b, EllipseGeometry face)
    {
        var rx = b.Rx;
        var er0 = t.Num("eye.rx", 0.075, 0.105) * rx;
        var ratio = t.Num("eye.ratio", 1.9, 3.2);
        var scale = t.Num("eye.scale", 0.78, 1.24);
        var stretch = t.Num("eye.stretch", 0.85, 1.18);
        var clearance = t.Num("eye.gap", 0.1, 0.24) * rx;
        var wide = er0 * Math.Max(1, scale);
        var tall = er0 * ratio * Math.Max(1, scale * stretch);
        var gap0 = wide + rx * 0.03 + clearance;

        var gx = t.Jitter("gaze.x", 0.09) * face.Rx;
        var gy = t.Num("gaze.y", -0.2, 0.08) * face.Ry;
        var dy = t.Jitter("eye.dy", 0.04) * face.Ry;
        var reach = JsMath.Hypot(wide, tall);
        var need = JsMath.Hypot(
            (Math.Abs(gx) + gap0 + reach) / face.Rx,
            (Math.Abs(gy) + Math.Abs(dy) + reach) / face.Ry);
        var fit = need > 0.9 ? 0.9 / need : 1;

        var er = er0 * fit;
        var eyeRy = er * ratio;
        var gap = gap0 * fit;
        var room = Math.Max(0, Math.Min(1, clearance / tall));
        var bound = Math.Min(12, (Math.Asin(room) * 180) / Math.PI);
        var lean = t.Num("eye.lean", -1, 1) * bound;
        var lean2 = Math.Max(-12, Math.Min(12, lean + t.Jitter("eye.lean2", 3.5)));

        var cx = face.Cx + gx * fit;
        var cy = face.Cy + gy * fit;
        return
        [
            new EyeGeometry(cx - gap, cy, er, eyeRy, t.Num("eye.n", 3.5, 6), lean),
            new EyeGeometry(cx + gap, cy + dy * fit, er * scale, eyeRy * scale * stretch, t.Num("eye.n", 3.5, 6), lean2),
        ];
    }

    /// <summary>
    /// Serializes a layout to the markup below the root group.
    /// </summary>
    /// <param name="l">The layout, posed or not.</param>
    /// <param name="head">The body fill.</param>
    /// <param name="eye">The eye fill.</param>
    /// <param name="motion">
    /// Whether to emit the motion groups and per-eye custom properties the stylesheet reads. Nothing in
    /// this markup varies with the expression: when animating, the pose is custom properties on the root.
    /// </param>
    public static string Render(LayoutData l, string head, string eye, bool motion)
    {
        var sb = new StringBuilder(1024);
        if (motion)
        {
            sb.Append("<g class=\"mo-breathe\"><g class=\"mo-bob\">");
        }

        sb.Append("<g fill=\"").Append(head).Append("\">");
        foreach (var d in l.Petals)
        {
            sb.Append("<circle cx=\"").Append(PathPrimitives.R2(d.Cx))
              .Append("\" cy=\"").Append(PathPrimitives.R2(d.Cy))
              .Append("\" r=\"").Append(PathPrimitives.R2(d.R)).Append("\"/>");
        }

        foreach (var d in l.Extra)
        {
            sb.Append("<path d=\"").Append(d).Append("\"/>");
        }

        sb.Append("<path d=\"").Append(l.BodyPath()).Append("\"/></g>");

        sb.Append("<g fill=\"").Append(eye).Append('"');
        if (motion)
        {
            sb.Append(" class=\"mo-eyes\"");
        }

        sb.Append('>');
        for (var i = 0; i < l.Eyes.Length; i++)
        {
            var e = l.Eyes[i];
            var path = "<path d=\"" + PathPrimitives.Superellipse(e.Cx, e.Cy, e.Rx, e.Ry, e.N, e.Rot) + "\"/>";
            if (motion)
            {
                sb.Append("<g class=\"mo-eye\" style=\"--mo-wrap:").Append(i != 0 ? "1" : "-1")
                  .Append(";--mo-lean:").Append(PathPrimitives.R2(e.Rot))
                  .Append(";transform-origin:").Append(PathPrimitives.R2(e.Cx)).Append("px ").Append(PathPrimitives.R2(e.Cy)).Append("px\">")
                  .Append(path).Append("</g>");
            }
            else
            {
                sb.Append(path);
            }
        }

        sb.Append("</g>");
        if (motion)
        {
            sb.Append("</g></g>");
        }

        return sb.ToString();
    }
}
