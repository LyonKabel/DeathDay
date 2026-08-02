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
                // Update river state; disable per-edge placeholder visuals so
                // we only show the continuous river lines created below.
                hexTile.HasRiver = tileData.HasRiver;
                hexTile.SetRiverEdges(new bool[6]);
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

            CreateRiverVisuals(riverGenerator.Rivers);
        }

        private void CreateRiverVisuals(System.Collections.Generic.IReadOnlyList<System.Collections.Generic.List<UnityEngine.Vector2Int>> rivers)
        {
            // Remove existing river parent if any
            Transform existing = gridManager.transform.Find("Rivers");
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            GameObject riversParent = new("Rivers");
            riversParent.transform.SetParent(gridManager.transform, false);

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material riverMat = new(shader) { color = new Color(0.06f, 0.48f, 0.8f, 1f) };

            foreach (var path in rivers)
            {
                if (path == null || path.Count < 2)
                    continue;

                // Build world-space positions from tile centers
                var positions = new System.Collections.Generic.List<UnityEngine.Vector3>();

                foreach (var coord in path)
                {
                    if (gridManager.Tiles.TryGetValue(coord, out var hex))
                    {
                        positions.Add(hex.transform.position + Vector3.up * 0.02f);
                    }
                }

                if (positions.Count < 2)
                    continue;

                // Optional smoothing: Catmull-Rom interpolation
                var smooth = SmoothPath(positions, 4);

                GameObject go = new GameObject("River");
                go.transform.SetParent(riversParent.transform, false);

                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.material = riverMat;
                lr.positionCount = smooth.Count;
                lr.SetPositions(smooth.ToArray());

                float baseWidth = 0.12f;
                if (smooth.Count >= 2)
                    baseWidth = Mathf.Clamp(Vector3.Distance(smooth[0], smooth[1]) * 0.22f, 0.03f, 0.22f);

                lr.startWidth = lr.endWidth = baseWidth;
                lr.numCapVertices = 6;
                lr.numCornerVertices = 6;
                lr.startColor = lr.endColor = new Color(0.05f, 0.45f, 0.9f, 1f);
            }
        }

        private System.Collections.Generic.List<UnityEngine.Vector3> SmoothPath(System.Collections.Generic.List<UnityEngine.Vector3> pts, int subdivisions)
        {
            var result = new System.Collections.Generic.List<UnityEngine.Vector3>();

            if (pts.Count < 2)
            {
                return pts;
            }

            // For endpoints, duplicate end control points to produce proper tangents
            for (int i = 0; i < pts.Count - 1; i++)
            {
                UnityEngine.Vector3 p0 = i == 0 ? pts[i] : pts[i - 1];
                UnityEngine.Vector3 p1 = pts[i];
                UnityEngine.Vector3 p2 = pts[i + 1];
                UnityEngine.Vector3 p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];

                for (int j = 0; j <= subdivisions; j++)
                {
                    float t = j / (float)subdivisions;
                    // Catmull-Rom
                    float t2 = t * t;
                    float t3 = t2 * t;

                    UnityEngine.Vector3 point = 0.5f * (
                        (2f * p1) +
                        (-p0 + p2) * t +
                        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                        (-p0 + 3f * p1 - 3f * p2 + p3) * t3);

                    // Avoid duplicates at joins
                    if (result.Count == 0 || (result[result.Count - 1] - point).sqrMagnitude > 0.0001f)
                        result.Add(point);
                }
            }

            // ensure last point
            if (!result.Contains(pts[pts.Count - 1]))
                result.Add(pts[pts.Count - 1]);

            return result;
        }
    }
}
