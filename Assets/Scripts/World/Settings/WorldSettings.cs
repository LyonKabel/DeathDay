using System;
using HexTactics.World.Generation;

namespace HexTactics.World.Settings
{
    [Serializable]
    public class WorldSettings
    {
        public WorldType WorldType = WorldType.Continents;

        public int Width = 40;
        public int Height = 40;

        public int Seed = 0;
        public bool RandomizeSeed;

        public float ContinentScale = 6f;
        public float SeaLevel = 0.12f;
        public float FalloffStrength = 2.2f;

        public float ElevationScale = 5f;
        public float HillThreshold = 0.57f;
        public float MountainThreshold = 0.72f;

        public float Temperature = 0.5f;
        public float Rainfall = 0.5f;
    }
}