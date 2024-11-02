using Icaria.Engine.Procedural;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels
{
    public static class VoxelStructurePlacer
    {
        static ConcurrentDictionary<(int x, int y, int z), ConcurrentQueue<(int x, int y, int z, int vox)>> queudChunks = new ConcurrentDictionary<(int x, int y, int z), ConcurrentQueue<(int x, int y, int z, int vox)>>();
        static Object lockQ = new object();
        
        public static void Place(int worldX, int worldY, int worldZ, VoxelStructure structure)
        {
            structure.Place(worldX, worldY, worldZ);
        }
        public static void Enqueue((int x, int y, int z) chunkPos,(int x, int y, int z, int voxel) vox)
        {
            if (queudChunks.ContainsKey(chunkPos))
            {
                queudChunks[chunkPos].Enqueue(vox);
                if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos(chunkPos),out Chunk chunk))
                {
                    chunk.queueModified = true;
                }
            }
            else if (!queudChunks.ContainsKey(chunkPos))
            {
                queudChunks.TryAdd(chunkPos, new ConcurrentQueue<(int x, int y, int z, int vox)>());
                queudChunks[chunkPos].Enqueue(vox);
                if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos(chunkPos), out Chunk chunk))
                {
                    chunk.queueModified = true;
                }
            }
        }
        public static ConcurrentQueue<(int x, int y, int z, int vox)> GetQueue((int x, int y, int z) chunkPos)
        {
            if (queudChunks.TryGetValue(chunkPos, out var queue))
            {
                return queue;
            }
            return null;
        }
    }
    public abstract class VoxelStructure
    {
        public abstract void Place(int worldX, int worldY, int worldZ);

        protected void SetVoxel(int x, int y, int z, int voxel)
        {
            int cx = (int)MathF.Floor((float)x / Chunk.Size);
            int cy = (int)MathF.Floor((float)y / Chunk.Size);
            int cz = (int)MathF.Floor((float)z / Chunk.Size);


            int px = ((x) - cx * Chunk.Size);
            int py = ((y) - cy * Chunk.Size);
            int pz = ((z) - cz * Chunk.Size);

            VoxelStructurePlacer.Enqueue((cx,cy,cz),(px,py,pz,voxel));
        }
    }

    public class Tree : VoxelStructure
    {
        const int BARK=5, LOG=6, LEAVES = 8;

        public override void Place(int worldX, int worldY, int worldZ)
        {
            int radius = 2;
            var rand = MGame.Instance.worldRandom;
            int treeHeight = 24 + rand.Next(-2, 64);

            for (int x = -12; x < 12; x++)
            {
                for (int z = -12; z < 12; z++)
                {
                    for (int y = -12; y < 12; y++)
                    {
                        float d = new Vector3(x, y, z).Length();

                        if (IcariaNoise.GradientNoise3D((x + worldX)*0.5f,(y + worldY + treeHeight) * 0.5f, (z + worldZ) * 0.5f)*10 / d < 0.1f) continue;

                        SetVoxel(x + worldX, y+treeHeight+worldY, z + worldZ, LEAVES);
                    }
                }
            }
            for (int y = worldY-4; y < worldY + treeHeight; y++)
            {
                radius = (int)MathF.Max(6-MathF.Pow((y + worldY - 6) / (float)(treeHeight/2 + worldY),2)*2,3);

                for (int x = worldX- radius; x < worldX + radius; x++)
                {
                    for (int z = worldZ- radius; z < worldZ+ radius; z++)
                    {
                        int d = (int)(MathF.Abs(x - worldX+0.5f)+MathF.Abs(z - worldZ+0.5f));

                        if (d > radius) continue;

                        SetVoxel(x, y, z, BARK);
                    }
                }
            }
        }
    }
    public class ShortGrass : VoxelStructure
    {
        const int GRASS = 7;

        public override void Place(int worldX, int worldY, int worldZ)
        {
            int height = 2 + MGame.Instance.worldRandom.Next(-1, 2);

            for (int y = worldY; y < worldY+height; y++)
            {
                SetVoxel(worldX,y,worldZ,GRASS);
            }
        }
    }
}
