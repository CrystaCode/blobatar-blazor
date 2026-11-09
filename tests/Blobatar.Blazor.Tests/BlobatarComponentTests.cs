using Bunit;
using Xunit;

namespace Blobatar.Blazor.Tests;

/// <summary>The adapter contract: the component re-expresses the renderer and adds nothing of its own.</summary>
public class BlobatarComponentTests : BunitContext
{
    private const string ModulePath = "./_content/Blobatar.Blazor/blobatar-interop.js";

    [Fact]
    public void A_still_blobatar_is_an_img_carrying_the_data_uri()
    {
        var cut = Render<Blobatar>(ps => ps.Add(c => c.Name, "alain").Add(c => c.Size, 48));
        var img = cut.Find("img");
        Assert.Equal(BlobatarRenderer.RenderDataUri("alain", new BlobatarOptions { Size = 48 }), img.GetAttribute("src"));
        Assert.Equal("48", img.GetAttribute("width"));
        Assert.Equal("48", img.GetAttribute("height"));
        Assert.Equal(string.Empty, img.GetAttribute("alt"));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void The_title_labels_the_image_in_both_modes()
    {
        var still = Render<Blobatar>(ps => ps.Add(c => c.Name, "alain").Add(c => c.Title, "Alain"));
        Assert.Equal("Alain", still.Find("img").GetAttribute("alt"));

        var moving = Render<Blobatar>(ps => ps.Add(c => c.Name, "alain").Add(c => c.Title, "Alain").Add(c => c.Animate, BlobatarAnimation.Hover));
        var svg = moving.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Null(svg.GetAttribute("aria-hidden"));
        Assert.Equal("Alain", moving.Find("svg > title").TextContent);

        var decorative = Render<Blobatar>(ps => ps.Add(c => c.Name, "alain").Add(c => c.Animate, BlobatarAnimation.Hover));
        Assert.Equal("true", decorative.Find("svg").GetAttribute("aria-hidden"));
        Assert.Null(decorative.Find("svg").GetAttribute("role"));
    }

    [Fact]
    public void An_animated_blobatar_is_an_svg_with_the_three_children_in_order()
    {
        var options = new BlobatarOptions { Size = 64, Background = BlobatarBackground.Circle, Animate = BlobatarAnimation.Always, Title = "Alain" };
        var parts = BlobatarRenderer.Parts("alain", options);
        var cut = Render<Blobatar>(ps => ps
            .Add(c => c.Name, "alain")
            .Add(c => c.Size, 64)
            .Add(c => c.Background, BlobatarBackground.Circle)
            .Add(c => c.Animate, BlobatarAnimation.Always)
            .Add(c => c.Title, "Alain"));

        var svg = cut.Find("svg");
        Assert.Equal("0 0 100 100", svg.GetAttribute("viewBox"));
        Assert.Equal("64", svg.GetAttribute("width"));
        Assert.Equal(parts.SerializeVariables(), svg.GetAttribute("style"));

        var children = svg.Children;
        Assert.Equal(3, children.Length);
        Assert.Equal("title", children[0].LocalName);
        Assert.Equal("path", children[1].LocalName);
        Assert.Equal(parts.Backdrop!.PathData, children[1].GetAttribute("d"));
        Assert.Equal("g", children[2].LocalName);
        Assert.Equal("mo-root mo-always", children[2].GetAttribute("class"));
        // Semantic comparison: the DOM re-serializes self-closing <path/> as <path></path>.
        children[2].MarkupMatches($"<g class=\"mo-root mo-always\">{parts.Inner}</g>");
    }

    [Fact]
    public void Changing_the_expression_only_touches_attributes()
    {
        var cut = Render<Blobatar>(ps => ps.Add(c => c.Name, "alain").Add(c => c.Animate, BlobatarAnimation.Always));
        var innerBefore = cut.Find("svg > g").InnerHtml;

        var styleBefore = cut.Find("svg").GetAttribute("style");

        cut.Render(ps => ps.Add(c => c.Name, "alain").Add(c => c.Animate, BlobatarAnimation.Always).Add(c => c.Expression, BlobatarExpression.Mad));

        // The pose lands on the root group's class and the <svg>'s custom properties; the markup inside the
        // group is byte-identical, which is what lets Blazor leave that subtree alone and the CSS morph run.
        Assert.NotEqual(styleBefore, cut.Find("svg").GetAttribute("style"));
        Assert.Equal("mo-root mo-always mo-expr", cut.Find("svg > g").GetAttribute("class"));
        Assert.Contains("--mo-esx:1.85", cut.Find("svg").GetAttribute("style"), StringComparison.Ordinal);
        Assert.Equal(innerBefore, cut.Find("svg > g").InnerHtml);
    }

    [Fact]
    public void Class_style_and_extra_attributes_pass_through_and_reserved_ones_do_not()
    {
        var cut = Render<Blobatar>(ps => ps
            .Add(c => c.Name, "alain")
            .Add(c => c.Animate, BlobatarAnimation.Hover)
            .Add(c => c.Class, "avatar")
            .Add(c => c.Style, "border-radius:50%")
            .AddUnmatched("data-user", "42")
            .AddUnmatched("src", "evil")
            .AddUnmatched("viewBox", "0 0 1 1"));

        var svg = cut.Find("svg");
        Assert.Equal("avatar", svg.GetAttribute("class"));
        Assert.EndsWith(";border-radius:50%", svg.GetAttribute("style"), StringComparison.Ordinal);
        Assert.StartsWith("--mo-phase:", svg.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Equal("42", svg.GetAttribute("data-user"));
        Assert.Null(svg.GetAttribute("src"));
        Assert.Equal("0 0 100 100", svg.GetAttribute("viewBox"));

        var still = Render<Blobatar>(ps => ps.Add(c => c.Name, "alain").Add(c => c.Style, "opacity:.5").AddUnmatched("loading", "lazy"));
        Assert.Equal("opacity:.5", still.Find("img").GetAttribute("style"));
        Assert.Equal("lazy", still.Find("img").GetAttribute("loading"));
    }

    [Fact]
    public void The_same_inputs_render_the_same_markup_as_the_core()
    {
        var cut = Render<Blobatar>(ps => ps.Add(c => c.Name, "Alain@Example.com").Add(c => c.Hue, 200.0).Add(c => c.Expression, BlobatarExpression.Wink));
        var expected = BlobatarRenderer.RenderDataUri("Alain@Example.com", new BlobatarOptions { Hue = 200, Expression = BlobatarExpression.Wink });
        Assert.Equal(expected, cut.Find("img").GetAttribute("src"));
    }

    [Fact]
    public void A_missing_name_is_an_error()
    {
        Assert.Throws<InvalidOperationException>(() => Render<Blobatar>(ps => ps.Add(c => c.Name, string.Empty)));
    }

    [Fact]
    public void No_JavaScript_runs_unless_gaze_is_requested()
    {
        // Strict interop: any call would throw.
        Render<Blobatar>(ps => ps.Add(c => c.Name, "alain").Add(c => c.Animate, BlobatarAnimation.Always).Add(c => c.Expression, BlobatarExpression.Happy));
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public async Task Gaze_attaches_aims_and_stops_with_the_element()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<int>("attach", _ => true).SetResult(7);

        var cut = Render<Blobatar>(ps => ps
            .Add(c => c.Name, "alain")
            .Add(c => c.Animate, BlobatarAnimation.Always)
            .Add(c => c.Gaze, new GazeOptions { Travel = 3, Target = GazeTarget.Pointer }));

        module.VerifyInvoke("attach");
        var travel = module.VerifyInvoke("setTravel");
        Assert.Equal(3.0, travel.Arguments[1]);
        module.VerifyInvoke("remeasure");
        var aim = module.VerifyInvoke("lookAt");
        Assert.Equal(7, aim.Arguments[0]);
        Assert.Equal("pointer", aim.Arguments[1]);

        await cut.Instance.LookAtAsync(new GazePoint(10, 20));
        var aims = module.Invocations["lookAt"];
        var point = aims[aims.Count - 1];
        Assert.Equal("point", point.Arguments[1]);
        Assert.Equal(10.0, point.Arguments[2]);
        Assert.Equal(20.0, point.Arguments[3]);

        // Re-rendering with the same gaze options (a new but equal record) attaches nothing new.
        cut.Render(ps => ps
            .Add(c => c.Name, "alain")
            .Add(c => c.Animate, BlobatarAnimation.Always)
            .Add(c => c.Gaze, new GazeOptions { Travel = 3, Target = GazeTarget.Pointer }));
        Assert.Single(module.Invocations["attach"]);

        // Going still removes the driver: an <img> has no eyes.
        cut.Render(ps => ps.Add(c => c.Name, "alain").Add(c => c.Animate, BlobatarAnimation.None).Add(c => c.Gaze, new GazeOptions { Travel = 3 }));
        var stop = module.VerifyInvoke("stop");
        Assert.Equal(7, stop.Arguments[0]);
        Assert.NotEmpty(cut.FindAll("img"));
    }

    [Fact]
    public void FollowPointer_is_a_pointer_target_with_the_given_travel()
    {
        var gaze = GazeOptions.FollowPointer(12);

        Assert.Equal(12.0, gaze.Travel);
        Assert.Equal(GazeTarget.Pointer, gaze.Target);
        Assert.Null(gaze.Settle);
        Assert.Null(gaze.Snap);
        Assert.Equal(new GazeOptions { Travel = 12, Target = GazeTarget.Pointer }, gaze);

        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<int>("attach", _ => true).SetResult(5);

        Render<Blobatar>(ps => ps
            .Add(c => c.Name, "alain")
            .Add(c => c.Animate, BlobatarAnimation.Always)
            .Add(c => c.Gaze, gaze));

        Assert.Equal(12.0, module.VerifyInvoke("setTravel").Arguments[1]);
        Assert.Equal("pointer", module.VerifyInvoke("lookAt").Arguments[1]);
    }

    [Fact]
    public async Task Disposal_stops_the_driver()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<int>("attach", _ => true).SetResult(3);

        Render<Blobatar>(ps => ps
            .Add(c => c.Name, "alain")
            .Add(c => c.Animate, BlobatarAnimation.Hover)
            .Add(c => c.Gaze, new GazeOptions()));
        module.VerifyInvoke("attach");

        await DisposeComponentsAsync();
        var stop = module.VerifyInvoke("stop");
        Assert.Equal(3, stop.Arguments[0]);
    }
}
