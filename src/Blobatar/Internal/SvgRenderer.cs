using System.Globalization;
using System.Text;

namespace Blobatar.Internal;

/// <summary>
/// Option resolution and serialization: the renderer behind <see cref="BlobatarRenderer"/>.
/// </summary>
internal static class SvgRenderer
{
    /// <summary>Normalizes and hashes the name once, builds the trait reader, and resolves the palette.</summary>
    public static (TraitReader Traits, BlobatarPalette Palette) Resolve(string name, BlobatarOptions o)
    {
        var t = TraitReader.For(name, o.Normalize, o.Traits);
        // `backgroundColor` only joins the contrast chain when a plate is actually drawn: pushing the body
        // off a colour that never reaches the screen would change the blobatar for nothing.
        var plated = o.Background != BlobatarBackground.None && !string.IsNullOrEmpty(o.BackgroundColor);
        var palette = ColorRamp.Palette(
            o.Hue ?? t.Num("hue", 0, 360),
            o.Contrast,
            o.Tone ?? t["tone"],
            plated ? o.BackgroundColor : null);

        if (o.Palette is { } overrides)
        {
            palette = palette with
            {
                Background = overrides.Background ?? palette.Background,
                Head = overrides.Head ?? palette.Head,
                Eye = overrides.Eye ?? palette.Eye,
            };
        }

        return (t, palette);
    }

    /// <summary>The plate behind the figure, as geometry rather than markup, so an adapter can draw it as a real element outside the motion root.</summary>
    public static BlobatarBackdrop? Backdrop(BlobatarBackground background, BlobatarPalette p) => background switch
    {
        BlobatarBackground.None => null,
        BlobatarBackground.Square => new BlobatarBackdrop("M0 0H100V100H0Z", p.Background!),
        BlobatarBackground.Circle => new BlobatarBackdrop(PathPrimitives.Superellipse(50, 50, 50, 50, 2), p.Background!),
        BlobatarBackground.Squircle => new BlobatarBackdrop(PathPrimitives.Superellipse(50, 50, 50, 50, 6), p.Background!),
        _ => throw new ArgumentOutOfRangeException(nameof(background), background, "Unknown background."),
    };

    /// <summary>The complete static SVG document.</summary>
    public static string Render(string name, BlobatarOptions o)
    {
        var (t, resolved) = Resolve(name, o);
        var palette = Poses.Tinted(resolved, o.Expression);
        var layout = BlobStyle.Layout(t);
        var (posed, wrap) = o.Expression == BlobatarExpression.Idle
            ? (layout, string.Empty)
            : Poses.Bake(layout, Poses.PoseOf(o.Expression));

        var sb = new StringBuilder(1200);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"");
        if (o.Size is int size && size != 0)
        {
            var s = size.ToString(CultureInfo.InvariantCulture);
            sb.Append(" width=\"").Append(s).Append("\" height=\"").Append(s).Append('"');
        }

        sb.Append('>');
        AppendLabel(sb, o.Title);
        AppendPlate(sb, Backdrop(o.Background, palette));
        AppendWrapped(sb, BlobStyle.Render(posed, palette.Head!, palette.Eye!, motion: false), wrap);
        return sb.Append("</svg>").ToString();
    }

    /// <summary>
    /// The blobatar in the pieces a renderer that owns the outer element needs. Nothing that varies with
    /// the expression appears in <see cref="BlobatarParts.Inner"/> when animating: the pose rides in the
    /// class and the custom properties, which land on attributes a renderer can diff in place.
    /// </summary>
    public static BlobatarParts Parts(string name, BlobatarOptions o)
    {
        var (t, palette) = Resolve(name, o);
        var layout = BlobStyle.Layout(t);

        if (o.Animate == BlobatarAnimation.None)
        {
            // Mirrors upstream `_parts` without a motion factory: the pose is baked and the figure wears the resolved palette.
            var (posed, wrap) = o.Expression == BlobatarExpression.Idle
                ? (layout, string.Empty)
                : Poses.Bake(layout, Poses.PoseOf(o.Expression));
            var still = new StringBuilder(1024);
            AppendWrapped(still, BlobStyle.Render(posed, palette.Head!, palette.Eye!, motion: false), wrap);
            return new BlobatarParts(null, Backdrop(o.Background, palette), still.ToString(), null);
        }

        var pose = Poses.PoseOf(o.Expression);
        var poseVars = Poses.PoseVars(pose);
        var tint = Poses.TintOf(o.Expression);
        var worn = tint is { } tt ? Poses.TintWith(palette, pose, tt) : palette;

        var vars = Motion.MotionVars(t);
        // The fills as custom properties on every animated blobatar: tinted when the pose tints, identical to
        // the markup's own attributes when it does not, so the stylesheet's `fill` rules always resolve.
        vars.Add(new KeyValuePair<string, string>("--mo-head", worn.Head!));
        vars.Add(new KeyValuePair<string, string>("--mo-eye", worn.Eye!));
        vars.AddRange(poseVars);

        return new BlobatarParts(
            Motion.RootClass(o.Animate, poseVars.Count > 0 || tint is not null),
            Backdrop(o.Background, palette),
            BlobStyle.Render(layout, palette.Head!, palette.Eye!, motion: true),
            vars);
    }

    /// <summary>The numeric layout and resolved palette, posed and tinted as the static renderer draws them.</summary>
    public static BlobatarLayout Layout(string name, BlobatarOptions o)
    {
        var (t, resolved) = Resolve(name, o);
        var layout = BlobStyle.Layout(t);
        if (o.Expression != BlobatarExpression.Idle)
        {
            layout = Poses.Bake(layout, Poses.PoseOf(o.Expression)).Layout;
        }

        var b = layout.Body;
        return new BlobatarLayout(
            layout.Silhouette.Shape,
            new BlobatarBody(b.Cx, b.Cy, b.Rx, b.Ry, b.N, b.Rot, b.Radii, b.Sides, b.Round),
            new BlobatarEllipse(layout.Face.Cx, layout.Face.Cy, layout.Face.Rx, layout.Face.Ry),
            layout.Eyes.Select(e => new BlobatarEye(e.Cx, e.Cy, e.Rx, e.Ry, e.N, e.Rot)).ToArray(),
            layout.Petals.Select(p => new BlobatarPetal(p.Cx, p.Cy, p.R)).ToArray(),
            layout.Extra.ToArray(),
            layout.BodyPath(),
            Poses.Tinted(resolved, o.Expression));
    }

    /// <summary>The silhouette a name resolves to, without laying out or colouring anything else.</summary>
    public static BlobatarShape ShapeOf(string name, BlobatarOptions o) =>
        BlobStyle.Pick(TraitReader.For(name, o.Normalize, o.Traits)["shape"]).Shape;

    private static void AppendLabel(StringBuilder sb, string? title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        sb.Append("<title>").Append(Escape(title)).Append("</title>");
    }

    private static void AppendPlate(StringBuilder sb, BlobatarBackdrop? backdrop)
    {
        if (backdrop is null)
        {
            return;
        }

        sb.Append("<path d=\"").Append(backdrop.PathData).Append("\" fill=\"").Append(backdrop.Fill).Append("\"/>");
    }

    private static void AppendWrapped(StringBuilder sb, string body, string transform)
    {
        if (transform.Length == 0)
        {
            sb.Append(body);
            return;
        }

        sb.Append("<g transform=\"").Append(transform).Append("\">").Append(body).Append("</g>");
    }

    /// <summary>Escapes the three characters that matter inside element text, exactly as upstream does.</summary>
    private static string Escape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
         .Replace("<", "&lt;", StringComparison.Ordinal)
         .Replace(">", "&gt;", StringComparison.Ordinal);
}
