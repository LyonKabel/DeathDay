using System;
using HexTactics.World.Generation;

namespace HexTactics.World.Settings
{
    [Serializable]
    public class WorldSettings
    {
        public WorldType WorldType = WorldType.Continents;

        // Map size settings
        public int Width = 40;
        public int Height = 40;

        // Seed settings
        public int Seed = 0;
        public bool RandomizeSeed;

        // Continent settings
        public float ContinentScale = 6f;
        public float SeaLevel = 0.12f;
        public float FalloffStrength = 2.2f;

        // Elevation settings
        public float ElevationScale = 5f;
        public float HillThreshold = 0.57f;
        public float MountainThreshold = 0.72f;

        // Climate settings
        public float Temperature = 0.5f;
        public float Rainfall = 0.5f;

        // River settings
        public int RiverCount = 8;
        public int MinimumRiverSourceElevation = 65;
        public int MaximumRiverLength = 80;
    }
}