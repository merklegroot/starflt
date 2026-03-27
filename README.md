# Starflight - A Space Exploration Game

A game inspired by classic Starflight space exploration game.

## Game sections

The window is split into a **main view** (left) and a **right panel** (menu + ship readouts). **Ship status** is the only mode that uses the full window.

![Main View](img/main-view.png.png)

### Right panel

The sidebar shows the **MENU** (top-level) or **NAVIGATOR** (submenu), then a horizontal rule, then **ship status**: fuel, credits, minerals, speed, and position.

- **Planet** — Opens **planetary encounter** (full-screen planet view).
- **Captain** — No actions yet.
- **Navigator** — Opens the submenu:
  - **Maneuver** — Flight mode in the canopy (engines on).
  - **Starmap** — Tactical star map.

Use **↑/↓** to move the highlight, **1 / 2 / 3** for the top-level items, **SPACE** or **ENTER** to confirm. If the highlighted item already matches the current mode, the same key returns to **canopy view**. **ESC** steps back out of the Navigator submenu (where applicable).

### Canopy view

The default **bridge** view: parallax starfield, nearby star systems in the canopy, and a framed viewport. Engines are off; the hint at the bottom points you to the Navigator menu for the starmap.

### Maneuver

Same visuals as canopy view, but the ship **flies**: turn with **A/D** or **←/→**, forward and reverse thrust with **W/S** or **↑/↓**. Thrust uses **fuel**. **ESC** leaves maneuver and returns to canopy view.

### Starmap

A zoomable **2D map** of star systems with your ship marked. Pan the camera with **WASD** or arrow keys, zoom with the **mouse wheel**, and **TAB** to warp to the **nearest** system (uses fuel when the warp succeeds). **ESC** returns to canopy view. **ENTER** opens **planetary exploration** when the ship is at a star system’s position. **I** opens **ship status**. **X** quits.

### Planetary encounter

Opened from the menu’s **Planet** item. Full-bleed rotating planet render, system name, and encounter title. **ESC** returns to canopy view. **R** regenerates the planet name/preview. **X** quits.

### Planetary exploration

Opened from the **starmap** with **ENTER** while your ship is at a star system. Shows a rotating planet in a **framed panel** (exploration preview). **ESC** returns to the starmap. **R** regenerates the planet. **X** quits.

### Ship status

Full-screen summary: fuel, credits, minerals, and position. Open from the starmap with **I**. **ESC** returns to the starmap. **X** quits.

## Controls (quick reference)

| Context | Keys |
|--------|------|
| Menu / Navigator | ↑↓, 1–3, SPACE/ENTER, ESC |
| Maneuver | A/D or ←/→ turn, W/S or ↑/↓ thrust, ESC to canopy |
| Starmap | WASD or arrows pan, wheel zoom, TAB warp to nearest, ENTER explore planet, I ship status, ESC canopy |
| Planetary encounter / exploration | ESC back, R regenerate planet, X quit |
| Ship status | ESC to starmap, X quit |
| Global | **X** quit (where shown) |

## Building and running

### Prerequisites

- .NET 8.0 SDK or later
- Linux, Windows, or macOS

### Build and run

```bash
dotnet restore
dotnet build
dotnet run
```

## Game mechanics

- **Fuel** — Maneuver thrust consumes fuel; starmap **TAB** warp consumes a fixed chunk of fuel when it succeeds.
- **Systems** — Star data is loaded from embedded JSON; the starmap camera is independent of the ship until you warp or move in maneuver mode.
- **Planets** — Exploration and encounter views show a procedural-style rotating sphere; **R** picks a new generated name and refreshes the view.
