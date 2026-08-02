using System.Collections.Generic;
using UnityEngine;

namespace HexTactics.Grid
{
    public class HexGridManager : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField, Min(1)] private int columns = 9;
        [SerializeField, Min(1)] private int rows = 7;

        [Header("Hex Settings")]
        [SerializeField, Min(0.1f)] private float hexRadius = 1f;
        [SerializeField, Min(0f)] private float tileSpacing = 0.05f;
        [SerializeField] private Material tileMaterial;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = true;

        private readonly Dictionary<Vector2Int, HexTile> tiles = new();

        public IReadOnlyDictionary<Vector2Int, HexTile> Tiles => tiles;

        public int Columns => columns;
        public int Rows => rows;

        private void Awake()
        {
            if (generateOnStart)
            {
                GenerateGrid();
            }
        }

        //private void Start()
        //{
            
        //}

        [ContextMenu("Generate Grid")]
        public void GenerateGrid()
        {
            ClearGrid();

            float adjustedRadius = hexRadius - tileSpacing;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    CreateTile(column, row, adjustedRadius);
                }
            }

            ConnectNeighbors();
            CenterGrid();
        }

        [ContextMenu("Clear Grid")]
        public void ClearGrid()
        {
            tiles.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        public bool TryGetTile(int column, int row, out HexTile tile)
        {
            return tiles.TryGetValue(new Vector2Int(column, row), out tile);
        }

        private void CreateTile(int column, int row, float adjustedRadius)
        {
            GameObject tileObject = new($"Hex ({column}, {row})");
            tileObject.transform.SetParent(transform);

            Vector3 tilePosition = CalculateWorldPosition(column, row);
            tileObject.transform.localPosition = tilePosition;

            HexTile tile = tileObject.AddComponent<HexTile>();

            MeshRenderer meshRenderer =
                tileObject.GetComponent<MeshRenderer>();

            if (tileMaterial != null)
            {
                meshRenderer.sharedMaterial = tileMaterial;
            }

            Color tileColor = (column + row) % 2 == 0
                ? new Color(0.3f, 0.45f, 0.3f)
                : new Color(0.35f, 0.5f, 0.35f);

            tile.Initialize(
                column,
                row,
                adjustedRadius,
                tileColor
            );

            tiles.Add(
                new Vector2Int(column, row),
                tile
            );
        }

        private Vector3 CalculateWorldPosition(int column, int row)
        {
            float width = Mathf.Sqrt(3f) * hexRadius;
            float horizontalSpacing = width;
            float verticalSpacing = hexRadius * 1.5f;

            float xOffset = row % 2 == 0 ? 0f : width * 0.5f;

            float x = column * horizontalSpacing + xOffset;
            float z = row * verticalSpacing;

            return new Vector3(x, 0f, z);
        }

        private void ConnectNeighbors()
        {
            foreach (HexTile tile in tiles.Values)
            {
                Vector2Int[] neighborDirections = GetNeighborDirections(tile.Row);

                foreach (Vector2Int direction in neighborDirections)
                {
                    int neighborColumn = tile.Column + direction.x;
                    int neighborRow = tile.Row + direction.y;

                    if (TryGetTile(neighborColumn, neighborRow, out HexTile neighbor))
                    {
                        tile.AddNeighbor(neighbor);
                    }
                }
            }
        }

        private static Vector2Int[] GetNeighborDirections(int row)
        {
            if (row % 2 == 0)
            {
                return new[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, -1),
                    new Vector2Int(0, -1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1)
                };
            }

            return new[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(1, -1),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            };
        }

        private void CenterGrid()
        {
            if (tiles.Count == 0)
            {
                return;
            }

            Bounds bounds = new(transform.GetChild(0).localPosition, Vector3.zero);

            foreach (Transform tileTransform in transform)
            {
                bounds.Encapsulate(tileTransform.localPosition);
            }

            Vector3 offset = new(
                -bounds.center.x,
                0f,
                -bounds.center.z
            );

            foreach (Transform tileTransform in transform)
            {
                tileTransform.localPosition += offset;
            }
        }
    }
}