using System.Collections.Generic;
using HexTactics.Grid;

namespace HexTactics.Pathfinding
{
    public static class HexPathfinder
    {
        public static Dictionary<HexTile, int> GetReachableTiles(
            HexTile startingTile,
            int movementPoints)
        {
            Dictionary<HexTile, int> movementCosts = new();
            List<HexTile> openTiles = new();

            movementCosts[startingTile] = 0;
            openTiles.Add(startingTile);

            while (openTiles.Count > 0)
            {
                HexTile currentTile =
                    GetLowestCostTile(openTiles, movementCosts);

                openTiles.Remove(currentTile);

                foreach (HexTile neighbor in currentTile.Neighbors)
                {
                    if (!CanEnterTile(neighbor, startingTile))
                    {
                        continue;
                    }

                    int newMovementCost =
                        movementCosts[currentTile] +
                        neighbor.MovementCost;

                    if (newMovementCost > movementPoints)
                    {
                        continue;
                    }

                    bool hasExistingCost =
                        movementCosts.TryGetValue(
                            neighbor,
                            out int existingCost
                        );

                    if (!hasExistingCost ||
                        newMovementCost < existingCost)
                    {
                        movementCosts[neighbor] = newMovementCost;

                        if (!openTiles.Contains(neighbor))
                        {
                            openTiles.Add(neighbor);
                        }
                    }
                }
            }

            return movementCosts;
        }

        public static List<HexTile> FindPath(
            HexTile startingTile,
            HexTile destinationTile)
        {
            if (startingTile == null || destinationTile == null)
            {
                return null;
            }

            Dictionary<HexTile, int> movementCosts = new();
            Dictionary<HexTile, HexTile> cameFrom = new();
            List<HexTile> openTiles = new();

            movementCosts[startingTile] = 0;
            openTiles.Add(startingTile);

            while (openTiles.Count > 0)
            {
                HexTile currentTile =
                    GetLowestCostTile(openTiles, movementCosts);

                openTiles.Remove(currentTile);

                if (currentTile == destinationTile)
                {
                    return BuildPath(
                        cameFrom,
                        startingTile,
                        destinationTile
                    );
                }

                foreach (HexTile neighbor in currentTile.Neighbors)
                {
                    if (!CanEnterTile(neighbor, startingTile))
                    {
                        continue;
                    }

                    int newMovementCost =
                        movementCosts[currentTile] +
                        neighbor.MovementCost;

                    bool hasExistingCost =
                        movementCosts.TryGetValue(
                            neighbor,
                            out int existingCost
                        );

                    if (!hasExistingCost ||
                        newMovementCost < existingCost)
                    {
                        movementCosts[neighbor] = newMovementCost;
                        cameFrom[neighbor] = currentTile;

                        if (!openTiles.Contains(neighbor))
                        {
                            openTiles.Add(neighbor);
                        }
                    }
                }
            }

            return null;
        }

        public static int GetPathCost(
            List<HexTile> path)
        {
            if (path == null || path.Count <= 1)
            {
                return 0;
            }

            int totalCost = 0;

            for (int i = 1; i < path.Count; i++)
            {
                totalCost += path[i].MovementCost;
            }

            return totalCost;
        }

        private static List<HexTile> BuildPath(
            Dictionary<HexTile, HexTile> cameFrom,
            HexTile startingTile,
            HexTile destinationTile)
        {
            List<HexTile> path = new();
            HexTile currentTile = destinationTile;

            path.Add(currentTile);

            while (currentTile != startingTile)
            {
                if (!cameFrom.TryGetValue(
                        currentTile,
                        out HexTile previousTile))
                {
                    return null;
                }

                currentTile = previousTile;
                path.Add(currentTile);
            }

            path.Reverse();
            return path;
        }

        private static bool CanEnterTile(
            HexTile tile,
            HexTile startingTile)
        {
            if (tile == null)
            {
                return false;
            }

            if (!tile.IsWalkable)
            {
                return false;
            }

            if (tile.IsOccupied && tile != startingTile)
            {
                return false;
            }

            return true;
        }

        private static HexTile GetLowestCostTile(
            List<HexTile> openTiles,
            Dictionary<HexTile, int> movementCosts)
        {
            HexTile lowestCostTile = openTiles[0];
            int lowestCost = movementCosts[lowestCostTile];

            for (int i = 1; i < openTiles.Count; i++)
            {
                HexTile tile = openTiles[i];
                int tileCost = movementCosts[tile];

                if (tileCost < lowestCost)
                {
                    lowestCostTile = tile;
                    lowestCost = tileCost;
                }
            }

            return lowestCostTile;
        }
    }
}