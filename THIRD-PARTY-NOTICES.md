# Third-party notices

This repository is a C# port of **blobatar** by Alain (<https://github.com/Alain00/blobatar>, <https://blobatar.dev>), released under the MIT License.

The following are derived from, or copied verbatim from, that project:

- The rendering algorithm in `src/Blobatar` (hashing, trait reading, silhouette geometry, OKLCh palette, expression poses, motion variables and SVG serialization), ported line by line from `packages/blobatar/src` at version 2.8.0, with its documentation comments carried over.
- `src/Blobatar.Blazor/wwwroot/motion.css`, `gaze.css` and `gaze.js`: the built stylesheets and gaze driver published in the `blobatar` npm package, version 2.7.0, shipped verbatim (source-map comments removed) with an attribution header.
- `tests/Blobatar.Tests/Fixtures/gen2.txt` and `reference-vectors.json`: the golden fixture and the cross-language reference vectors from the upstream test suites, used here to prove the port is byte-identical.

```
MIT License

Copyright (c) 2026 Alain

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR OTHER DEALINGS IN THE SOFTWARE.
```
