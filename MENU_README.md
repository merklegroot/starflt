# Menuing System

A hierarchical menu system for navigating game views and modes.

## Structure

**Top Level Menu:**
- Captain
- Navigator

**Navigator Submenu:**
- Maneuver
- Starmap

## Controls

- **Arrow Keys (↑/↓)**: Navigate menu items (disabled in StarMap/Maneuver modes)
- **Number Keys (1/2)**: Quick select top-level items
- **SPACE/ENTER**: Select menu item, enter submenu, or exit active item (if currently active item is selected)
- **ESC**: Go back to previous menu level or exit active game mode

## Features

- **Visual State Indicators**: Menu items use a box-within-a-box design to indicate state:
  - **Focused State**: Outer box is filled when a menu item is selected (navigated to)
  - **Active State**: Inner box is filled when a menu item represents the current game mode/state
  - When both focused and active, both boxes are filled
- Visual selection highlighting (yellow text when focused)
- Menu state persists when switching game views
- Context-aware navigation (arrow keys disabled during gameplay)
