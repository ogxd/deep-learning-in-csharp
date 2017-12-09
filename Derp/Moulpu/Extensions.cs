using System;

namespace Ogee.AI.Derp {

    public static class Extensions {

        const int RANDOM_SEED = 16541;
        public static Random Rnd = new Random(RANDOM_SEED);

        public static float GetRandomFloat(float min, float max) {
            return (float)(Rnd.NextDouble() * (max - min) + min);
        }

        public static void Multiply(this double[] array, double factor) {
            for (int j = 0; j < array.Length; j++) {
                array[j] *= factor;
            }
        }

        public static void Add(this int[] array, int count) {
            for (int j = 0; j < array.Length; j++) {
                array[j] += count;
            }
        }

        public static double[] GetRandomArray(this Random random, int length, double minimum, double maximum) {
            double[] output = new double[length];
            for (int i = 0; i < output.Length; i++)
                output[i] = random.NextDouble() * (maximum - minimum) + minimum;
            return output;
        }
    }
}
