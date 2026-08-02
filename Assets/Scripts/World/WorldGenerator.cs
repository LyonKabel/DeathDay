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

            // Generate rivers after elevation so sources and downhill directions exist
            GenerateRivers();

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
            // Tailor generation depending on world type. Archipelago uses
            // smaller-scale noise and an additional island scatter pass so
            // it produces many small islands instead of large continents.
            float scale = settings.ContinentScale;
            float seaLevel = settings.SeaLevel;
            float falloff = settings.FalloffStrength;

            if (settings.WorldType == Generation.WorldType.Archipelago)
            {
                // Increase scale for smaller features and raise sea level
                // so more water remains. Reduce falloff so islands are not
                // forced to the center of the map.
                scale = settings.ContinentScale * 3f;
                seaLevel = Mathf.Clamp01(settings.SeaLevel + 0.18f);
                falloff = Mathf.Max(0.5f, settings.FalloffStrength * 0.5f);
            }

            ContinentGenerator continentGenerator = new(
                scale,
                seaLevel,
                falloff,
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

            // Additional island scattering for Archipelago type to create
            // more small, scattered islands.
            if (settings.WorldType == Generation.WorldType.Archipelago)
            {
                System.Random rand = new(settings.Seed + 99999);

                int area = settings.Width * settings.Height;
                int islandCenters = Mathf.Clamp(area / 300, 8, 200);

                int avgDim = (settings.Width + settings.Height) / 2;

                for (int i = 0; i < islandCenters; i++)
                {
                    int cx = rand.Next(0, settings.Width);
                    int cy = rand.Next(0, settings.Height);

                    int radius = Mathf.Clamp(avgDim / 40 + rand.Next(avgDim / 100 + 1), 2, 14);

                    for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(settings.Height - 1, cy + radius); y++)
                    {
                        for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(settings.Width - 1, cx + radius); x++)
                        {
                            TileData t = CurrentWorld.Tiles[x, y];

                            // skip existing water far from center
                            float dx = x - cx;
                            float dy = y - cy;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);

                            if (dist > radius)
                                continue;

                            // probability decreases with distance
                            double chance = 0.85 - (dist / (radius + 1)) * 0.8;

                            if (rand.NextDouble() < chance)
                            {
                                t.Terrain = TerrainType.Grass;
                                // give a small elevation bump so islands are not flat
                                t.Elevation = Mathf.Max(t.Elevation, rand.Next(10, 45));
                            }
                        }
                    }
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
                // Mark river visuals on the tile
                hexTile.HasRiver = tileData.HasRiver;
            }
        }

        private void GenerateRivers()
        {
            RiverGenerator riverGenerator = new(CurrentWorld, settings.Seed);

            riverGenerator.GenerateRivers(
                settings.RiverCount,
                settings.MinRiverSourceElevation,
                settings.MaxRiverLength
            );
        }
    }
}
