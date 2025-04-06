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

        public const int HumidityLevels = 3;
        public const int TemperatureLevels = 3;
        public static int[,] biomeIndexes = new int[TemperatureLevels, HumidityLevels]
        {
            //Temp ->
            { 2, 2, 0 }, // ^
            { 2, 0, 0 }, // |
            { 0, 1, 1 }, // Humid
        };

        static BiomeTracker()
        {
            biomes = [new GlowForest(),new Desert(), new BigForest()];
        }
        /// <summary>
        /// Fetches a biome.
        /// </summary>
        /// <param name="humidity">humidity in range 0 - 1</param>
        /// <param name="temperature">termperature in range 0 - 1</param>
        public static BiomeProvider GetBiome(float humidity, float temperature)
        {
            int H = (int)((1- humidity) *(HumidityLevels));
            int T = (int)(temperature*(TemperatureLevels));

            return biomes[biomeIndexes[T, H]];
        }
    }
}
