using System;
using UnityEngine;
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

        public float SeaLevel = 0.40f;

        public float Temperature = 0.5f;

        public float Rainfall = 0.5f;
    }
}