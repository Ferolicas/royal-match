// DeterministicRNG.cs — Seeded RNG (LCG algorithm)
// Ported from src/utils/rng.ts

namespace RoyalMatch.Utils
{
    public class DeterministicRNG
    {
        private uint _state;
        private readonly uint _seed;

        public DeterministicRNG(int seed)
        {
            _seed = (uint)seed;
            _state = _seed;
        }

        public float Next()
        {
            _state = (_state * 1664525u + 1013904223u);
            // The float conversion can round the largest uint values to 1.0f.
            // A random draw must stay in [0, 1) for inclusive index ranges.
            return System.Math.Min((float)((double)_state / 4294967296.0), 0.99999994f);
        }

        public int NextInRange(int min, int max)
        {
            return MathfFloorToInt(Next() * (max - min + 1)) + min;
        }

        public float NextFloat(float min = 0f, float max = 1f)
        {
            return Next() * (max - min) + min;
        }

        public void Shuffle<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = NextInRange(0, i);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        public T PickOne<T>(T[] array)
        {
            if (array.Length == 0) return default(T);
            int index = NextInRange(0, array.Length - 1);
            return array[index];
        }

        public int[][] ShuffleWithHistory<T>(T[] array)
        {
            // Returns new shuffled array (doesn't modify original)
            T[] result = new T[array.Length];
            System.Array.Copy(array, result, array.Length);
            Shuffle(result);
            return null; // Not used — this is just a placeholder signature
        }

        public uint GetSeed() => _seed;

        public void Reset() => _state = _seed;

        private int MathfFloorToInt(float f)
        {
            return (int)System.Math.Floor(f);
        }
    }
}
