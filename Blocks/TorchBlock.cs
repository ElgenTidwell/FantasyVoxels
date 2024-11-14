using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    internal class TorchBlock : Block
    {
        static Vector3[] vertices =
        {
            new (0.0f,-0.5f,0.5f),
            new (0.0f,0.5f,0.5f),
            new (0.0f,0.5f,-0.5f),
            new (0.0f,-0.5f,-0.5f),
        };
        public TorchBlock()
        {
            supportsCustomMeshing = true;
            smoothLightingEnable = false;
        }
        public override void Init()
        {
        }

        public override bool ShouldMeshFace(int checkFace, int otherVoxel)
        {
            //just one face is all we need.
            return checkFace == 0;
        }
        public override List<VertexPositionNormalTexture> CustomMesh(int x, int y, int z, int checkFace, int otherVoxel, Vector2 baseUVOffset, Vector3 chunkPos)
        {
            const float pix = 1 / 16f;

            Voxel.PlacementSettings placement = Voxel.PlacementSettings.BOTTOM;
            if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos(((int)chunkPos.X, (int)chunkPos.Y, (int)chunkPos.Z)), out var chunk))
            {
                placement = chunk.voxeldata[x + Chunk.Size * (y + Chunk.Size * z)].placement;
            }

            var verts = new List<VertexPositionNormalTexture>
            {
                //stem
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                
                //stem2
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[1], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[3], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[1], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[3], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitX, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),

                
                //stem
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                
                //stem2
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[1], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[3], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[1], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]) / (16))),
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),
                new VertexPositionNormalTexture(vertices[3], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]) / (16))),
                new VertexPositionNormalTexture(vertices[2], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]) / (16))),
                new VertexPositionNormalTexture(vertices[0], -Vector3.UnitZ, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]) / (16))),

                //top
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitY, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]*pix*2+(new Vector2(7,6))*pix) / (16))),
                new VertexPositionNormalTexture(vertices[1], Vector3.UnitY, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 1]*pix*2+(new Vector2(7,6))*pix) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitY, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]*pix*2+(new Vector2(7,6))*pix) / (16))),
                new VertexPositionNormalTexture(vertices[0], Vector3.UnitY, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 0]*pix*2+(new Vector2(7,6))*pix) / (16))),
                new VertexPositionNormalTexture(vertices[2], Vector3.UnitY, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 2]*pix*2+(new Vector2(7,6))*pix) / (16))),
                new VertexPositionNormalTexture(vertices[3], Vector3.UnitY, baseUVOffset + ((Chunk.uvs[checkFace * 4 + 3]*pix*2+(new Vector2(7,6))*pix) / (16))),
            };

            const int size = 12;

            float nX = 0.0f;
            float nY = 0.0f;
            float nZ = 0.0f;

            Matrix rotation = Matrix.CreateTranslation(-0.5f,-0.5f,-0.5f);
            const float wallrot = 0.44f;

            switch (placement)
            {
                case Voxel.PlacementSettings.LEFT:
                    nX = -0.28f;

                    nY = pix * 2;

                    rotation *= Matrix.CreateRotationZ(-wallrot);

                    break;
                case Voxel.PlacementSettings.RIGHT:
                    nX = 0.28f;

                    nY = pix * 2;

                    rotation *= Matrix.CreateRotationZ(wallrot);

                    break;
                case Voxel.PlacementSettings.FRONT:
                    nZ = 0.28f;

                    nY = pix * 2;

                    rotation *= Matrix.CreateRotationX(-wallrot);

                    break;
                case Voxel.PlacementSettings.BACK:
                    nZ = -0.28f;

                    nY = pix * 2;

                    rotation *= Matrix.CreateRotationX(wallrot);

                    break;
            }
            rotation *= Matrix.CreateTranslation(0.5f+nX, 0.5f+nY, 0.5f+nZ);

            Matrix translate = Matrix.CreateRotationY(MathHelper.ToRadians(90)) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f + pix) * rotation;
            int index = 0;
            for (int i = size * index++; i < size * index; i++)
            {
                verts[i] = new VertexPositionNormalTexture(Vector3.Transform(verts[i].Position, translate) + new Vector3(x, y, z), verts[i].Normal, verts[i].TextureCoordinate);
            }
            translate = Matrix.CreateRotationY(MathHelper.ToRadians(-90)) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f - pix) * rotation;
            for (int i = size * index++; i < size * index; i++)
            {
                verts[i] = new VertexPositionNormalTexture(Vector3.Transform(verts[i].Position, translate) + new Vector3(x, y, z), verts[i].Normal, verts[i].TextureCoordinate);
            }

            translate = Matrix.CreateRotationY(MathHelper.ToRadians(0)) * Matrix.CreateTranslation(0.5f + pix, 0.5f, 0.5f) * rotation;
            for (int i = size * index++; i < size * index; i++)
            {
                verts[i] = new VertexPositionNormalTexture(Vector3.Transform(verts[i].Position, translate) + new Vector3(x, y, z), verts[i].Normal, verts[i].TextureCoordinate);
            }
            translate = Matrix.CreateRotationY(MathHelper.ToRadians(180)) * Matrix.CreateTranslation(0.5f - pix, 0.5f, 0.5f) * rotation;
            for (int i = size * index++; i < size * index; i++)
            {
                verts[i] = new VertexPositionNormalTexture(Vector3.Transform(verts[i].Position, translate) + new Vector3(x, y, z), verts[i].Normal, verts[i].TextureCoordinate);
            }

            translate = Matrix.CreateScale(pix*2) * Matrix.CreateRotationZ(MathHelper.ToRadians(90)) * Matrix.CreateTranslation(0.5f, pix * 10, 0.5f) * rotation;
            for (int i = size * index; i < size * index + 6; i++)
            {
                verts[i] = new VertexPositionNormalTexture(Vector3.Transform(verts[i].Position, translate) + new Vector3(x, y, z), verts[i].Normal, verts[i].TextureCoordinate);
            }

            return verts;
        }

        protected override bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk)
        {
            return false;
        }
    }
}
