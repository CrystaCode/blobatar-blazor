using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blobatar.Blazor;

/// <summary>
/// A deterministic geometric avatar for any string.
/// </summary>
/// <remarks>
/// <para>Still by default, rendered as a single <c>&lt;img&gt;</c> carrying a <c>data:</c> URI, which is the
/// right shape for a list of a few hundred. With <see cref="Animate"/> set it renders inline SVG instead,
/// roughly a dozen DOM nodes, because content inside an <c>&lt;img&gt;</c> is an isolated document that
/// <c>:hover</c> and the host page's stylesheet cannot reach. Animation needs
/// <c>_content/Blobatar.Blazor/motion.css</c>; the gaze layer additionally needs
/// <c>_content/Blobatar.Blazor/gaze.css</c>.</para>
/// <code>
/// &lt;Blobatar Name="@user.Email" Size="48" /&gt;
/// &lt;Blobatar Name="@user.Email" Size="48" Animate="BlobatarAnimation.Hover" Expression="BlobatarExpression.Happy" /&gt;
/// &lt;Blobatar @ref="hero" Name="alain" Size="240" Animate="BlobatarAnimation.Always"
///           Gaze="GazeOptions.FollowPointer(12)" /&gt;
/// </code>
/// <para>The component is an adapter: it re-expresses <see cref="BlobatarRenderer"/> and adds nothing of
/// its own, so a parameter left unset reaches the renderer unset and two renderers given the same name and
/// options draw the same blobatar.</para>
/// </remarks>
public partial class Blobatar : ComponentBase, IAsyncDisposable
{
    private const string InteropModulePath = "./_content/Blobatar.Blazor/blobatar-interop.js";

    /// <summary>Attributes a caller may not override because they would break the picture.</summary>
    private static readonly HashSet<string> ReservedAttributes = new(StringComparer.OrdinalIgnoreCase) { "src", "viewBox", "xmlns" };

    private Inputs? _last;
    private string? _src;
    private BlobatarParts? _parts;
    private string? _sizeAttribute;
    private bool _labelled;
    private string? _class;
    private string? _svgStyle;
    private string? _imgStyle;
    private IReadOnlyDictionary<string, object>? _splat;
    private ElementReference _svg;

    private IJSObjectReference? _module;
    private int? _gazeId;
    private string? _attachedSvgId;
    private (double? Settle, double? Snap) _attachedTuning;
    private double? _appliedTravel;
    private bool _travelApplied;
    private GazeTarget? _declaredTarget;
    private bool _declaredApplied;
    private GazeTarget? _target;
    private bool _targetRequested;
    private bool _disposed;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Who the blobatar is for: a username, a display name, an email, a bot's handle, a user id. Any string,
    /// and the same string always renders the same blobatar. The only required parameter.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = default!;

    /// <summary>Pixel size. Emits <c>width</c>/<c>height</c>; omit to let CSS size it.</summary>
    [Parameter]
    public int? Size { get; set; }

    /// <summary>The plate behind the figure. None by default.</summary>
    [Parameter]
    public BlobatarBackground Background { get; set; }

    /// <summary>The plate's fill colour. See <see cref="BlobatarOptions.BackgroundColor"/>.</summary>
    [Parameter]
    public string? BackgroundColor { get; set; }

    /// <summary>Per-slot palette overrides. See <see cref="BlobatarOptions.Palette"/>.</summary>
    [Parameter]
    public BlobatarPalette? Palette { get; set; }

    /// <summary>Locks the hue in degrees. See <see cref="BlobatarOptions.Hue"/>.</summary>
    [Parameter]
    public double? Hue { get; set; }

    /// <summary>Locks the tone as a 0–1 position in the swatch set. See <see cref="BlobatarOptions.Tone"/>.</summary>
    [Parameter]
    public double? Tone { get; set; }

    /// <summary>Pins or narrows individual traits. See <see cref="BlobatarTraits"/>.</summary>
    [Parameter]
    public BlobatarTraits? Traits { get; set; }

    /// <summary>Whether to normalize the name before hashing. Default <see langword="true"/>.</summary>
    [Parameter]
    public bool Normalize { get; set; } = true;

    /// <summary>Whether to enforce the contrast floors. Default <see langword="true"/>.</summary>
    [Parameter]
    public bool Contrast { get; set; } = true;

    /// <summary>
    /// The accessible name. With one the image carries <c>role="img"</c> and a <c>&lt;title&gt;</c>; without
    /// one it is decorative and hidden from assistive technology.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The pose the blobatar holds. Independent of <see cref="Animate"/>; the morph between poses needs animation.</summary>
    [Parameter]
    public BlobatarExpression Expression { get; set; }

    /// <summary>The idle animation mode. Switches the rendering from <c>&lt;img&gt;</c> to inline <c>&lt;svg&gt;</c>.</summary>
    [Parameter]
    public BlobatarAnimation Animate { get; set; }

    /// <summary>The <c>&lt;img&gt;</c>'s <c>alt</c>. Defaults to <see cref="Title"/>, then to empty (decorative).</summary>
    [Parameter]
    public string? Alt { get; set; }

    /// <summary>CSS classes for the rendered element.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style for the rendered element. When animating it is appended after the motion custom properties, so it wins.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// The pointer-driven gaze layer. Only honoured while animating; attaches the JavaScript driver to the
    /// <c>&lt;svg&gt;</c> after it renders, so nothing happens during prerendering or static rendering.
    /// </summary>
    [Parameter]
    public GazeOptions? Gaze { get; set; }

    /// <summary>Any other attribute, passed through to the rendered <c>&lt;img&gt;</c> or <c>&lt;svg&gt;</c>.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The rendered <c>&lt;svg&gt;</c> while animating, for callers that drive their own JavaScript against it. Unset while still.</summary>
    public ElementReference SvgElement => _svg;

    /// <summary>
    /// Points the eyes at something. Remembered if the driver is not attached yet, and handed to it the
    /// moment it is, so calling this before the first render is a queued request rather than a no-op.
    /// </summary>
    /// <param name="target">A <see cref="GazePoint"/>, a <see cref="GazeElement"/>, <see cref="GazeTarget.Pointer"/>, <see cref="GazeTarget.Rest"/> or <see cref="GazeTarget.None"/>.</param>
    public async ValueTask LookAtAsync(GazeTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        _targetRequested = true;
        if (_module is { } module && _gazeId is { } id)
        {
            await ApplyTargetAsync(module, id, target).ConfigureAwait(false);
        }
    }

    /// <summary>Re-measures the blobatar's box, for a host that moved it in a way no observer reports.</summary>
    public async ValueTask RemeasureAsync()
    {
        if (_module is { } module && _gazeId is { } id)
        {
            await Interop(() => module.InvokeVoidAsync("remeasure", id)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"{nameof(Blobatar)} requires a {nameof(Name)}.");
        }

        var inputs = new Inputs(Name, Size, Background, BackgroundColor, Palette, Hue, Tone, Traits, Normalize, Contrast, Title, Expression, Animate);
        if (_last != inputs)
        {
            Recompute(inputs);
            _last = inputs;
        }

        _splat = FilterSplat(AdditionalAttributes, out var splatClass, out var splatStyle);
        _class = Join(" ", Class, splatClass);
        _imgStyle = Join(";", Style, splatStyle);
        _svgStyle = Join(";", _parts?.SerializeVariables(), Style, splatStyle);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (Gaze is null || _parts is null)
        {
            // A still blobatar has no eyes to drive; a driver left on an <img> would measure a box and run a frame loop for nothing.
            await DetachAsync().ConfigureAwait(false);
            return;
        }

        var module = await GetModuleAsync().ConfigureAwait(false);
        if (module is null)
        {
            return;
        }

        var tuning = (Gaze.Settle, Gaze.Snap);
        var attached = false;
        if (_gazeId is null || !string.Equals(_svg.Id, _attachedSvgId, StringComparison.Ordinal) || tuning != _attachedTuning)
        {
            // A new <svg> (the element was recreated) or a new time constant: build a fresh driver and re-aim it.
            await DetachAsync().ConfigureAwait(false);
            var id = await Interop(() => module.InvokeAsync<int>("attach", _svg, Gaze.Settle, Gaze.Snap)).ConfigureAwait(false);
            if (id == 0 || _disposed)
            {
                return;
            }

            _gazeId = id;
            _attachedSvgId = _svg.Id;
            _attachedTuning = tuning;
            _travelApplied = false;
            _declaredApplied = false;
            attached = true;
            if (_targetRequested && _target is { } remembered)
            {
                await ApplyTargetAsync(module, id, remembered).ConfigureAwait(false);
            }
        }

        if (attached || !_travelApplied || _appliedTravel != Gaze.Travel)
        {
            await Interop(() => module.InvokeVoidAsync("setTravel", _svg, Gaze.Travel)).ConfigureAwait(false);
            await Interop(() => module.InvokeVoidAsync("remeasure", _gazeId!.Value)).ConfigureAwait(false);
            _appliedTravel = Gaze.Travel;
            _travelApplied = true;
        }

        if (!_declaredApplied || !Equals(_declaredTarget, Gaze.Target))
        {
            _declaredTarget = Gaze.Target;
            _declaredApplied = true;
            if (Gaze.Target is { } declared)
            {
                await LookAtAsync(declared).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await DetachAsync().ConfigureAwait(false);
        if (_module is { } module)
        {
            _module = null;
            await Interop(() => module.DisposeAsync()).ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }

    private void Recompute(Inputs inputs)
    {
        var options = new BlobatarOptions
        {
            Size = inputs.Size,
            Background = inputs.Background,
            BackgroundColor = inputs.BackgroundColor,
            Palette = inputs.Palette,
            Hue = inputs.Hue,
            Tone = inputs.Tone,
            Traits = inputs.Traits,
            Normalize = inputs.Normalize,
            Contrast = inputs.Contrast,
            Title = inputs.Title,
            Expression = inputs.Expression,
            Animate = inputs.Animate,
        };

        _sizeAttribute = inputs.Size?.ToString(CultureInfo.InvariantCulture);
        _labelled = !string.IsNullOrEmpty(inputs.Title);
        if (inputs.Animate == BlobatarAnimation.None)
        {
            _parts = null;
            _src = BlobatarRenderer.RenderDataUri(inputs.Name, options);
        }
        else
        {
            _src = null;
            _parts = BlobatarRenderer.Parts(inputs.Name, options);
        }
    }

    private async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (_module is not null)
        {
            return _module;
        }

        var module = await Interop(() => JS.InvokeAsync<IJSObjectReference>("import", InteropModulePath)).ConfigureAwait(false);
        if (_disposed)
        {
            if (module is not null)
            {
                await Interop(() => module.DisposeAsync()).ConfigureAwait(false);
            }

            return null;
        }

        _module = module;
        return module;
    }

    private async ValueTask DetachAsync()
    {
        if (_gazeId is { } id && _module is { } module)
        {
            _gazeId = null;
            _attachedSvgId = null;
            await Interop(() => module.InvokeVoidAsync("stop", id)).ConfigureAwait(false);
        }
    }

    private static ValueTask ApplyTargetAsync(IJSObjectReference module, int id, GazeTarget target)
    {
        var (kind, x, y, element) = target.ToInterop();
        return Interop(() => module.InvokeVoidAsync("lookAt", id, kind, x, y, element));
    }

    /// <summary>
    /// Runs one interop call, treating a lost circuit as "nothing to do": the browser already tore the page
    /// down, and there is no element left to drive.
    /// </summary>
    private static async ValueTask Interop(Func<ValueTask> call)
    {
        try
        {
            await call().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static async ValueTask<T?> Interop<T>(Func<ValueTask<T>> call)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            return default;
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (ObjectDisposedException)
        {
            return default;
        }
    }

    private static Dictionary<string, object>? FilterSplat(IReadOnlyDictionary<string, object>? attributes, out string? cls, out string? style)
    {
        cls = null;
        style = null;
        if (attributes is null || attributes.Count == 0)
        {
            return null;
        }

        Dictionary<string, object>? filtered = null;
        foreach (var (key, value) in attributes)
        {
            if (string.Equals(key, "class", StringComparison.OrdinalIgnoreCase))
            {
                cls = value?.ToString();
            }
            else if (string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
            {
                style = value?.ToString();
            }
            else if (!ReservedAttributes.Contains(key))
            {
                (filtered ??= new Dictionary<string, object>(attributes.Count, StringComparer.Ordinal))[key] = value;
            }
        }

        return filtered;
    }

    private static string? Join(string separator, params string?[] parts)
    {
        string? result = null;
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            result = result is null ? part : result + separator + part;
        }

        return result;
    }

    /// <summary>Every parameter that changes the picture, compared by value so a re-render with the same inputs costs no hashing.</summary>
    private readonly record struct Inputs(
        string Name,
        int? Size,
        BlobatarBackground Background,
        string? BackgroundColor,
        BlobatarPalette? Palette,
        double? Hue,
        double? Tone,
        BlobatarTraits? Traits,
        bool Normalize,
        bool Contrast,
        string? Title,
        BlobatarExpression Expression,
        BlobatarAnimation Animate);
}
