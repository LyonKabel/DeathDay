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

        // River presence on each hex edge. Index 0..5 following the
        // neighbor direction arrays used by RiverGenerator.
        public bool[] RiverEdges = new bool[6];

        public bool HasRiver;

        public bool Explored;

        public bool Visible;
    }
}