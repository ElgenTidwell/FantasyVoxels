using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    public class GrassBlock : Block
    {
        public GrassBlock()
        {
            smoothLightingEnable = true;
        }

        public override void Init()
        {
        }

        protected override bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk)
        {
            if (MGame.Instance.GrabVoxel(new Vector3(posInChunk.x, posInChunk.y + 1, posInChunk.z) + new Vector3(chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z) * Chunk.Size) > 0 &&
                MGame.Instance.GrabVoxel(new Vector3(posInChunk.x, posInChunk.y + 1, posInChunk.z) + new Vector3(chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z) * Chunk.Size) != 12)
            {
                MGame.Instance.SetVoxel(new Vector3(posInChunk.x, posInChunk.y, posInChunk.z) + new Vector3(chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z) * Chunk.Size, 2);
                return true;
            }

            return false;
        }
    }
}
