using HexTactics.Grid;
using HexTactics.World.Data;
using HexTactics.World.Generation;
using HexTactics.World.Settings;
using UnityEngine;

namespace HexTactics.World
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HexGridManager gridManager;

        [Header("World Settings")]
        [SerializeField] private WorldSettings settings = new();

        public WorldData CurrentWorld { get; private set; }

        private void Start()
        {
            GenerateWorld();
        }

        [ContextMenu("Generate World")]
        public void GenerateWorld()
        {
            if (gridManager == null)
            {
                Debug.LogError(
                    "WorldGenerator is missing its HexGridManager reference."
                );

                return;
            }

            if (gridManager.Tiles.Count == 0)
            {
                Debug.LogError(
                    "The hex grid has no tiles. Generate the grid first."
                );

                return;
            }

            settings.Width = gridManager.Columns;
            settings.Height = gridManager.Rows;

            if (settings.RandomizeSeed)
            {
                settings.Seed = Random.Range(
                    -1_000_000_000,
                    1_000_000_000
                );
            }

            CurrentWorld = CreateEmptyWorld();

            switch (settings.WorldType)
            {
                case WorldType.Continents:
                    GenerateContinents();
                    break;

                case WorldType.Pangaea:
                    Debug.LogWarning(
                        "Pangaea is not implemented yet. Using Continents."
                    );
                    GenerateContinents();
                    break;

                case WorldType.Archipelago:
                    Debug.LogWarning(
                        "Archipelago is not implemented yet. Using Continents."
                    );
                    GenerateContinents();
                    break;

                case WorldType.InlandSea:
                    Debug.LogWarning(
                        "Inland Sea is not implemented yet. Using Continents."
                    );
                    GenerateContinents();
                    break;

                case WorldType.Fractured:
                    Debug.LogWarning(
                        "Fractured is not implemented yet. Using Continents."
                    );
                    GenerateContinents();
                    break;

                default:
                    Debug.LogError(
                        $"Unsupported world type: {settings.WorldType}"
                    );
                    return;
            }
            GenerateElevation();
            RenderWorld();

            Debug.Log(
                $"World generated. Type: {settings.WorldType}, " +
                $"Size: {CurrentWorld.Width}x{CurrentWorld.Height}, " +
                $"Seed: {settings.Seed}"
            );
        }

        private WorldData CreateEmptyWorld()
        {
            WorldData world = new()
            {
                Width = settings.Width,
                Height = settings.Height,
                Tiles = new TileData[
                    settings.Width,
                    settings.Height
                ]
            };

            for (int row = 0; row < settings.Height; row++)
            {
                for (int column = 0;
                     column < settings.Width;
                     column++)
                {
                    world.Tiles[column, row] = new TileData
                    {
                        Column = column,
                        Row = row,
                        Terrain = TerrainType.Water,
                        Biome = BiomeType.Plains,
                        Resource = ResourceType.None,
                        Improvement = ImprovementType.None,
                        Elevation = 0,
                        HasRiver = false,
                        Explored = false,
                        Visible = false
                    };
                }
            }

            return world;
        }

        private void GenerateElevation()
        {
            ElevationGenerator elevationGenerator = new(
                settings.ElevationScale,
                settings.Seed
            );

            for (int row = 0; row < CurrentWorld.Height; row++)
            {
                for (int column = 0;
                     column < CurrentWorld.Width;
                     column++)
                {
                    TileData tileData =
                        CurrentWorld.Tiles[column, row];

                    if (tileData.Terrain == TerrainType.Water)
                    {
                        tileData.Elevation = 0;
                        continue;
                    }

                    float elevation = elevationGenerator.GetElevation(
                        column,
                        row,
                        CurrentWorld.Width,
                        CurrentWorld.Height
                    );

                    tileData.Elevation =
                        Mathf.RoundToInt(elevation * 100f);

                    if (elevation >= settings.MountainThreshold)
                    {
                        tileData.Terrain = TerrainType.Mountain;
                    }
                    else if (elevation >= settings.HillThreshold)
                    {
                        tileData.Terrain = TerrainType.Hill;
                    }
                    else
                    {
                        tileData.Terrain = TerrainType.Grass;
                    }
                }
            }
        }

        private void GenerateContinents()
        {
            ContinentGenerator continentGenerator = new(
                settings.ContinentScale,
                settings.SeaLevel,
                settings.FalloffStrength,
                settings.Seed
            );

            for (int row = 0; row < CurrentWorld.Height; row++)
            {
                for (int column = 0;
                     column < CurrentWorld.Width;
                     column++)
                {
                    bool isLand = continentGenerator.IsLand(
                        column,
                        row,
                        CurrentWorld.Width,
                        CurrentWorld.Height
                    );

                    TileData tileData =
                        CurrentWorld.Tiles[column, row];

                    tileData.Terrain = isLand
                        ? TerrainType.Grass
                        : TerrainType.Water;
                }
            }
        }

        private void RenderWorld()
        {
            foreach (HexTile hexTile in gridManager.Tiles.Values)
            {
                TileData tileData = CurrentWorld.Tiles[
                    hexTile.Column,
                    hexTile.Row
                ];

                hexTile.Terrain = tileData.Terrain;
                hexTile.Biome = tileData.Biome;
                hexTile.Resource = tileData.Resource;
                hexTile.Improvement = tileData.Improvement;
                hexTile.Elevation = tileData.Elevation;
            }
        }
    }
}