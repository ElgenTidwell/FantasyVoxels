using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using System;

namespace FantasyVoxels.Biomes
{
    public class GlowForest : BiomeProvider
    {
        public override string Name => "Glow Forest";

        public override byte GetVoxel(float x, float y, float z, int terrainHeight, bool p3d)
        {
            // Main terrain voxel assignment with 3D noise layers
            byte voxel = (byte)(y <= terrainHeight ? y < 15 ? 4 : 2 : 0);

            if (p3d) voxel = 2;

            // Water voxel assignment for regions below sea level
            if (y < 12 && y >= terrainHeight)
                voxel = 3;

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

        public override byte GetSurfaceVoxel(float samplex, float sampley, float samplez)
        {
            return 2;
        }
    }
}
