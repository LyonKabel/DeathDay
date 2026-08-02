using HexTactics.World;

namespace HexTactics.World.Data
{
    public class TileData
    {
        public int Column;
        public int Row;

        public TerrainType Terrain;

        public BiomeType Biome;

        public ResourceType Resource;

        public ImprovementType Improvement;

        public int Elevation;

        public bool HasRiver;

        public bool Explored;

        public bool Visible;
    }
}