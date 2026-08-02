using System.Collections.Generic;
using UnityEngine;
using HexTactics.World;

namespace HexTactics.Grid
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class HexTile : MonoBehaviour
    {
        [Header("Coordinates")]
        [SerializeField] private int column;
        [SerializeField] private int row;

        [Header("Tile State")]
        [SerializeField] private bool isWalkable = true;
        [SerializeField, Min(1)] private int movementCost = 1;

        [Header("World")]
        [SerializeField] private TerrainType terrain = TerrainType.Grass;
        [SerializeField] private BiomeType biome = BiomeType.Plains;
        [SerializeField] private ResourceType resource = ResourceType.None;
        [SerializeField] private ImprovementType improvement = ImprovementType.None;
        [SerializeField] private int elevation = 0;

        private readonly List<HexTile> neighbors = new();

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;

        private Color normalColor = Color.gray;
        private Color currentColor = Color.gray;

        private static readonly int BaseColorID =
            Shader.PropertyToID("_BaseColor");

        public int Column => column;
        public int Row => row;

        public bool IsWalkable
        {
            get => isWalkable;
            set => isWalkable = value;
        }

        public int MovementCost
        {
            get => movementCost;
            set => movementCost = Mathf.Max(1, value);
        }

        public IReadOnlyList<HexTile> Neighbors => neighbors;

        public bool IsOccupied { get; private set; }
        public GameObject OccupyingUnit { get; private set; }

        public TerrainType Terrain
        {
            get => terrain;
            set
            {
                terrain = value;

                switch (terrain)
                {
                    case TerrainType.Grass:
                        MovementCost = 1;
                        IsWalkable = true;
                        break;

                    case TerrainType.Forest:
                        MovementCost = 2;
                        IsWalkable = true;
                        break;

                    case TerrainType.Hill:
                        MovementCost = 2;
                        IsWalkable = true;
                        break;

                    case TerrainType.Desert:
                        MovementCost = 2;
                        IsWalkable = true;
                        break;

                    case TerrainType.Swamp:
                        MovementCost = 3;
                        IsWalkable = true;
                        break;

                    case TerrainType.Snow:
                        MovementCost = 2;
                        IsWalkable = true;
                        break;

                    case TerrainType.Mountain:
                        MovementCost = 99;
                        IsWalkable = false;
                        break;

                    case TerrainType.Water:
                        MovementCost = 99;
                        IsWalkable = false;
                        break;
                }

                RefreshTerrainVisual();
            }
        }

        public BiomeType Biome
        {
            get => biome;
            set => biome = value;
        }

        public ResourceType Resource
        {
            get => resource;
            set => resource = value;
        }

        public ImprovementType Improvement
        {
            get => improvement;
            set => improvement = value;
        }

        public int Elevation
        {
            get => elevation;
            set => elevation = value;
        }

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propertyBlock = new MaterialPropertyBlock();
        }

        public void Initialize(
            int newColumn,
            int newRow,
            float radius,
            Color tileColor)
        {
            column = newColumn;
            row = newRow;

            normalColor = tileColor;
            currentColor = normalColor;

            name = $"Hex ({column}, {row})";

            CreateHexMesh(radius);
            SetColor(normalColor);
        }

        public void AddNeighbor(HexTile neighbor)
        {
            if (neighbor == null || neighbor == this)
                return;

            if (!neighbors.Contains(neighbor))
                neighbors.Add(neighbor);
        }

        public void SetOccupyingUnit(GameObject unit)
        {
            OccupyingUnit = unit;
            IsOccupied = unit != null;
        }

        public void SetHover(bool isHovered)
        {
            if (isHovered)
                SetColor(Color.yellow);
            else
                SetColor(currentColor);
        }

        public void SetSelected(bool isSelected)
        {
            currentColor = isSelected ? Color.cyan : normalColor;
            SetColor(currentColor);
        }

        public void SetMovementHighlight(bool isHighlighted)
        {
            currentColor = isHighlighted ? Color.blue : normalColor;
            SetColor(currentColor);
        }

        public void SetAttackHighlight(bool isHighlighted)
        {
            currentColor = isHighlighted ? Color.red : normalColor;
            SetColor(currentColor);
        }

        public void ResetVisual()
        {
            currentColor = normalColor;
            SetColor(normalColor);
        }

        public void RefreshTerrainVisual()
        {
            switch (terrain)
            {
                case TerrainType.Grass:
                    normalColor = new Color(.32f, .58f, .29f);
                    break;

                case TerrainType.Forest:
                    normalColor = new Color(.16f, .40f, .18f);
                    break;

                case TerrainType.Hill:
                    normalColor = new Color(.63f, .52f, .27f);
                    break;

                case TerrainType.Mountain:
                    normalColor = Color.gray;
                    break;

                case TerrainType.Water:
                    normalColor = Color.blue;
                    break;

                case TerrainType.Desert:
                    normalColor = new Color(.93f, .85f, .42f);
                    break;

                case TerrainType.Swamp:
                    normalColor = new Color(.28f, .33f, .18f);
                    break;

                case TerrainType.Snow:
                    normalColor = Color.white;
                    break;
            }

            ResetVisual();
        }

        private void SetColor(Color color)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorID, color);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void CreateHexMesh(float radius)
        {
            Mesh mesh = new()
            {
                name = $"Hex Mesh ({column}, {row})"
            };

            Vector3[] vertices = new Vector3[7];
            int[] triangles = new int[18];

            vertices[0] = Vector3.zero;

            for (int i = 0; i < 6; i++)
            {
                float angle = (30f + i * 60f) * Mathf.Deg2Rad;

                vertices[i + 1] = new Vector3(
                    radius * Mathf.Cos(angle),
                    0f,
                    radius * Mathf.Sin(angle)
                );
            }

            for (int i = 0; i < 6; i++)
            {
                int index = i * 3;

                triangles[index] = 0;
                triangles[index + 1] = ((i + 1) % 6) + 1;
                triangles[index + 2] = i + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}