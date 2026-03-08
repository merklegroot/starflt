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
- **SPACE/ENTER**: Select menu item or enter submenu
- **ESC**: Go back to previous menu level

## Features

- Visual selection highlighting (yellow text with background)
- Active state indicators (●) show current game mode
- Menu state persists when switching game views
- Context-aware navigation (arrow keys disabled during gameplay)
