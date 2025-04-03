using FantasyVoxels.ItemManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FantasyVoxels.Voxel;

namespace FantasyVoxels.Blocks
{
    [System.Serializable]
    public struct CompactBlockPos
    {
        public int x; 
        public int y; 
        public int z; 
        public long chunkID;
    }
    public interface TickingCustomDataBlock
    {
        public void tick(CompactBlockPos pos);
    }
    public interface TextureOverrideCustomDataBlock
    {
		public short topTexture { get; }
		public short bottomTexture { get; }
		public short leftTexture { get; }
		public short rightTexture { get; }
		public short frontTexture { get; }
		public short backTexture { get; }
	}
	public interface BlockLightOverrideCustomDataBlock
	{
		public byte light { get; }
	}
	public abstract class Block
    {
        protected int myVoxelID;
        public bool supportsCustomMeshing;
        public bool smoothLightingEnable;
        public bool customDrops;
        public bool customBounds;
        public static Dictionary<CompactBlockPos, object> blockCustomData = new Dictionary<CompactBlockPos, object>();

        public static void TickBlockTickers()
        {
            foreach(var block in blockCustomData)
            {
                if(block.Value is TickingCustomDataBlock)
                {
                    (block.Value as TickingCustomDataBlock).tick(block.Key);
                }
            }
        }

        public Block SetTextureData(TextureSetSettings settings, short texture)
        {
            if (settings.HasFlag(TextureSetSettings.RIGHT)) voxelTypes[myVoxelID].rightTexture = texture;
            if (settings.HasFlag(TextureSetSettings.LEFT)) voxelTypes[myVoxelID].leftTexture = texture;
            if (settings.HasFlag(TextureSetSettings.FRONT)) voxelTypes[myVoxelID].frontTexture = texture;
            if (settings.HasFlag(TextureSetSettings.BACK)) voxelTypes[myVoxelID].backTexture = texture;
            if (settings.HasFlag(TextureSetSettings.TOP)) voxelTypes[myVoxelID].topTexture = texture;
            if (settings.HasFlag(TextureSetSettings.BOTTOM)) voxelTypes[myVoxelID].bottomTexture = texture;

            return this;
        }

        public Block()
        {

        }
        /// <summary>
        /// Updates a voxel using the global instance of a Block class
        /// </summary>
        /// <param name="posInChunk">The position of the voxel to update within the parent chunk</param>
        /// <param name="chunk">The parent chunk</param>
        /// <returns>Whether to instantly propogate to the surrounding voxels. For things like water, this would instantly fill an area, so it should be false in that case.</returns>
        protected abstract bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk);
        public virtual bool UseBlock((int x, int y, int z) posInChunk, Chunk chunk, Entity from) { return false; }
        public virtual void PlaceBlock((int x, int y, int z) posInChunk, Chunk chunk) { }
        public virtual void BreakBlock((int x, int y, int z) posInChunk, Chunk chunk) { }
        public virtual List<VertexPositionNormalTexture> CustomMesh(int x, int y, int z, int checkFace, int otherVoxel, Vector2 baseUVOffset, Vector3 chunkPos)
        {
            return null;
        }
        public virtual bool ShouldMeshFace(int checkFace, int otherVoxel)
        {
            return false;
        }
        public virtual bool CanPlace(PlacementSettings placement, int otherVoxel)
        {
            return true;
        }
        public virtual Item[] GetCustomDrops()
        {
            return null;
        }
        public virtual BoundingBox GetCustomBounds(PlacementSettings placement)
        {
            return new BoundingBox();
        }
        public abstract void Init();
        public void TryUpdateBlock((int x, int y, int z) posInChunk, Chunk chunk, bool force = false)
        {
            bool update = BlockUpdate(posInChunk, chunk);
            if (update || force)
            {
                for (int p = 0; p < 6; p++)
                {
                    (int x, int y, int z) newPos = (posInChunk.x + Chunk.positionChecks[p].x, posInChunk.y + Chunk.positionChecks[p].y, posInChunk.z + Chunk.positionChecks[p].z);

                    if (Chunk.IsOutOfBounds(newPos))
                    {
                        MGame.Instance.UpdateBlock(new Microsoft.Xna.Framework.Vector3(newPos.x + chunk.chunkPos.x * Chunk.Size, newPos.y + chunk.chunkPos.y * Chunk.Size, newPos.z + chunk.chunkPos.z * Chunk.Size));
                    }
                    else
                    {
                        int index = newPos.x + Chunk.Size * (newPos.y + Chunk.Size * newPos.z);
                        voxelTypes[chunk.voxels[index]].myClass?.TryUpdateBlock(newPos, chunk);
                    }
                }
            }
        }
        public static CompactBlockPos GetPos(int x, int y, int z, long chunkID)
        {
            return new CompactBlockPos { x = x, y = y, z = z, chunkID = chunkID };
        }
        public static void MarkChunkDirty(long id)
        {
            MGame.Instance.loadedChunks[id].Remesh();
        }
    }
}
