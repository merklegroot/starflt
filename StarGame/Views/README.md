# Views

UI and world rendering for the main game window. Each type’s responsibilities are summarized in XML doc comments on the class.

| Type | Role |
|------|------|
| `StarMapView` | Embedded star data, map camera/zoom/warp, 2D map draw |
| `CanopyStarSystemView` | Cockpit: systems around center, ship, particles |
| `RightPanel` | Right sidebar: menu + status |
| `StatusPanel` | Fuel, credits, minerals, speed, position text |
| `PlanetView` | Planet sphere preview / encounter full-bleed |

`GameMenu` lives at the project root and is composed by `RightPanel`.
