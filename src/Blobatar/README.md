# Blobatar

Deterministic geometric avatars ("blobatars") from any string, rendered as SVG. A faithful C# port of [blobatar](https://blobatar.dev) by Alain: the same name renders the same creature here, in the JavaScript library and at `https://blobatar.dev/avatar/<name>`, byte for byte. No dependencies, trimming- and AOT-friendly.

```csharp
using Blobatar;

string svg = BlobatarRenderer.Render("alain@example.com");
string src = BlobatarRenderer.RenderDataUri(user.Id, new BlobatarOptions { Size = 48, Background = BlobatarBackground.Squircle });
```

Reach for it when something needs a picture of somebody it has no picture of: a user without an avatar, a commit author, a bot, a team, a seat in a list. Nothing is stored and nothing is fetched.

## Options

| Option | Default | Notes |
| --- | --- | --- |
| `Size` | none | Emits `width`/`height`. Omit to let CSS size it. |
| `Background` | `None` | `Square`, `Circle` or `Squircle`. |
| `BackgroundColor` | none | The plate's fill; needs a `Background`. A `#rgb`/`#rrggbb` value keeps the contrast guarantee. |
| `Hue` | none | Locks the hue in degrees; the name then drives shape only. |
| `Tone` | none | Locks the swatch as a 0–1 position, pale to ink (`BlobatarTone.Ink.ToPosition()`). |
| `Traits` | none | Pins or narrows individual traits as 0–1 positions. See below. |
| `Palette` | none | Per-slot hex overrides. Bypasses the contrast guarantee. |
| `Normalize` | `true` | NFC + trim + lowercase before hashing. |
| `Contrast` | `true` | Enforce the contrast floors. |
| `Title` | none | Adds a `<title>` for screen readers. |
| `Expression` | `Idle` | One of fourteen poses. |
| `Animate` | `None` | Honoured by renderers that own the `<svg>` element, such as the `Blobatar.Blazor` component. |

## Configuring

```csharp
// Always a sun with wide eyes; colour and everything else still per name.
var traits = new BlobatarTraits().PinShape(BlobatarShape.Sun).Pin(BlobatarTraitKey.EyeRatio, 0);

// Round, cloud or sun, never the other seven. Which one is still per name.
var house = new BlobatarTraits().NarrowShapes(BlobatarShape.Round, BlobatarShape.Cloud, BlobatarShape.Sun);

BlobatarRenderer.Render(user.Email, new BlobatarOptions { Traits = traits });
```

Pin every trait (`BlobatarTraits.Fixed(0.42)`) and the name stops mattering, which is how you build one fixed blobatar.

## More than markup

- `BlobatarRenderer.Layout(name, options)`: the numeric layout (shape, body, face, eyes, petals) and the resolved palette.
- `BlobatarRenderer.ShapeOf(name)`: just the silhouette, cheap enough to run over thousands of names.
- `BlobatarRenderer.Parts(name, options)`: the pieces a component that owns the `<svg>` needs for animation.
- `TraitReader`, `SeedHash`, `ColorRamp`: the hashing and palette utilities, for anything that wants to derive its own values from a name.

## Guarantees

The same name always renders the same blobatar within a generation: the test suite replays upstream's golden fixture (1312 recorded renders and a 20 000-seed shape histogram) and its cross-language reference vectors, all byte-identical. Eyes clear 4.5:1 against the body at every hue and tone. Names are normalized with JavaScript's exact semantics. No element ids, gradients or filters are used.

MIT. The algorithm is the work of [Alain](https://github.com/Alain00), used under the MIT License (see THIRD-PARTY-NOTICES.md). This port tracks upstream 2.8.0, generation 2.
