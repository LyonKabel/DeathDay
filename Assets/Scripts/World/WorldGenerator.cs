using UnityEngine;
using HexTactics.Grid;
using HexTactics.World.Generation;

namespace HexTactics.World
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private HexGridManager gridManager;

        [Header("World Settings")]
        [SerializeField]
        private WorldType worldType = WorldType.Continents;

        [SerializeField]
        private int seed = 0;

        [SerializeField]
        private float scale = 6f;

        [SerializeField]
        private float seaLevel = 0.12f;

        [SerializeField]
        private float falloffStrength = 2.2f;

        private void Start()
        {
            GenerateWorld();
        }

        [ContextMenu("Generate World")]
        public void GenerateWorld()
        {
            if (gridManager == null)
            {
                Debug.LogError("Grid Manager missing.");
                return;
            }

            switch (worldType)
            {
                case WorldType.Continents:
                    GenerateContinents();
                    break;

                case WorldType.Pangaea:
                    Debug.Log("Pangaea generation not implemented yet.");
                    break;

                case WorldType.Archipelago:
                    Debug.Log("Archipelago generation not implemented yet.");
                    break;

                case WorldType.InlandSea:
                    Debug.Log("Inland Sea generation not implemented yet.");
                    break;

                case WorldType.Fractured:
                    Debug.Log("Fractured generation not implemented yet.");
                    break;
            }
        }

        private void GenerateContinents()
        {
            ContinentGenerator continent = new ContinentGenerator(
                scale,
                seaLevel,
                falloffStrength,
                seed);

            foreach (HexTile tile in gridManager.Tiles.Values)
            {
                bool land = continent.IsLand(
                    tile.Column,
                    tile.Row,
                    gridManager.Columns,
                    gridManager.Rows);

                tile.Terrain = land
                    ? TerrainType.Grass
                    : TerrainType.Water;
            }

            Debug.Log("Continent generated.");
        }
    }
}