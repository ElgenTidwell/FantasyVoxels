using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Biomes
{
    public static class BiomeTracker
    {
        public static BiomeProvider[] biomes;

        const int HumidityLevels = 3;
        const int TemperatureLevels = 3;
        public static int[,] biomeIndexes = new int[TemperatureLevels,HumidityLevels];

        static BiomeTracker()
        {
            biomes = [new GlowForest()];
        }
        /// <summary>
        /// Fetches a biome.
        /// </summary>
        /// <param name="humidity">humidity in range 0 - 1</param>
        /// <param name="temperature">termperature in range 0 - 1</param>
        public static BiomeProvider GetBiome(float humidity, float temperature)
        {
            int H = (int)(humidity*(HumidityLevels-1));
            int T = (int)(humidity*(TemperatureLevels-1));

            return biomes[biomeIndexes[T, H]];
        }
    }
}
