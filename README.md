# Blobatar for .NET and Blazor

Deterministic geometric avatars ("blobatars") from any string, as a pure C# library and a Blazor component. A faithful port of [blobatar](https://blobatar.dev) by Alain: the same name renders the same creature here, in the JavaScript library, and at `https://blobatar.dev/avatar/<name>`, byte for byte.

```razor
<Blobatar Name="@user.Email" Size="48" />
<Blobatar Name="@user.Email" Size="48" Animate="BlobatarAnimation.Hover" Expression="BlobatarExpression.Happy" />
```

```csharp
string svg = BlobatarRenderer.Render("alain@example.com");
string src = BlobatarRenderer.RenderDataUri(user.Id, new BlobatarOptions { Size = 48, Background = BlobatarBackground.Squircle });
```

## Packages

| Package | What it is |
| --- | --- |
| `Blobatar` | The renderer: a string in, SVG markup (or a `data:` URI, or the numeric layout) out. No dependencies, trimming- and AOT-friendly, `net8.0`/`net9.0`/`net10.0`. Use it in ASP.NET endpoints, MAUI, console apps, anywhere. |
| `Blobatar.Blazor` | The `<Blobatar>` Razor component plus the motion and gaze assets. References `Blobatar`. Works in static SSR, Interactive Server, WebAssembly and Hybrid. |

```sh
dotnet add package Blobatar.Blazor      # pulls in Blobatar
dotnet add package Blobatar             # the renderer alone
```

## Using the component

Add the namespaces to `_Imports.razor`:

```razor
@using Blobatar
@using Blobatar.Blazor
```

A still blobatar renders as a single `<img>` carrying a `data:` URI, which is the right shape for a list of a few hundred. Everything is optional except `Name`:

```razor
<Blobatar Name="@user.Email" Size="48" Title="@user.DisplayName" Background="BlobatarBackground.Circle" />
```

### Animation

Opt in with `Animate`, and load the stylesheet once in your host page:

```html
<link rel="stylesheet" href="_content/Blobatar.Blazor/motion.css" />
<!-- Blazor Web App (.NET 9+): href="@Assets["_content/Blobatar.Blazor/motion.css"]" -->
```

```razor
<Blobatar Name="@user.Email" Size="48" Animate="BlobatarAnimation.Hover" />
<Blobatar Name="@user.Email" Size="200" Animate="BlobatarAnimation.Always" />
```

The blobatar then idles: a breathe, a bob, a blink and the occasional glance, every timing seeded from the name so a grid reads as a crowd. `Hover` animates one at a time, the right default for a grid; `Always` is for the single-blobatar case. Turning animation on changes the rendering from one `<img>` to inline SVG (about a dozen nodes), because `:hover` cannot reach inside an image. Motion respects `prefers-reduced-motion`.

### Expressions

Fourteen poses, held until you change them. With animation on, the component morphs between any two; without it, the pose is rendered statically:

```razor
<Blobatar Name="@user.Email" Animate="BlobatarAnimation.Always" Expression="@mood" />
```

`Idle`, `Happy`, `Sad`, `Mad`, `Surprised`, `Wink`, `Sleepy`, `Smug`, `Unsure`, `Scared`, `Love`, `Shy`, `Sick`, `Thinking`. An expression is a state, not an event: nothing returns to idle on its own.

### Following the pointer

The gaze layer needs a second stylesheet and a little JavaScript, which the package carries:

```html
<link rel="stylesheet" href="_content/Blobatar.Blazor/gaze.css" />
```

```razor
<Blobatar @ref="hero" Name="alain" Size="240" Animate="BlobatarAnimation.Always"
          Gaze="GazeOptions.FollowPointer(12)" />

@code {
    Blobatar? hero;
    // the option is where it usually looks; the method is where it looks now
    Task Watch(ElementReference input) => hero!.LookAtAsync(new GazeElement(input)).AsTask();
    Task Rest() => hero!.LookAtAsync(GazeTarget.Rest).AsTask();
}
```

`GazeOptions.FollowPointer(travel)` is the common case: the eyes follow the pointer wherever it is on the page, as the hero on blobatar.dev does. `Travel` is the excursion in viewBox units (the face is 100 across) and is what opts a blobatar in: 12 is what that hero uses and reads as a deliberate look, 3 is a glance you have to watch for. The full record also takes `Settle`, `Snap` and any `Target`: a `GazePoint`, a `GazeElement`, `GazeTarget.Pointer`, `GazeTarget.Rest` (its own centre, held) or `GazeTarget.None`. The driver attaches only on the client after the `<svg>` renders, so prerendering and static rendering are unaffected.

### Configuring

Every axis of a blobatar is a named trait, and `Traits` pins or narrows any of them. Keys you leave out still come from the name:

```csharp
// Always a sun with wide eyes; colour and everything else still per name.
var traits = new BlobatarTraits().PinShape(BlobatarShape.Sun).Pin(BlobatarTraitKey.EyeRatio, 0);

// Round, cloud or sun, never the other seven. Which one is still per name.
var house = new BlobatarTraits().NarrowShapes(BlobatarShape.Round, BlobatarShape.Cloud, BlobatarShape.Sun);
```

`Hue` (degrees) and `Tone` (a 0–1 position in the six swatches, or `BlobatarTone.Ink.ToPosition()`) state the two colour traits in friendlier units. `Palette` overrides colours outright and bypasses the contrast guarantee; `BackgroundColor` colours the plate and keeps it.

## What it guarantees

- **Determinism.** The same name always renders the same blobatar within a generation. The test suite replays upstream's golden fixture (1312 recorded renders and a 20 000-seed shape histogram) and its cross-language reference vectors, and all of it matches byte for byte.
- **Contrast.** Eyes clear 4.5:1 against the body at every hue and tone; polarity flips so the near-black tone gets light eyes. Verified at 1° hue resolution.
- **Name normalization.** NFC, trim and lowercase, with JavaScript's semantics (including `İ` and the final sigma), so `Alain@Example.com` and `alain@example.com` agree. Pass `Normalize="false"` for case-sensitive ids.
- **No element ids.** Nothing uses `<defs>`, gradients or filters, so hundreds of blobatars on one page cannot collide.

## The renderer on its own

```csharp
using Blobatar;

var svg = BlobatarRenderer.Render("alain");                       // "<svg xmlns=... viewBox=\"0 0 100 100\">…</svg>"
var uri = BlobatarRenderer.RenderDataUri("alain");                // "data:image/svg+xml,…"
var layout = BlobatarRenderer.Layout("alain");                    // shape, body, eyes, petals, palette
var shape = BlobatarRenderer.ShapeOf("alain");                    // BlobatarShape.Organic
var parts = BlobatarRenderer.Parts("alain", new BlobatarOptions { Animate = BlobatarAnimation.Hover }); // the adapter seam
```

`BlobatarOptions` carries `Size`, `Background`, `BackgroundColor`, `Palette`, `Hue`, `Tone`, `Traits`, `Normalize`, `Contrast`, `Title`, `Expression` and `Animate`; it is a record, so two option sets that draw the same picture compare equal.

## Repository

```
src/Blobatar              the renderer (ported from packages/blobatar/src upstream)
src/Blobatar.Blazor       the component, blobatar-interop.js, and upstream's motion.css / gaze.css / gaze.js
tests/Blobatar.Tests      golden fixture, parity vectors, and the upstream test suites ported to xUnit
tests/Blobatar.Blazor.Tests   bUnit tests for the component
samples/Blobatar.Demo     a Blazor Web App: crowd, expressions, editor, gaze
tools/sync-upstream-assets.ps1   re-fetches the upstream assets and fixtures
```

```sh
dotnet build
dotnet test
dotnet pack src/Blobatar -c Release -o artifacts
dotnet pack src/Blobatar.Blazor -c Release -o artifacts
dotnet run --project samples/Blobatar.Demo
dotnet test --filter Category=Network     # opt-in: compares against the deployed blobatar.dev endpoint
```

### Releasing

Releases are cut from tags. Bump `<VersionPrefix>` in `Directory.Build.props`, merge, then tag the merge commit:

```sh
git tag v0.1.0
git push origin v0.1.0
```

[`release.yml`](.github/workflows/release.yml) builds and tests on the tag, packs both packages with the tag's version (a `v0.2.0-preview.1` tag gives a prerelease), pushes them to NuGet.org and attaches them to a GitHub release. It refuses a tag whose version does not match `VersionPrefix`, so the repository and the package never disagree about what was shipped.

One-time setup: create an API key on [nuget.org](https://www.nuget.org/account/apikeys) with the *Push* permission, scoped to the glob `Blobatar*`, and store it as the `NUGET_API_KEY` repository secret (Settings → Secrets and variables → Actions).

## Credits and license

MIT. The algorithm, the stylesheets, the gaze driver and the fixtures are the work of [Alain](https://github.com/Alain00) and are used under the MIT License; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). This port tracks upstream 2.8.0 (generation 2).
