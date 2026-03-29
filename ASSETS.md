# Third-party assets

This file lists assets **from outside the project** with their license and source. **Fonts** in `StarGame/Fonts/` are committed. **Tiny Ships** textures are **not** committed (see [.gitignore](/.gitignore)); obtain them locally as below. Add a row here whenever you import a new asset.

| Asset | Location in repo | Source | License | Notes |
|--------|------------------|--------|---------|--------|
| Open Sans (variable font) | `StarGame/Fonts/OpenSans.ttf` | [Google Fonts — Open Sans](https://github.com/google/fonts/tree/main/ofl/opensans) (`OpenSans[wdth,wght].ttf`) | [SIL Open Font License 1.1](https://openfontlicense.org/) | Full license text: `StarGame/Fonts/OpenSans-OFL.txt`. Used by `UiText` for UI text. |
| **Tiny Ships** (sprite sheets `tinyShip1.png`–`tinyShip20.png`) | **Local only:** `StarGame/Textures/tiny-spaceships/` (gitignored) | [Tiny Spaceships (FREE) — Disruptor Art on itch.io](https://disruptorart.itch.io/tiny-ships-free-spaceships) · readme in pack: `Read Me, Commander.pdf` | **From pack readme:** use freely for any purpose **except reselling the pack**. *(The itch.io page may state additional terms such as CC BY—follow the license shown on the download page and in the pack.)* | Animated strips: frame sizes and `idle` / `move` / `attack` per sheet are in the PDF. **1 px padding** between frames (right and bottom). `StarflightGame.csproj` copies this folder to build output when present. Load at runtime with e.g. `Path.Combine(AppContext.BaseDirectory, "Textures", "tiny-spaceships", "tinyShip1.png")`. |
| *(Optional reference)* **[8×8] Space Shooter Asset Pack** | — | [Gustavo Vituri on itch.io](https://gvituri.itch.io/space-shooter) | **From itch page:** free for personal and commercial use; modify freely; **credit optional**; **do not redistribute** the asset pack standalone; **do not** use for NFTs. | Different art (`SpaceShooterAssetPack_*.png`). Not wired into this project unless you add paths and entries here. |

## How to obtain gitignored assets (Tiny Ships)

1. **Download** [Tiny Spaceships (FREE Space Ships)](https://disruptorart.itch.io/tiny-ships-free-spaceships) from itch.io (no purchase required for the free download).
2. **Unpack** the archive `tiny-spaceships.zip` (or extract the same files from the Unity package if you only use that—this project expects the PNG sheets).
3. **Install into the repo:** copy the extracted folder so that **`tinyShip1.png` … `tinyShip20.png` and `Read Me, Commander.pdf` sit directly in**  
   `StarGame/Textures/tiny-spaceships/`  
   (not nested inside another `tiny-spaceships` folder).
4. **Build** with `dotnet build` from `StarGame/` (or your usual solution build). The `.csproj` copies `Textures/tiny-spaceships/**` to the output directory next to the executable.

If you keep a copy of `tiny-spaceships.zip` at the **repository root**, that file is gitignored as well—do not commit it.

### If you were looking for a different pack

- **[8×8] Space Shooter Asset Pack** ([Gustavo Vituri](https://gvituri.itch.io/space-shooter)): download `SpaceShooterAssetPack.zip` from that page; it is **not** the same as Tiny Ships. To use it, extract under a new folder (e.g. `StarGame/Textures/space-shooter-vituri/`), add a `ASSETS.md` row, add a matching `<None Include=... CopyToOutputDirectory>` in `StarflightGame.csproj`, and add the folder to `.gitignore` if you do not want it in git.

## How to add an entry

1. Place the file under a clear path (e.g. `StarGame/Fonts/`, `StarGame/Textures/`).
2. If the license requires it, add the project’s license file beside the asset (or a `*-OFL.txt` / `LICENSE` copy).
3. Reference it in the project file (`StarflightGame.csproj`) if it must be copied to output.
4. Append a row to the table above with **source URL or project**, **license name + link**, and **short notes**.

## NuGet packages

Runtime dependencies (e.g. Raylib-cs, Microsoft.Extensions.Hosting) are governed by their respective package licenses on NuGet; they are not listed here as vendored assets.
