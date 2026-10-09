// MathUtils.cs — Math helper functions
// Ported from src/utils/math.ts

using UnityEngine;

namespace RoyalMatch.Utils
{
    public static class MathUtils
    {
        public static float Clamp(float value, float min, float max)
        {
            return Mathf.Max(min, Mathf.Min(max, value));
        }

        public static float Lerp(float start, float end, float t)
        {
            return start + (end - start) * t;
        }

        public static float EuclideanMod(float n, float d)
        {
            return ((n % d) + d) % d;
        }

        public static float DegToRad(float degrees)
        {
            return degrees * Mathf.PI / 180f;
        }

        public static float RadToDeg(float radians)
        {
            return radians * 180f / Mathf.PI;
        }

        public static float DistanceSq(Vector2 a, Vector2 b)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            return dx * dx + dy * dy;
        }

        public static int ManhattanDistance(RoyalMatch.Types.Point a, RoyalMatch.Types.Point b)
        {
            return Mathf.Abs(a.Row - b.Row) + Mathf.Abs(a.Col - b.Col);
        }
    }
}
