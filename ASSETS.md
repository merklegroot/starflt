# Third-party assets

This file lists **files shipped in the repo** (or copied to build output) that come from outside the project, with their license and source. Add a row here whenever you import a new asset.

| Asset | Location in repo | Source | License | Notes |
|--------|------------------|--------|---------|--------|
| Open Sans (variable font) | `StarGame/Fonts/OpenSans.ttf` | [Google Fonts — Open Sans](https://github.com/google/fonts/tree/main/ofl/opensans) (`OpenSans[wdth,wght].ttf`) | [SIL Open Font License 1.1](https://openfontlicense.org/) | Full license text: `StarGame/Fonts/OpenSans-OFL.txt`. Used by `UiText` for UI text. |

## How to add an entry

1. Place the file under a clear path (e.g. `StarGame/Fonts/`, `StarGame/Textures/`).
2. If the license requires it, add the project’s license file beside the asset (or a `*-OFL.txt` / `LICENSE` copy).
3. Reference it in the project file (`StarflightGame.csproj`) if it must be copied to output.
4. Append a row to the table above with **source URL or project**, **license name + link**, and **short notes**.

## NuGet packages

Runtime dependencies (e.g. Raylib-cs, Microsoft.Extensions.Hosting) are governed by their respective package licenses on NuGet; they are not listed here as vendored assets.
