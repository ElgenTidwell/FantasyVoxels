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

        static BiomeTracker()
        {
            biomes = ReflectiveEnumerator.GetEnumerableOfType<BiomeProvider>().ToArray();

            foreach (var biome in biomes) biome.Setup();
        }
    }
}
