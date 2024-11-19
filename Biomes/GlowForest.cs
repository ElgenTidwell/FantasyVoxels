using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using System;

namespace FantasyVoxels.Biomes
{
    public class GlowForest : BiomeProvider
    {
        public override string Name => "Glow Forest";

        public override void Setup()
        {
            ContinentalnessCurve.Keys.Add(new CurveKey(1, 150));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.3f, 90));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.28f, 87));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.04f, 44));
            ContinentalnessCurve.Keys.Add(new CurveKey(0.0f, 40));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.1f, 20));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.2f, 0));
            ContinentalnessCurve.Keys.Add(new CurveKey(-0.5f, -25));
            ContinentalnessCurve.Keys.Add(new CurveKey(-1.0f, 60));

            ErosionCurve.Keys.Add(new CurveKey(-1,3));
            ErosionCurve.Keys.Add(new CurveKey(-0.8f,-1));
            ErosionCurve.Keys.Add(new CurveKey(0,1));
            ErosionCurve.Keys.Add(new CurveKey(0.5f,0));
            ErosionCurve.Keys.Add(new CurveKey(1,0.1f));

            PVCurve.Keys.Add(new CurveKey(-1, -10));
            PVCurve.Keys.Add(new CurveKey(0,  0));
            PVCurve.Keys.Add(new CurveKey(1,  20));

            DensityCurve.Keys.Add(new CurveKey(0, 0.3f));
            DensityCurve.Keys.Add(new CurveKey(50, 0));
        }
        public override byte GetVoxel(float x, float y, float z, int terrainHeight, bool p3d)
        {
            // Main terrain voxel assignment with 3D noise layers
            byte voxel = (byte)(y <= terrainHeight ? y < 15 ? 4 : 2 : 0);

            if (p3d) voxel = 2;

            // Water voxel assignment for regions below sea level
            if (y < 12 && y >= terrainHeight)
                voxel = 3;

            /*
            float frequency = 0.012f;
            float yscale = (GetOctaveNoise3D(x, y, z, 0.004f, 1, 0.9f, 1.24f, -2) + 1) * 0.8f;

            float n1 = IcariaNoise.BrokenGradientNoise3D(x * frequency, y * frequency * yscale, z * frequency, MGame.Instance.seed - 10);
            float n2 = IcariaNoise.GradientNoise3D(x * frequency * 1.5f, y * frequency * yscale * 2, z * frequency * 1.5f, MGame.Instance.seed - 25);
            float n3 = IcariaNoise.GradientNoise3D(x * frequency, y * frequency * 2, z * frequency, MGame.Instance.seed + 15);

            float mix = (IcariaNoise.GradientNoise3D(x * frequency * 0.2f, y * frequency * 0.2f, z * frequency * 0.2f, MGame.Instance.seed)) * 2;

            float density = float.Abs(IcariaNoise.GradientNoise(x * frequency * 0.1f, z * frequency * 0.1f, MGame.Instance.seed - 15)) * 0.59f + 0.01f;

            if (((n1 * mix - n2) + 0.2f) / (MathF.Max(y - terrainHeight, 1) * 0.8f) > density && y > terrainHeight)
                voxel = 2;

            // Cave generation parameters
            float caveFrequency = 0.02f;  // Adjusts the size and frequency of caves (lower values = larger caves)
            float caveThreshold = 0.02f;  // Threshold for determining if a voxel is part of a cave

            // Function to determine if a voxel is part of a cave (returns true for caves, false for solid ground)
            bool IsCave(float x, float y, float z)
            {
                float caveChance = IcariaNoise.GradientNoise3D(
                    x * caveFrequency * 0.001f,
                    y * caveFrequency * 0.01f,
                    z * caveFrequency * 0.001f,
                    MGame.Instance.seed - 60);
                if (caveChance <= 0) return false;

                // Apply 3D Perlin noise at the given (x, y, z) position
                float caveNoise = IcariaNoise.GradientNoise3D(
                    x * caveFrequency,
                    y * caveFrequency,
                    z * caveFrequency,
                    MGame.Instance.seed + 10);

                // Return true if the noise is within the cave threshold range (indicating air/cave space)
                return MathF.Abs(caveNoise) < caveThreshold - caveChance * 0.1f;
            }

            //if (IsCave(x,y,z) && voxel != 3)
            //    voxel = 0;

            if (y < terrainHeight && y < terrainHeight - 3 - MathF.Abs(IcariaNoise.GradientNoise(x * 0.9f, z * 0.9f)) * 2 && voxel != 0)
                voxel = 9;

            */

            return voxel;
        }

        public override byte RequestFolliage(float samplex, float sampley, float samplez, Random tRandom)
        {
            float r = IcariaNoise.GradientNoise(samplex * 0.01f, samplez * 0.01f, MGame.Instance.seed - 10);
            byte voxel = 0;

            if ((int)(r * 35) == tRandom.Next(-35, 35))
            {
                VoxelStructurePlacer.Place((int)samplex, (int)sampley + 1, (int)samplez, new Tree());
            }
            if ((int)(r * 50) == tRandom.Next(-150, 150))
            {
                voxel = 12;
            }
            if ((int)(r * 200) == tRandom.Next(-150, 150))
            {
                voxel = 15;
            }
            return voxel;
        }
    }
}
