using System;
using System.Collections.Generic;
using HexTactics.World.Data;
using UnityEngine;

namespace HexTactics.World.Generation
{
    public class RiverGenerator
    {
        private readonly WorldData world;
        private readonly System.Random random;
        // Stored river paths as lists of tile coordinates (column,row)
        private readonly List<List<UnityEngine.Vector2Int>> rivers = new();

        private static readonly Vector2Int[] EvenRowDirections =
        {
            new(-1, 0),
            new(1, 0),
            new(-1, -1),
            new(0, -1),
            new(-1, 1),
            new(0, 1)
        };

        private static readonly Vector2Int[] OddRowDirections =
        {
            new(-1, 0),
            new(1, 0),
            new(0, -1),
            new(1, -1),
            new(0, 1),
            new(1, 1)
        };

        public RiverGenerator(WorldData world, int seed)
        {
            this.world = world;
            random = new System.Random(seed + 47_921);
        }

        public IReadOnlyList<List<UnityEngine.Vector2Int>> Rivers => rivers;

        public void GenerateRivers(
            int riverCount,
            int minimumSourceElevation,
            int maximumRiverLength)
        {
            ClearExistingRivers();

            List<TileData> possibleSources = FindPossibleSources(
                minimumSourceElevation
            );

            Shuffle(possibleSources);

            int generatedRivers = 0;

            foreach (TileData source in possibleSources)
            {
                if (generatedRivers >= riverCount)
                {
                    break;
                }

                if (TryCreateRiver(source, maximumRiverLength))
                {
                    generatedRivers++;
                }
            }

            Debug.Log(
                $"Generated {generatedRivers} of {riverCount} requested rivers."
            );
        }

        private List<TileData> FindPossibleSources(
            int minimumSourceElevation)
        {
            List<TileData> sources = new();

            for (int row = 0; row < world.Height; row++)
            {
                for (int column = 0; column < world.Width; column++)
                {
                    TileData tile = world.Tiles[column, row];

                    if (tile.Terrain == TerrainType.Water)
                    {
                        continue;
                    }

                    if (tile.Elevation < minimumSourceElevation)
                    {
                        continue;
                    }

                    sources.Add(tile);
                }
            }

            return sources;
        }

        private bool TryCreateRiver(
            TileData source,
            int maximumRiverLength)
        {
            List<TileData> riverPath = new();
            HashSet<TileData> visited = new();

            TileData current = source;

            for (int step = 0; step < maximumRiverLength; step++)
            {
                if (current == null || visited.Contains(current))
                {
                    break;
                }

                visited.Add(current);
                riverPath.Add(current);

                if (current.Terrain == TerrainType.Water)
                {
                    // Ensure tiles in the path are marked as having river
                    MarkRiverPath(riverPath);

                    // Record the river path as coordinates for later visualisation
                    List<UnityEngine.Vector2Int> coords = new();
                    foreach (TileData t in riverPath)
                    {
                        coords.Add(new UnityEngine.Vector2Int(t.Column, t.Row));
                    }

                    rivers.Add(coords);

                    return riverPath.Count >= 3;
                }

                TileData prev = current;

                TileData next = FindBestDownhillNeighbor(
                    current,
                    visited
                );

                if (next == null)
                {
                    break;
                }

                // Mark the river edge between prev and next so the river is
                // stored per-edge (on both tiles).
                MarkEdgeBetween(prev, next);

                current = next;
            }

            // Do not keep short rivers that never reach water.
            return false;
        }

        private void MarkEdgeBetween(TileData a, TileData b)
        {
            if (a == null || b == null) return;

            Vector2Int[] directions =
                a.Row % 2 == 0 ? EvenRowDirections : OddRowDirections;

            int dirIndex = -1;

            for (int i = 0; i < directions.Length; i++)
            {
                if (directions[i].x == b.Column - a.Column &&
                    directions[i].y == b.Row - a.Row)
                {
                    dirIndex = i;
                    break;
                }
            }

            if (dirIndex == -1)
                return;

            a.RiverEdges[dirIndex] = true;
            int opposite = (dirIndex + 3) % 6;
            b.RiverEdges[opposite] = true;

            a.HasRiver = true;
            b.HasRiver = true;
        }

        private TileData FindBestDownhillNeighbor(
            TileData current,
            HashSet<TileData> visited)
        {
            List<TileData> neighbors = GetNeighbors(current);

            TileData bestNeighbor = null;
            float bestScore = float.MaxValue;

            foreach (TileData neighbor in neighbors)
            {
                if (neighbor == null || visited.Contains(neighbor))
                {
                    continue;
                }

                if (neighbor.Terrain == TerrainType.Water)
                {
                    return neighbor;
                }

                float score = neighbor.Elevation;

                // Small randomness stops every river from looking identical.
                score += (float)random.NextDouble() * 8f;

                // Existing rivers are attractive so rivers can merge.
                if (neighbor.HasRiver)
                {
                    score -= 15f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNeighbor = neighbor;
                }
            }

            if (bestNeighbor == null)
            {
                return null;
            }

            // Prevent rivers from climbing too far uphill.
            if (bestNeighbor.Elevation > current.Elevation + 8)
            {
                return null;
            }

            return bestNeighbor;
        }

        private List<TileData> GetNeighbors(TileData tile)
        {
            List<TileData> neighbors = new();

            Vector2Int[] directions =
                tile.Row % 2 == 0
                    ? EvenRowDirections
                    : OddRowDirections;

            foreach (Vector2Int direction in directions)
            {
                int column = tile.Column + direction.x;
                int row = tile.Row + direction.y;

                if (column < 0 ||
                    column >= world.Width ||
                    row < 0 ||
                    row >= world.Height)
                {
                    continue;
                }

                neighbors.Add(world.Tiles[column, row]);
            }

            return neighbors;
        }

        private static void MarkRiverPath(
            List<TileData> riverPath)
        {
            foreach (TileData tile in riverPath)
            {
                if (tile.Terrain != TerrainType.Water)
                {
                    tile.HasRiver = true;
                }
            }
        }

        private void ClearExistingRivers()
        {
            for (int row = 0; row < world.Height; row++)
            {
                for (int column = 0; column < world.Width; column++)
                {
                    TileData t = world.Tiles[column, row];
                    t.HasRiver = false;
                    if (t.RiverEdges == null || t.RiverEdges.Length != 6)
                        t.RiverEdges = new bool[6];
                    else
                        for (int i = 0; i < 6; i++) t.RiverEdges[i] = false;
                }
            }

            rivers.Clear();
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);

                (list[i], list[swapIndex]) =
                    (list[swapIndex], list[i]);
            }
        }
    }
}