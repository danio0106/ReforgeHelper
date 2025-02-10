# ReforgeHelper Plugin for Path of Exile 2

An advanced automation plugin for the reforging bench mechanic in Path of Exile 2, featuring smart item grouping and human-like interactions.

## Core Features

- Smart Triplet Formation
  - Groups compatible items based on base type, rarity, and item level
  - Configurable item level disparity threshold
  - Automatic rarity validation per item type
  - Support for multiple item categories

- Bench Interaction
  - Automatic bench detection and element location
  - Human-like mouse movements with randomized paths
  - Intelligent slot management
  - Emergency stop functionality

- Supported Item Types
  - Soul Cores
  - Jewels (All types)
  - Rings
  - Amulets
  - Waystones
  - Supports both normal and magic/rare items

## Installation

1. Clone the repository to your POE2 plugins directory
2. Build the solution using Visual Studio or your preferred .NET IDE
3. Enable the plugin through ExileCore2's plugin manager

## Usage

1. Configure the plugin settings in ExileCore2
2. Open the reforging bench in-game
3. Place compatible items in your inventory
4. Press the configured hotkey (default: F6) to start/stop automation
5. Use emergency stop (default: F7) if needed

## Settings

### Core Settings
- Enable/Disable plugin
- Debug logging toggle
- Hotkey configuration
- Mouse movement parameters

### Item Filters
- Min/Max item level
- Maximum item level disparity
- Item category toggles
  - Soul Cores
  - Jewels
  - Rings
  - Amulets
  - Waystones

## Requirements

- ExileCore2
- .NET Framework (compatible version)
- Path of Exile 2 game client

## Development

The plugin is built with a modular architecture:
- `ReforgeHelper.cs`: Core plugin logic and UI interaction
- `TripletManager`: Smart item grouping and validation
- `ReforgeBenchHelper`: Bench element detection
- `ItemSubtypes`: Item categorization and validation

## Safety Features

- Human-like mouse movements
- Randomized delays between actions
- Automatic validation of bench and inventory state
- Process monitoring and safe interruption

## License

Licensed under the MIT License