using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using System;

namespace FantasyVoxels.Biomes
{
    public class BigForest : BiomeProvider
    {
        public override int GrassColor => 1;
        public override string Name => "Big Forest";
        private static TerrainLode[] lodes =
        [
            new TerrainLode(19,-50,60, 0.35f),
            new TerrainLode(21,-180,30, 0.3f)
        ];
        public override TerrainLode[] Lodes => lodes;

        public override byte GetVoxel(float x, float y, float z, int terrainHeight, bool p3d)
        {
            if (p3d) return (byte)(y < 15 ? 4 : 2);

            // Main terrain voxel assignment with 3D noise layers
            byte voxel = (byte)(y <= terrainHeight ? y < 15 && terrainHeight < 15 ? 4 : 2 : 0);

            // Water voxel assignment for regions below sea level
            if (y <= 13 && y >= terrainHeight && voxel == 0)
                voxel = 3;

            return voxel;
        }

        public override byte RequestFolliage(float samplex, float sampley, float samplez, Random tRandom, int vox)
        {
            if (vox != 1) return 0;

            float r = IcariaNoise.GradientNoise(samplex * 0.01f, samplez * 0.01f, MGame.Instance.seed - 10);
            byte voxel = 0;

            if ((int)(r * 35) == tRandom.Next(-35, 35) && IcariaNoise.CellularNoise(samplex*0.1f,sampley * 0.1f, MGame.Instance.seed).r >= 0.25f)
            {
                VoxelStructurePlacer.Place((int)samplex, (int)sampley + 1, (int)samplez, new BigTree());
            }
            if ((int)(r * 600) == tRandom.Next(-250, 250) && IcariaNoise.CellularNoise(samplex * 0.1f, sampley * 0.1f, MGame.Instance.seed).r >= 0.35f)
            {
                VoxelStructurePlacer.Place((int)samplex, (int)sampley + 1, (int)samplez, new HugeBigTree());
            }
            if ((int)(r * 50) == tRandom.Next(-150, 150))
            {
                voxel = 12;
            }
            if ((int)(r * 40) == tRandom.Next(0,80))
            {
                voxel = 18;
            }

            return voxel;
        }

        public override byte GetSurfaceVoxel(float samplex, float sampley, float samplez)
        {
            return 2;
        }
    }
}
