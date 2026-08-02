using UnityEngine;

namespace HexTactics.Grid
{
    public static class HexGridUtility
    {
        public static int GetDistance(HexTile first, HexTile second)
        {
            if (first == null || second == null)
            {
                return int.MaxValue;
            }

            Vector3Int firstCube = OffsetToCube(
                first.Column,
                first.Row
            );

            Vector3Int secondCube = OffsetToCube(
                second.Column,
                second.Row
            );

            return Mathf.Max(
                Mathf.Abs(firstCube.x - secondCube.x),
                Mathf.Abs(firstCube.y - secondCube.y),
                Mathf.Abs(firstCube.z - secondCube.z)
            );
        }

        private static Vector3Int OffsetToCube(int column, int row)
        {
            int x = column - ((row - (row & 1)) / 2);
            int z = row;
            int y = -x - z;

            return new Vector3Int(x, y, z);
        }
    }
}