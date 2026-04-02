# Third-party assets

This file lists **files in the repo** (or copied to build output) that come from outside the project, with their license and source. Add a row here whenever you import a new asset.

| Asset | Location in repo | Source | License | Notes |
|--------|------------------|--------|---------|--------|
| Open Sans (variable font) | `StarGame/Fonts/OpenSans.ttf` | [Google Fonts — Open Sans](https://github.com/google/fonts/tree/main/ofl/opensans) (`OpenSans[wdth,wght].ttf`) | [SIL Open Font License 1.1](https://openfontlicense.org/) | Full license text: `StarGame/Fonts/OpenSans-OFL.txt`. Used by `UiText` for UI text. |
| **Tiny Ships** (sprite sheets `tinyShip1.png`–`tinyShip20.png`) | `StarGame/Textures/tiny-spaceships/` ([Git LFS](https://git-lfs.com/); see [.gitattributes](/.gitattributes)) | [Tiny Spaceships (FREE) — Disruptor Art on itch.io](https://disruptorart.itch.io/tiny-ships-free-spaceships) · readme in pack: `Read Me, Commander.pdf` | **From pack readme:** use freely for any purpose **except reselling the pack**. *(The itch.io page may state additional terms such as CC BY—follow the license shown on the download page and in the pack.)* | Animated strips: frame sizes and `idle` / `move` / `attack` per sheet are in the PDF. **1 px padding** between frames (right and bottom). `StarflightGame.csproj` copies this folder to build output. Load at runtime with e.g. `Path.Combine(AppContext.BaseDirectory, "Textures", "tiny-spaceships", "tinyShip1.png")`. |
| **Saturn ring** (`saturn_ring_alpha.png`) | `StarGame/Textures/solarsystemscope/` · attribution: `ATTRIBUTION.txt` | [Solar System Scope — Solar Textures](https://www.solarsystemscope.com/textures/) | [**CC BY 4.0**](https://creativecommons.org/licenses/by/4.0/) — “You may use, adapt, and share these textures for any purpose, even commercially” (see site). | **Source:** `https://www.solarsystemscope.com/textures/download/2k_saturn_ring_alpha.png` (2K). **In-repo:** resized to **512×31** (LANCZOS) for a smaller build; swap in the 2K file if you need more detail. Used for **all** ringed planets (see `RingTextureResources`). **MIT + CC BY:** game *source* stays MIT; this *asset* stays under CC BY 4.0—keep `ATTRIBUTION.txt` (or equivalent credit) with distributions that include the PNG. |
| *(Optional reference)* **[8×8] Space Shooter Asset Pack** | — | [Gustavo Vituri on itch.io](https://gvituri.itch.io/space-shooter) | **From itch page:** free for personal and commercial use; modify freely; **credit optional**; **do not redistribute** the asset pack standalone; **do not** use for NFTs. | Different art (`SpaceShooterAssetPack_*.png`). Not wired into this project unless you add paths and entries here. |

## Git LFS (Tiny Ships binaries)

PNG and PDF files under `StarGame/Textures/tiny-spaceships/` are stored with **Git LFS**. Install [Git LFS](https://git-lfs.com/) and run **`git lfs install`** once per machine, then clone or pull as usual so LFS objects are downloaded. If you **change or add** files in that folder, install Git LFS *before* `git add` so they are stored as LFS objects (not full blobs in git history).

### If you were looking for a different pack

- **[8×8] Space Shooter Asset Pack** ([Gustavo Vituri](https://gvituri.itch.io/space-shooter)): download `SpaceShooterAssetPack.zip` from that page; it is **not** the same as Tiny Ships. To use it, extract under a new folder (e.g. `StarGame/Textures/space-shooter-vituri/`), add a `ASSETS.md` row, add a matching `<None Include=... CopyToOutputDirectory>` in `StarflightGame.csproj`, and add the folder to `.gitignore` if you do not want it in git.

## How to add an entry

1. Place the file under a clear path (e.g. `StarGame/Fonts/`, `StarGame/Textures/`).
2. If the license requires it, add the project’s license file beside the asset (or a `*-OFL.txt` / `LICENSE` copy).
3. Reference it in the project file (`StarflightGame.csproj`) if it must be copied to output.
4. Append a row to the table above with **source URL or project**, **license name + link**, and **short notes**.

## NuGet packages

Runtime dependencies (e.g. Raylib-cs, Microsoft.Extensions.Hosting) are governed by their respective package licenses on NuGet; they are not listed here as vendored assets.
