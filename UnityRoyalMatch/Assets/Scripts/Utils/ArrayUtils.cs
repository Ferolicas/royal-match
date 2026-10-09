// ArrayUtils.cs — Array helper functions
// Ported from src/utils/array.ts

using System;
using System.Collections.Generic;

namespace RoyalMatch.Utils
{
    public static class ArrayUtils
    {
        public static T[] Create2DArray<T>(int rows, int cols, T fill)
        {
            T[] result = new T[rows * cols];
            for (int i = 0; i < result.Length; i++)
                result[i] = fill;
            return result;
        }

        public static T[][] CreateJagged2D<T>(int rows, int cols, T fill)
        {
            T[][] result = new T[rows][];
            for (int r = 0; r < rows; r++)
            {
                result[r] = new T[cols];
                for (int c = 0; c < cols; c++)
                    result[r][c] = fill;
            }
            return result;
        }

        public static void Shuffle<T>(T[] array, System.Random rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }
    }
}
