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
}
