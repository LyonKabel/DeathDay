using UnityEngine;

namespace HexTactics.World.Generation
{
    public class ContinentGenerator
    {
        private readonly float scale;
        private readonly float seaLevel;
        private readonly float falloffStrength;
        private readonly Vector2 offset;

        public ContinentGenerator(
            float scale,
            float seaLevel,
            float falloffStrength,
            int seed)
        {
            this.scale = scale;
            this.seaLevel = seaLevel;
            this.falloffStrength = falloffStrength;

            Random.InitState(seed);

            offset = new Vector2(
                Random.Range(-10000f, 10000f),
                Random.Range(-10000f, 10000f));
        }

        public bool IsLand(
            int x,
            int y,
            int width,
            int height)
        {
            float nx = x / (float)width;
            float ny = y / (float)height;

            float noise = Mathf.PerlinNoise(
                nx * scale + offset.x,
                ny * scale + offset.y);

            float dx = nx - .5f;
            float dy = ny - .5f;

            float distance =
                Mathf.Sqrt(dx * dx + dy * dy);

            float falloff =
                Mathf.Pow(distance * 1.4142f,
                          falloffStrength);

            float value = noise - falloff;

            return value > seaLevel;
        }
    }
}