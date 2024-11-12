using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FantasyVoxels.Blocks
{
    public class WaterBlock : Block
    {
        private List<(int x, int y, int z)> blocksToUpdateNextTick = new List<(int x, int y, int z)>();
        private float nextBlockTick = 0.25f;
        private Timer timer;

        const int WATER = 3;

        public WaterBlock()
        {
            supportsCustomMeshing = true;
            smoothLightingEnable = true;
        }

        public override List<VertexPositionNormalTexture> CustomMesh(int x, int y, int z, int checkFace, int otherVoxel, Vector2 baseUVOffset, Vector3 chunkPos)
        {
            var verts = new List<VertexPositionNormalTexture>();
            Vector3 normal = new Vector3(Chunk.positionChecks[checkFace].x, Chunk.positionChecks[checkFace].y, Chunk.positionChecks[checkFace].z);

            Vector3 scale = Vector3.One;

            void GetScale(Vector3 pos)
            {
                Vector3 snapped = Vector3.Floor(pos + new Vector3(x, y, z));
                scale.Y = 0.8f;

                {
                    int minx = (int)(snapped.X - 1);
                    int miny = (int)(snapped.Y - 1);
                    int minz = (int)(snapped.Z - 1);
                    int maxx = (int)(snapped.X + 1);
                    int maxy = (int)(snapped.Y + 1);
                    int maxz = (int)(snapped.Z + 1);
                    int samples = 0;

                    for (int xx = minx; xx < maxx; xx++)
                    {
                        int yy = y + 1;
                        {
                            for (int zz = minz; zz < maxz; zz++)
                            {
                                if (xx == x && yy == y && zz == z) break;

                                int success = MGame.Instance.GrabVoxel(new Vector3(xx + chunkPos.X * Chunk.Size, yy + chunkPos.Y * Chunk.Size, zz + chunkPos.Z * Chunk.Size));

                                if (success == -1) continue;

                                if (success == WATER)
                                {
                                    scale.Y = 1; break;
                                }
                            }
                        }
                    }
                }
            }

            GetScale(Chunk.vertsPerCheck[checkFace * 4 + 0]);
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[checkFace * 4 + 0] * scale + new Vector3(x, y, z), normal, baseUVOffset + Chunk.uvs[checkFace * 4 + 0] / (16)));

            GetScale(Chunk.vertsPerCheck[checkFace * 4 + 1]);
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[checkFace * 4 + 1] * scale + new Vector3(x, y, z), normal, baseUVOffset + Chunk.uvs[checkFace * 4 + 1] / (16)));

            GetScale(Chunk.vertsPerCheck[checkFace * 4 + 2]);
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[checkFace * 4 + 2] * scale + new Vector3(x, y, z), normal, baseUVOffset + Chunk.uvs[checkFace * 4 + 2] / (16)));

            GetScale(Chunk.vertsPerCheck[checkFace * 4 + 0]);
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[checkFace * 4 + 0] * scale + new Vector3(x, y, z), normal, baseUVOffset + Chunk.uvs[checkFace * 4 + 0] / (16)));

            GetScale(Chunk.vertsPerCheck[checkFace * 4 + 2]);
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[checkFace * 4 + 2] * scale + new Vector3(x, y, z), normal, baseUVOffset + Chunk.uvs[checkFace * 4 + 2] / (16)));

            GetScale(Chunk.vertsPerCheck[checkFace * 4 + 3]);
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[checkFace * 4 + 3] * scale + new Vector3(x, y, z), normal, baseUVOffset + Chunk.uvs[checkFace * 4 + 3] / (16)));

            return verts;
        }
        public override bool ShouldMeshFace(int checkFace, int otherVoxel)
        {
            bool shouldMeshFace = otherVoxel == 0 || (Voxel.voxelTypes[otherVoxel].isTransparent && otherVoxel != WATER) || (checkFace == 2 && otherVoxel != WATER);

            return shouldMeshFace;
        }

        protected override bool BlockUpdate((int x, int y, int z) p, Chunk chunk)
        {
            EnqueueBlockNextTick(p.x + chunk.chunkPos.x * Chunk.Size, p.y + chunk.chunkPos.y * Chunk.Size, p.z + chunk.chunkPos.z * Chunk.Size);

            return false;
        }
        private void Flow((int x, int y, int z) p, Chunk chunk)
        {
            (int x, int y, int z)[] checks =
            [
                (0, -1, 0),
                (-1, 0, 0),
                (1, 0, 0),
                (0, 0, -1),
                (0, 0, 1),
            ];

            for (int i = 0; i < checks.Length; i++)
            {
                bool successfulspread = false;

                (int x, int y, int z) newPos = (p.x + checks[i].x, p.y + checks[i].y, p.z + checks[i].z);

                if (Chunk.IsOutOfBounds(newPos))
                {
                    newPos.x += chunk.chunkPos.x * Chunk.Size;
                    newPos.y += chunk.chunkPos.y * Chunk.Size;
                    newPos.z += chunk.chunkPos.z * Chunk.Size;

                    int cx = (int)MathF.Floor((float)newPos.x / Chunk.Size);
                    int cy = (int)MathF.Floor((float)newPos.y / Chunk.Size);
                    int cz = (int)MathF.Floor((float)newPos.z / Chunk.Size);

                    long chunkID = MGame.CCPos((cx, cy, cz));

                    if (MGame.Instance.loadedChunks.ContainsKey(chunkID) && MGame.Instance.loadedChunks[chunkID].generated)
                    {
                        int x = (int)((float)newPos.x - cx * Chunk.Size);
                        int y = (int)((float)newPos.y - cy * Chunk.Size);
                        int z = (int)((float)newPos.z - cz * Chunk.Size);

                        int index = x + Chunk.Size * (y + Chunk.Size * z);

                        if (MGame.Instance.loadedChunks[chunkID].voxels[index] == 0)
                        {
                            MGame.Instance.loadedChunks[chunkID].Modify(x, y, z, WATER);
                            EnqueueBlockNextTick(newPos.x, newPos.y, newPos.z);
                            successfulspread = true;
                        }
                        if (MGame.Instance.loadedChunks[chunkID].voxels[index] == WATER) successfulspread = true;
                    }
                }
                else
                {
                    int index = newPos.x + Chunk.Size * (newPos.y + Chunk.Size * newPos.z);

                    if (chunk.voxels[index] == 0)
                    {
                        chunk.Modify(newPos.x, newPos.y, newPos.z, WATER);
                        EnqueueBlockNextTick(newPos.x + chunk.chunkPos.x * Chunk.Size, newPos.y + chunk.chunkPos.y * Chunk.Size, newPos.z + chunk.chunkPos.z * Chunk.Size);
                        successfulspread = true;
                    }
                    if (chunk.voxels[index] == WATER) successfulspread = true;
                }

                if (successfulspread && i == 0)
                    break;
            }
        }
        protected void EnqueueBlockNextTick(int x, int y, int z)
        {
            if (blocksToUpdateNextTick.Contains((x, y, z))) return;

            blocksToUpdateNextTick.Add((x, y, z));
        }
        public override void Init()
        {
            timer = new Timer(TimeSpan.FromMilliseconds(500));
            timer.Elapsed += TickBlocks;
            timer.Start();
        }

        private void TickBlocks(object sender, ElapsedEventArgs e)
        {
            var copy = blocksToUpdateNextTick.ToArray();
            blocksToUpdateNextTick.Clear();
            Parallel.ForEach(copy, ((int x, int y, int z) blockUpdate) =>
            {
                int cx = (int)MathF.Floor((float)blockUpdate.x / Chunk.Size);
                int cy = (int)MathF.Floor((float)blockUpdate.y / Chunk.Size);
                int cz = (int)MathF.Floor((float)blockUpdate.z / Chunk.Size);

                if (MGame.Instance.loadedChunks.ContainsKey(MGame.CCPos((cx, cy, cz))))
                {
                    int x = (int)((float)blockUpdate.x - cx * Chunk.Size);
                    int y = (int)((float)blockUpdate.y - cy * Chunk.Size);
                    int z = (int)((float)blockUpdate.z - cz * Chunk.Size);

                    int vox = MGame.Instance.loadedChunks[MGame.CCPos((cx, cy, cz))].voxels[x + Chunk.Size * (y + Chunk.Size * z)];

                    if (Voxel.voxelTypes[vox].myClass is WaterBlock) 
                        ((WaterBlock)Voxel.voxelTypes[vox].myClass).Flow((x, y, z), MGame.Instance.loadedChunks[MGame.CCPos((cx, cy, cz))]);
                }
            });
        }
    }
}
