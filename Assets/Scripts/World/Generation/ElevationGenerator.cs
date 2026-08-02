using UnityEngine;

namespace HexTactics.World.Generation
{
    public class ElevationGenerator
    {
        private readonly float scale;
        private readonly Vector2 offset;

        public ElevationGenerator(float scale, int seed)
        {
            this.scale = Mathf.Max(0.01f, scale);

            System.Random random = new(seed + 91827);

            offset = new Vector2(
                random.Next(-100000, 100000),
                random.Next(-100000, 100000)
            );
        }

        public float GetElevation(
            int column,
            int row,
            int width,
            int height)
        {
            float normalizedX = column / (float)Mathf.Max(1, width - 1);
            float normalizedY = row / (float)Mathf.Max(1, height - 1);

            float largeFeatures = Mathf.PerlinNoise(
                normalizedX * scale + offset.x,
                normalizedY * scale + offset.y
            );

            float smallFeatures = Mathf.PerlinNoise(
                normalizedX * scale * 2.5f + offset.x + 300f,
                normalizedY * scale * 2.5f + offset.y + 300f
            );

            return Mathf.Clamp01(
                largeFeatures * 0.75f +
                smallFeatures * 0.25f
            );
        }
    }
}