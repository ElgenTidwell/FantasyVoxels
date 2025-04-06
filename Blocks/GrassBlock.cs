using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace FantasyVoxels.Blocks
{
    public class GrassBlock : Block
    {
        public static Vector3[] vertsPerCheck =
        {
            new (1,0,1),
            new (1,1,1),
            new (1,1,0),
            new (1,0,0),

            new (0,0,0),
            new (0,1,0),
            new (0,1,1),
            new (0,0,1),

            new (0,1,0),
            new (1,1,0),
            new (1,1,1),
            new (0,1,1),

            new (0,0,1),
            new (1,0,1),
            new (1,0,0),
            new (0,0,0),

            new (0,1,1),
            new (1,1,1),
            new (1,0,1),
            new (0,0,1),

            new (0,0,0),
            new (1,0,0),
            new (1,1,0),
            new (0,1,0),
        };

        Vector2 dirtcoords = (new Vector2(31 % (MGame.AtlasSize / 16), 31 / (MGame.AtlasSize / 16)) * 16)/MGame.AtlasSize;
        public GrassBlock()
        {
            smoothLightingEnable = true;
            supportsCustomMeshing = true;
            customMeshColorControl = true;
        }

        public override void Init()
        {
        }

        public override bool ShouldMeshFace(int checkFace, int vox)
        {
            return vox == 0 || (Voxel.voxelTypes[vox].isTransparent && vox != 1) || Voxel.voxelTypes[vox].renderNeighbors;
        }

        public override List<VertexPositionColorNormalTexture> CustomMeshColorControl(int x, int y, int z, int p, int otherVoxel, Vector2 baseUVOffset, Vector3 chunkPos)
        {
            Color color = new Color(0,0,0,1);
            Vector3 normal = new Vector3(Chunk.positionChecks[p].x, Chunk.positionChecks[p].y, Chunk.positionChecks[p].z);


            var verts = new List<VertexPositionColorNormalTexture>();

            if(normal.Y == 0)
            {
                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0] + new Vector3(x, y, z), Color.Black, normal, dirtcoords + Chunk.uvs[p * 4 + 0] / (MGame.AtlasSize / 16)));
                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 1] + new Vector3(x, y, z), Color.Black, normal, dirtcoords + Chunk.uvs[p * 4 + 1] / (MGame.AtlasSize / 16)));
                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2] + new Vector3(x, y, z), Color.Black, normal, dirtcoords + Chunk.uvs[p * 4 + 2] / (MGame.AtlasSize / 16)));
                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0] + new Vector3(x, y, z), Color.Black, normal, dirtcoords + Chunk.uvs[p * 4 + 0] / (MGame.AtlasSize / 16)));
                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2] + new Vector3(x, y, z), Color.Black, normal, dirtcoords + Chunk.uvs[p * 4 + 2] / (MGame.AtlasSize / 16)));
                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 3] + new Vector3(x, y, z), Color.Black, normal, dirtcoords + Chunk.uvs[p * 4 + 3] / (MGame.AtlasSize / 16)));
            }

            verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0] + new Vector3(x, y, z), Color.White, normal, baseUVOffset + Chunk.uvs[p * 4 + 0] / (MGame.AtlasSize / 16)));
            verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 1] + new Vector3(x, y, z), Color.White, normal, baseUVOffset + Chunk.uvs[p * 4 + 1] / (MGame.AtlasSize / 16)));
            verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2] + new Vector3(x, y, z), Color.White, normal, baseUVOffset + Chunk.uvs[p * 4 + 2] / (MGame.AtlasSize / 16)));
            verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0] + new Vector3(x, y, z), Color.White, normal, baseUVOffset + Chunk.uvs[p * 4 + 0] / (MGame.AtlasSize / 16)));
            verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2] + new Vector3(x, y, z), Color.White, normal, baseUVOffset + Chunk.uvs[p * 4 + 2] / (MGame.AtlasSize / 16)));
            verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 3] + new Vector3(x, y, z), Color.White, normal, baseUVOffset + Chunk.uvs[p * 4 + 3] / (MGame.AtlasSize / 16)));

            return verts;
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
