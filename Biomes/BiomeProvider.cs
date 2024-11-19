using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Biomes
{
    public abstract class BiomeProvider:IComparable<BiomeProvider>
    {
        protected float GetOctaveNoise2D(float x, float z, float frequency, int octaveCount, float persistence, float lacunarity, int seedOffset = 0)
        {
            float totalNoise = 0;
            float amplitude = 1;
            float maxAmplitude = 0; // Used to normalize the result
            for (int i = 0; i < octaveCount; i++)
            {
                totalNoise += IcariaNoise.GradientNoise(x * frequency, z * frequency, MGame.Instance.seed - 10) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= persistence;    // Decrease amplitude for each octave
                frequency *= lacunarity;     // Increase frequency for each octave
            }
            return totalNoise / maxAmplitude;
        }
        protected float GetOctaveNoise3D(float x, float y, float z, float frequency, int octaveCount, float persistence, float lacunarity, int seedOffset = 0)
        {
            float totalNoise = 0;
            float amplitude = 1;
            float maxAmplitude = 0; // Used to normalize the result
            for (int i = 0; i < octaveCount; i++)
            {
                totalNoise += IcariaNoise.GradientNoise3D(x * frequency, y * frequency, z * frequency, MGame.Instance.seed - seedOffset) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= persistence;    // Decrease amplitude for each octave
                frequency *= lacunarity;     // Increase frequency for each octave
            }
            return totalNoise / maxAmplitude;
        }
        public abstract string Name { get; }
        public Curve ContinentalnessCurve = new Curve();
        public Curve ErosionCurve = new Curve();
        public Curve PVCurve = new Curve();
        public Curve DensityCurve = new Curve();
        public abstract void Setup();
        public abstract byte GetVoxel(float x, float y, float z, int terrainHeight, bool p3d);
        public abstract byte RequestFolliage(float samplex, float sampley, float samplez, Random tRandom);

        public int CompareTo(BiomeProvider other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
