# Project World - Architecture Notes

## Design Philosophy

-   Data drives the game.
-   Unity renders the game.
-   Every class has one responsibility.

## Major Systems

-   WorldGenerator
-   GridManager
-   TurnManager
-   CombatManager
-   UnitManager
-   EconomyManager
-   UIManager

## World Generation Pipeline

WorldGenerator - ContinentGenerator - ElevationGenerator -
RiverGenerator - BiomeGenerator - ResourceGenerator - SpawnGenerator

## Data Model

WorldData - Width - Height - Tiles

TileData - Coordinates - Terrain - Biome - Resource - Improvement -
Elevation - River - Visibility - Exploration

WorldSettings - World Type - Width - Height - Seed - Sea Level -
Temperature - Rainfall
