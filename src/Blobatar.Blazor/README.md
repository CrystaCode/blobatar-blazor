# Blobatar.Blazor

The `<Blobatar>` component: deterministic geometric avatars from any string, with optional idle animation, fourteen expressions and a pointer-following gaze. The Blazor adapter for [Blobatar](https://www.nuget.org/packages/Blobatar), a byte-identical C# port of [blobatar.dev](https://blobatar.dev).

```razor
@using Blobatar
@using Blobatar.Blazor

<Blobatar Name="@user.Email" Size="48" />
```

Every parameter is optional except `Name`. A still blobatar is a single `<img>` carrying a `data:` URI. Works in static SSR, Interactive Server, WebAssembly and Blazor Hybrid.

## Animation

```html
<link rel="stylesheet" href="_content/Blobatar.Blazor/motion.css" />
```

```razor
<Blobatar Name="@user.Email" Size="48" Animate="BlobatarAnimation.Hover" />
```

Breathe, bob, blink and glance, every timing seeded from the name. `Hover` for grids, `Always` for a single big blobatar. Animation switches the rendering to inline SVG, since `:hover` cannot reach inside an `<img>`. Respects `prefers-reduced-motion`.

## Expressions

```razor
<Blobatar Name="@user.Email" Animate="BlobatarAnimation.Always" Expression="BlobatarExpression.Happy" />
```

`Idle`, `Happy`, `Sad`, `Mad`, `Surprised`, `Wink`, `Sleepy`, `Smug`, `Unsure`, `Scared`, `Love`, `Shy`, `Sick`, `Thinking`. With animation on, the component morphs between poses; without it, the pose is rendered statically.

## Gaze

```html
<link rel="stylesheet" href="_content/Blobatar.Blazor/gaze.css" />
```

```razor
<Blobatar @ref="hero" Name="alain" Size="240" Animate="BlobatarAnimation.Always"
          Gaze="new GazeOptions { Travel = 3, Target = GazeTarget.Pointer }" />

@code {
    Blobatar? hero;
    Task Watch(ElementReference input) => hero!.LookAtAsync(new GazeElement(input)).AsTask();
    Task Rest() => hero!.LookAtAsync(GazeTarget.Rest).AsTask();
}
```

`Travel` (viewBox units, 1.5 to 4 reads well) opts the blobatar in. Targets: `GazePoint`, `GazeElement`, `GazeTarget.Pointer`, `GazeTarget.Rest`, `GazeTarget.None`. The driver attaches on the client after the `<svg>` renders; prerendering is unaffected.

## Parameters

| Parameter | Notes |
| --- | --- |
| `Name` | Required. Who the blobatar is for. |
| `Size`, `Background`, `BackgroundColor`, `Palette`, `Hue`, `Tone`, `Traits`, `Normalize`, `Contrast`, `Title`, `Expression` | Passed to the renderer unchanged; see the `Blobatar` package. |
| `Animate` | `None`, `Hover` or `Always`. |
| `Alt` | The `<img>` alt text. Defaults to `Title`, then to empty (decorative). |
| `Class`, `Style` | Applied to the rendered element. When animating, `Style` is appended after the motion custom properties. |
| `Gaze` | A `GazeOptions`. Only honoured while animating. |
| any other attribute | Passed through to the `<img>` or `<svg>`. |

MIT. The stylesheets and the gaze driver are shipped verbatim from the upstream npm package (Alain, MIT); see THIRD-PARTY-NOTICES.md.
