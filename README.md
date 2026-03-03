# Starflight - A Space Exploration Game

A C# game built with Raylib inspired by the classic Starflight game mechanics.

## Features

- **Star System Navigation**: Explore multiple star systems in a galaxy map
- **Planetary Exploration**: Land on planets and explore their surfaces
- **Resource Mining**: Discover and mine valuable minerals on planets
- **Fuel Management**: Manage your ship's fuel as you travel between systems
- **Dynamic Economy**: Collect minerals and earn credits

## Controls

### Star Map View
- **WASD / Arrow Keys**: Move camera around the star map
- **Mouse Wheel**: Zoom in/out
- **TAB**: Warp to nearest star system (consumes fuel)
- **ENTER**: Enter planetary exploration mode
- **S**: View ship status

### Planetary Exploration
- **WASD / Arrow Keys**: Move your ship on the planet surface
- **SPACE**: Mine minerals when near them
- **ESC**: Return to star map

### Ship Status Screen
- **ESC / S**: Return to star map

## Building and Running

### Prerequisites
- .NET 8.0 SDK or later
- Linux, Windows, or macOS

### Build and Run
```bash
dotnet restore
dotnet build
dotnet run
```

## Game Mechanics

- **Fuel**: Your ship consumes fuel when moving. Warp travel between systems consumes more fuel.
- **Mining**: Approach minerals on planets and press SPACE to mine them. Each mineral has a value in credits.
- **Exploration**: Each planet has procedurally generated mineral deposits. Explore to find valuable resources!

## Future Enhancements

Potential additions inspired by Starflight:
- Alien encounters and diplomacy
- Ship upgrades and crew management
- More complex planetary terrain
- Trading between systems
- Artifact discovery and quests
