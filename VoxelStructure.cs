using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
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
        static ConcurrentDictionary<(int x, int y, int z), ConcurrentQueue<(int x, int y, int z, int vox)>> queuedChunks = new ConcurrentDictionary<(int x, int y, int z), ConcurrentQueue<(int x, int y, int z, int vox)>>();
        static Object lockQ = new object();
        
        public static void Clear()
        {
            queuedChunks.Clear();
        }

        public static void Place(int worldX, int worldY, int worldZ, VoxelStructure structure)
        {
            structure.Place(worldX, worldY, worldZ);
        }
        public static void Enqueue((int x, int y, int z) chunkPos,(int x, int y, int z, int voxel) vox)
        {
            lock(lockQ)
            {
                if (queuedChunks.TryGetValue(chunkPos, out ConcurrentQueue<(int x, int y, int z, int vox)> value))
                {
                    value.Enqueue(vox);
                    if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos(chunkPos), out Chunk chunk))
                    {
                        chunk.queueModified = true;
                    }
                }
                else
                {
                    queuedChunks.TryAdd(chunkPos, new ConcurrentQueue<(int x, int y, int z, int vox)>());
                    queuedChunks[chunkPos].Enqueue(vox);
                    if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos(chunkPos), out Chunk chunk))
                    {
                        chunk.queueModified = true;
                    }
                }
            }
        }
        //public static ConcurrentQueue<(int x, int y, int z, int vox)> GetQueue((int x, int y, int z) chunkPos)
        //{
        //    if (queuedChunks.TryGetValue(chunkPos, out var queue))
        //    {
        //        return queue;
        //    }
        //    return null;
        //}
        public static int GetQueueLength((int x, int y, int z) chunkPos)
        {
            if (queuedChunks.TryGetValue(chunkPos, out var queue))
            {
                return queue.Count;
            }
            return 0;
        }
        public static (int x, int y, int z, int vox) Dequeue((int x, int y, int z) chunkPos)
        {
            lock(lockQ)
            {
                if (queuedChunks.TryGetValue(chunkPos, out var queue) && queue.TryDequeue(out var o))
                {
                    return o;
                }
                return (-1, -1, -1, -1);
            }
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
        const int BARK=5, PLEAVES=16, LEAVES = 8;

        public override void Place(int worldX, int worldY, int worldZ)
        {
            int treeHeight = 4 + (int)((IcariaNoise.GradientNoise(worldX * 0.8f, worldZ * 0.8f)+1) * 4);

            bool ttype = (int)((IcariaNoise.GradientNoise(worldX * 0.3f, worldZ * 0.3f, MGame.Instance.seed - 15)) * 3) % 2 == 0;

            for (int y = worldY + treeHeight - 2; y <= worldY + treeHeight+1; y++)
            {
                int rad = (y > worldY + treeHeight?1:2);

                for (int x = -rad; x <= rad; x++)
                {
                    for (int z = -rad; z <= rad; z++)
                    {
                        int offset = (int)((IcariaNoise.GradientNoise3D(worldX * 0.7f, worldY * 0.7f, worldZ * 0.7f))*2f);

                        if (int.Abs(x) - offset == rad && int.Abs(z) - offset == rad && y <= worldY + treeHeight) continue;

                        SetVoxel(x + worldX, y, z + worldZ, ttype ? LEAVES : PLEAVES);
                    }
                }
            }
            for (int y = worldY; y <= worldY + treeHeight; y++)
            {
                SetVoxel(worldX, y, worldZ, BARK);
            }
        }
    }
    public class BigTree : VoxelStructure
    {
        const int BARK = 5, PLEAVES = 8, LEAVES = 25;

        public override void Place(int worldX, int worldY, int worldZ)
        {
            int radius = 2;
            int treeHeight = 8 + (int)((IcariaNoise.GradientNoise(worldX * 0.8f, worldZ * 0.8f) + 1) * 8);

            bool ttype = (int)((IcariaNoise.GradientNoise(worldX * 0.3f, worldZ * 0.3f, MGame.Instance.seed - 15)) * 3) % 2 == 0;

            for (int y = worldY + 3; y <= worldY + treeHeight; y++)
            {
                if ((y - worldY) % 3 == 0) continue;                                                                                                                                                          

                int rad = (int)(((1 - (((float)y - worldY - 3) / (treeHeight - 3)) + 2) * 1.5f) - int.Min((y - worldY) % 3, 2));

                for (int x = -rad; x <= rad; x++)
                {
                    for (int z = -rad; z <= rad; z++)
                    {
                        if (float.Abs(x) + float.Abs(z) > rad) continue;

                        SetVoxel(x + worldX, y, z + worldZ, ttype ? LEAVES : PLEAVES);
                    }
                }
            }
            for (int y = worldY - 4; y <= worldY + treeHeight; y++)
            {
                radius = (int)MathF.Max(6 - MathF.Pow((y + worldY - 6) / (float)(treeHeight / 2 + worldY), 2) * 2, 3);

                SetVoxel(worldX, y, worldZ, BARK);
            }
            SetVoxel(worldX, worldY + treeHeight+1, worldZ, ttype ? LEAVES : PLEAVES);
        }
    }
    public class HugeBigTree : VoxelStructure
    {
        const int BARK = 5, PLEAVES = 8, LEAVES = 25;

        public override void Place(int worldX, int worldY, int worldZ)
        {
            int radius = 2;
            int treeHeight = 22 + (int)((IcariaNoise.GradientNoise(worldX * 0.8f, worldZ * 0.8f) + 1) * 16);

            bool ttype = (int)((IcariaNoise.GradientNoise(worldX * 0.3f, worldZ * 0.3f, MGame.Instance.seed - 15)) * 3) % 2 == 0;

            for (int y = worldY - 4; y <= worldY + treeHeight-5; y++)
            {
                radius = (int)float.Max((1 - float.Pow(float.Max((y - worldY - 4) / (float)(treeHeight),0), 0.5f))*4, 1);

                if ((y - worldY) % 3 != 0)
                {
                    int rad = (int)(((1 - (((float)y - worldY - 4) / (treeHeight))) * 5.5f))+radius;

                    for (int x = -rad; x <= rad; x++)
                    {
                        for (int z = -rad; z <= rad; z++)
                        {
                            if ((x * x) + (z * z) >= rad*3+ 8 - int.Min((y - worldY) % 3, 3)*4) continue;

                            SetVoxel(x + worldX, y, z + worldZ, ttype ? LEAVES : PLEAVES);
                        }
                    }
                }

                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        if ((x*x) + (z*z) > radius) continue;

                        SetVoxel(worldX+x, y, worldZ+z, BARK);
                    }
                }
            }
            SetVoxel(worldX, worldY + treeHeight - 4, worldZ, ttype ? LEAVES : PLEAVES);
        }
    }
}
