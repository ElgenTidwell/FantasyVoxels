using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    public class BasicFlowerBlock : Block
    {
        static Vector3[] vertices =
        {
            new (0.0f,-0.5f,0.5f),
            new (0.0f,0.5f,0.5f),
            new (0.0f,0.5f,-0.5f),
            new (0.0f,-0.5f,-0.5f),
        };
        public BasicFlowerBlock()
        {
            supportsCustomMeshing = true;
            smoothLightingEnable = false;
            customBounds = true;
        }
        public override void Init()
        {
        }

        public override bool ShouldMeshFace(int checkFace, int otherVoxel)
        {
            //just one face is all we need.
            return checkFace == 0;
        }
        public override BoundingBox GetCustomBounds(Voxel.PlacementSettings placement)
        {
            return new BoundingBox(new Vector3(0.15f, 0.0f, 0.15f), new Vector3(0.85f, 0.85f, 0.85f));
        }

        public override List<VertexPositionNormalTexture> CustomMesh(int x, int y, int z, int checkFace, int otherVoxel, Vector2 baseUVOffset, Vector3 chunkPos)
        {
            var verts = new List<VertexPositionNormalTexture>
            {
                //stem
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (MGame.AtlasSize / 16))),
                                                           
                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
                
                //stem2
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (MGame.AtlasSize / 16))),

                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (MGame.AtlasSize / 16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.Up, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (MGame.AtlasSize / 16))),
            };

            Matrix translate = Matrix.CreateRotationY(MathHelper.ToRadians(45)) * Matrix.CreateTranslation(0.5f,0.5f,0.5f);
            for (int i = 0; i < 12; i ++)
            {
                verts[i] = new VertexPositionNormalTexture(Vector3.Transform(verts[i].Position, translate)+new Vector3(x,y,z), verts[i].Normal, verts[i].TextureCoordinate);
            }
            translate = Matrix.CreateRotationY(MathHelper.ToRadians(-45)) * Matrix.CreateTranslation(0.5f,0.5f, 0.5f);
            for (int i = 12; i < 24; i++)
            {
                verts[i] = new VertexPositionNormalTexture(Vector3.Transform(verts[i].Position, translate) + new Vector3(x, y, z), verts[i].Normal, verts[i].TextureCoordinate);
            }

            return verts;
        }

        protected override bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk)
        {
            if(MGame.Instance.GrabVoxel(new Vector3(posInChunk.x,posInChunk.y-1,posInChunk.z)+new Vector3(chunk.chunkPos.x,chunk.chunkPos.y,chunk.chunkPos.z)*Chunk.Size) == 0)
            {
                MGame.Instance.SetVoxel(new Vector3(posInChunk.x, posInChunk.y, posInChunk.z) + new Vector3(chunk.chunkPos.x, chunk.chunkPos.y, chunk.chunkPos.z) * Chunk.Size, 0);
                return true;
            }

            return false;
        }
    }
}
