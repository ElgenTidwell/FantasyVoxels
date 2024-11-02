using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FantasyVoxels
{
    public struct Voxel
    {
        public static Voxel[] voxelTypes =
        [
            new Voxel(((int x, int y, int z) pos) =>
            {
                return 1f;
            }),
            //Grass
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble()*0.1f+1 + IcariaNoise.GradientNoise3D(pos.x * 0.06f, pos.y * 0.06f, pos.z * 0.06f)*0.1f);
            }),
            //Dirt
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble()*0.1f+1);
            }),
            //Water
            new Voxel(((int x, int y, int z) pos) =>
            {
                return MathF.Abs(MathF.Sin((IcariaNoise.GradientNoise3D(pos.x * 0.1f, pos.y * 0.1f, pos.z * 0.1f))))*0.1f + 0.9f + (float)(Random.Shared.NextDouble() * 0.05f);
            },true,1,true,true,2),
            //Sand
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble() * 0.1f + 1);
            }),
            //Bark
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)((Random.Shared.NextDouble() * 0.1f + 1)*(IcariaNoise.GradientNoise3D(pos.x * 0.9f, pos.y * 0.1f, pos.z * 0.9f)*0.5f+0.5f));
            }),
            //Log
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble() * 0.1f + 1);
            }),
            //Short Grass
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble() * 0.2f + 0.8f);
            },true, 2,lightPassthrough:0),
            //Leaves
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble() * 0.2f + 0.8f);
            }, shaderEffect: 2,lightPassthrough:10),
            //stone
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)((Random.Shared.NextDouble() * 0.1f + 1) * (IcariaNoise.GradientNoise3D(pos.x * 0.2f, pos.y * 0.9f, pos.z * 0.2f) * 0.2f + 0.8f));
            }),
            //Plank
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)((Random.Shared.NextDouble() * 0.13f + 0.9f));
            }),
            //Brick
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)((Random.Shared.NextDouble() * 0.13f + 0.9f));
            }),
        ];

        public Func<(int x, int y, int z),float> GetShade;
        public bool ignoreCollision,isTransparent,isLiquid;
        public int shaderEffect, lightPassthrough;

        public Voxel(Func<(int x, int y, int z), float> GetShade, bool ignoreCollision = false, int shaderEffect = 0, bool isTransparent = false, bool isLiquid = false, int lightPassthrough = 200)
        {
            this.GetShade = GetShade;
            this.ignoreCollision = ignoreCollision;
            this.isTransparent = isTransparent;
            this.shaderEffect = shaderEffect;
            this.isLiquid = isLiquid;
            this.lightPassthrough = lightPassthrough;
        }
    }
    public struct VoxelData
    {
        public int shade;
    }
    public class Chunk
    {
        public const int Size = 32;
        public (int x, int y, int z) chunkPos;
        public byte[] voxels = new byte[Size*Size*Size];
        public VoxelData[] voxeldata = new VoxelData[Size*Size*Size];

        public Texture3D voxelDataTexture;
        public Color[] voxelDataColors = new Color[Size * Size * Size];

        public bool queueModified,queueInWorks;

        public bool CompletelyEmpty;

        public static VertexPosition[] chunkVerts = 
        [
            new VertexPosition(new Vector3 (0, 0, 0)),
            new VertexPosition(new Vector3 (1, 0, 0)),
            new VertexPosition(new Vector3 (1, 1, 0)),
            new VertexPosition(new Vector3 (0, 1, 0)),
            new VertexPosition(new Vector3 (0, 1, 1)),
            new VertexPosition(new Vector3 (1, 1, 1)),
            new VertexPosition(new Vector3 (1, 0, 1)),
            new VertexPosition(new Vector3 (0, 0, 1)),
        ];
        public static short[] triangles = 
        [
            1, 2, 5, //face right
	        1, 5, 6,
            0, 7, 4, //face left
	        0, 4, 3,
            0, 2, 1, //face front
	        0, 3, 2,
            5, 4, 7, //face back
	        5, 7, 6,
            2, 3, 4, //face top
	        2, 4, 5,
            0, 6, 7, //face bottom
	        0, 1, 6
        ];
        static Vector3[] vertsPerCheck =
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
        public static (int x, int y, int z)[] positionChecks =
        {
            (1,0,0),
            (-1,0,0),
            (0,1,0),
            (0,-1,0),
            (0,0,1),
            (0,0,-1),
        };
        static Vector2[] uvs =
        {
            new (0,1),
            new (0,0),
            new (1,0),
            new (1,1),

            new (0,1),
            new (0,0),
            new (1,0),
            new (1,1),

            new (0,1),
            new (0,0),
            new (1,0),
            new (1,1),

            new (0,1),
            new (0,0),
            new (1,0),
            new (1,1),

            new (1,0),
            new (0,0),
            new (0,1),
            new (1,1),

            new (1,1),
            new (0,1),
            new (0,0),
            new (1,0),
        };
        public static VertexBuffer chunkBuffer;
        public VertexBuffer[] chunkVertexBuffers;
        public ushort[,] vSidesStart = new ushort[5,6];

        public int[,] skylightAbove = null; 

        public bool[,] sidesVisible = new bool[6,6];
        public bool[] facesVisibleAtAll = new bool[6];
        public bool generated = false, modified = false,lightOutOfDate = false;
        public bool[] meshUpdated = new bool[4];
        public int MaxY;
        Random tRandom;

        private int[,] tHeight = new int[Size, Size];
        private bool[,,] visibilityPropogation = new bool[Size, Size, Size];

        public Chunk()
        {
            chunkVertexBuffers = new VertexBuffer[5];
            tRandom = new Random(MGame.Instance.seed + 4);

            voxelDataTexture = new Texture3D(MGame.Instance.GraphicsDevice,Size,Size,Size,false,SurfaceFormat.Color);
        }
        public static float GetOctaveNoise3D(float x, float y, float z, float frequency, int octaveCount, float persistence, float lacunarity, int seedOffset = 0)
        {
            float totalNoise = 0;
            float amplitude = 1;
            float maxAmplitude = 0; // Used to normalize the result
            for (int i = 0; i < octaveCount; i++)
            {
                totalNoise += IcariaNoise.GradientNoise3D(x * frequency, y * frequency, z * frequency,MGame.Instance.seed-10) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= persistence;    // Decrease amplitude for each octave
                frequency *= lacunarity;     // Increase frequency for each octave
            }
            return totalNoise;
        }
        public static int GetTerrainHeight(float samplex, float samplez)
        {
            float ocean = MathF.Pow((MathF.Min(IcariaNoise.GradientNoise(samplex * 0.004f, samplez * 0.004f, MGame.Instance.seed - 1), 0)), 2);
            float hill = MathF.Abs(IcariaNoise.GradientNoise(samplex * 0.002f, samplez * 0.002f, MGame.Instance.seed)) *220;
            float terrainHeight = ocean * -220 + 10+hill*(1-(MathF.Min(ocean*50,1)));

            return (int)terrainHeight;
        }

        public static byte GetVoxel(float x, float y, float z, int terrainHeight)
        {
            // Main terrain voxel assignment with 3D noise layers
            byte voxel = (byte)(y <= terrainHeight ? y < 5 ? 4 : 2 : 0);

            // Water voxel assignment for regions below sea level
            if (y < 5 && y >= terrainHeight)
                voxel = 3;

            if (y < terrainHeight - 30 - IcariaNoise.GradientNoise(x * 0.9f, z * 0.9f) * 20 && voxel != 0)
                voxel = 9;

            return voxel;
        }
        public void Generate()
        {
            CompletelyEmpty = true;
            Array.Fill(voxels,(byte)0);
            Array.Fill(voxeldata,new VoxelData());
            int elementsCount = 0;
            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    int samplex = x + chunkPos.x * Size, samplez = z + chunkPos.z * Size;

                    //float ocean = (IcariaNoise.GradientNoise(samplex * 0.001f, samplez * 0.001f, 1 + MGame.Instance.seed) +1f);
                    float ocean = 0f;
                    int terrainHeight = (int)(GetTerrainHeight(samplex, samplez)*(1-ocean) + (ocean*2));
                    tHeight[x, z] = terrainHeight;

                    int shortGrassHeight = tRandom.Next(1,4);
                    float shortGrassChance = (float)(tRandom.NextDouble() * 2 - 1);
                    bool grassed = false;

                    for (int y = Size-1; y >= 0; y--)
                    {
                        int sampley = y + chunkPos.y * Size;

                        // Main terrain voxel assignment with 3D noise layers
                        voxels[x + Size * (y + Size * z)] = GetVoxel(samplex,sampley,samplez,terrainHeight);

                        if (voxels[x + Size * (y + Size * z)] != 0)
                        {
                            CompletelyEmpty = false;
                            MaxY = Math.Max(MaxY, y);
                        }
                        if(voxels[x + Size * (y + Size * z)] == 0 || Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].isTransparent)
                        {
                            visibilityPropogation[x, y, z] = true;
                        }

                        float patchyRandom = IcariaNoise.GradientNoise(samplex * 0.1f, sampley * 0.1f, MGame.Instance.seed - 10);
                        grassed = GetVoxel(samplex, sampley + 2, samplez, terrainHeight) == 2 || sampley < terrainHeight - 1 || sampley > (patchyRandom * 100 + 410);
                        if (voxels[x + Size * (y + Size * z)] == 2 && (!grassed || MGame.Instance.worldRandom.Next(-100, 100) == 10))
                        {
                            grassed = true;

                            voxels[x + Size * (y + Size * z)] = (byte)(sampley < 10 ? 4 : 1);
                        }

                        if (voxels[x + Size * (y + Size * z)] == 4 && sampley < patchyRandom * 40 - 45)
                        {
                            voxels[x + Size * (y + Size * z)] = 2;
                        }

                        float r = IcariaNoise.CellularNoise(samplex * 0.1f, samplez * 0.1f, MGame.Instance.seed - 10).r;

                        if (voxels[x + Size * (y + Size * z)] == 1 && (int)(r * 120) == MGame.Instance.worldRandom.Next(0, 120) && sampley == terrainHeight && shortGrassChance * MathF.Min(MathF.Max(sampley / 15, 0), 1) > 0.93f)
                        {
                            VoxelStructurePlacer.Place(samplex, sampley, samplez, new Tree());
                        }

                        if (voxels[x + Size * (y + Size * z)] == 1 && shortGrassChance * MathF.Min(MathF.Max(sampley / 15, 0), 1) > 0.93f)
                        {
                            VoxelStructurePlacer.Place(samplex, sampley + 1, samplez, new ShortGrass());
                        }

                        voxeldata[x + Size * (y + Size * z)].shade = (int)((MathF.Min(MathF.Max(terrainHeight - 10, 0), 100) * 0.2f + 190) * Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].GetShade((samplex, sampley, samplez)));


                        elementsCount++;
                    }
                }
            }

            if (!CheckQueue())
            {
                GenerateVisibility(true);
                Remesh(false);
            }

            generated = true;
        }
        /// <summary>
        /// Recomputes the lighting texture
        /// </summary>
        /// <param name="disableIteration">Disable the iterative fix to chunk edges</param>
        public void ReLight(bool disableIteration)
        {
            if (MGame.Instance.GraphicsDevice == null) return;

            int[,] previousSkylight = new int[Size, Size];
            //If there is some skylight trying to pass through this chunk, we should skip over this early out, so we can properly propogate downward
            if (CompletelyEmpty && skylightAbove == null)
            {
                lightOutOfDate = false;

                Array.Fill(voxelDataColors, Color.White);

                for (int x = 0; x < Size; x++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        previousSkylight[x, z] = 255;
                    }
                }
                if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y - 1, chunkPos.z)), out Chunk b))
                {
                    if (b.CompletelyEmpty) return;
                    b.skylightAbove = previousSkylight;
                    b.lightOutOfDate = true;
                }

                return;
            }
            else 
            if (CompletelyEmpty && skylightAbove != null)
            {
                lightOutOfDate = false;

                Array.Fill(voxelDataColors, Color.White);
                Array.Copy(skylightAbove, previousSkylight,skylightAbove.Length);

                if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y - 1, chunkPos.z)), out Chunk b))
                {
                    if (b.CompletelyEmpty) return;
                    b.skylightAbove = previousSkylight;
                    b.lightOutOfDate = true;
                }

                return;
            }

            bool propDownward = true;

            Array.Fill(voxelDataColors, Color.Black);

            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    int sunLight = skylightAbove == null? Size+chunkPos.y*Size >= tHeight[x, z] ? 255 : 0 : skylightAbove[x,z];
                    for (int y = MaxY+1; y >= 0; y--)
                    {
                        if (y >= Size) continue;

                        //TODO: blocklight
                        voxelDataColors[x + Size * (y + Size * z)].R = (byte)MathF.Min(MathF.Max(sunLight,0),255);

                        if (voxels[x + Size * (y + Size * z)] != 0) 
                            sunLight = (int)MathF.Max(sunLight - Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].lightPassthrough, 0);
                    }

                    if (sunLight > 0) propDownward = true; 

                    previousSkylight[x, z] = sunLight;
                }
            }
            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    for (int y = Size-1; y >= 0; y--)
                    {
                        if ((x > 0 && x < Size - 1) && (z > 0 && z < Size - 1) && (y > 0 && y < Size - 1)) continue;

                        for (int p = 0; p < 6; p++)
                        {
                            int ourLight = voxelDataColors[x + Size * (y + Size * z)].R;

                            (int x, int y, int z) newpos = (x + positionChecks[p].x, y + positionChecks[p].y, z + positionChecks[p].z);
                            if (IsOutOfBounds(newpos))
                            {
                                int cx = (int)MathF.Floor((float)newpos.x / Chunk.Size);
                                int cy = (int)MathF.Floor((float)newpos.y / Chunk.Size);
                                int cz = (int)MathF.Floor((float)newpos.z / Chunk.Size);

                                long pos = MGame.CCPos((cx + chunkPos.x, cy + chunkPos.y, cz + chunkPos.z));

                                if (MGame.Instance.loadedChunks.ContainsKey(pos))
                                {
                                    int _x = (int)(newpos.x - cx * Chunk.Size);
                                    int _y = (int)(newpos.y - cy * Chunk.Size);
                                    int _z = (int)(newpos.z - cz * Chunk.Size);

                                    int otherlight = MGame.Instance.loadedChunks[pos].voxelDataColors[_x + Size * (_y + Size * _z)].R;

                                    voxelDataColors[x + Size * (y + Size * z)].R = (byte)MathF.Max(otherlight, ourLight);

                                    if (ourLight - otherlight > 5) MGame.Instance.loadedChunks[pos].lightOutOfDate = true;
                                }
                            }
                        }
                    }
                }
            }
            Queue<(int tx, int ty, int tz, int x, int y, int z)> prop = new Queue<(int tx, int ty, int tz, int x, int y, int z)>();

            void grabandqueue(int x, int y, int z, int dx, int dy, int dz, int light)
            {
                if(!IsOutOfBounds((x+dx,y+dy,z+dz)))
                {
                    if (voxelDataColors[(x + dx) + Size * ((y + dy) + Size * (z + dz))].R < light &&
                        light - voxelDataColors[(x + dx) + Size * ((y + dy) + Size * (z + dz))].R > 5) prop.Enqueue((x, y, z, x + dx, y + dy, z + dz));
                }
            }

            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    for (int y = MaxY; y >= 0; y--)
                    {
                        if (y >= Size) continue;

                        if (voxels[x + Size * (y + Size * z)] != 0) continue;

                        int ourLight = voxelDataColors[x + Size * (y + Size * z)].R;
                        grabandqueue(x,y,z,-1,0,0,ourLight);
                        grabandqueue(x,y,z, 1,0,0,ourLight);
                        grabandqueue(x,y,z,0,-1,0,ourLight);
                        grabandqueue(x,y,z,0, 1,0,ourLight);
                        grabandqueue(x,y,z,0,0,-1,ourLight);
                        grabandqueue(x,y,z,0,0, 1,ourLight);
                    }
                }
            }

            while (prop.Count > 0)
            {
                (int rx, int ry, int rz, int x, int y, int z) = prop.Dequeue();

                if (voxels[rx + Size * (ry + Size * rz)] != 0) continue;
                if (voxelDataColors[rx + Size * (ry + Size * rz)].R - voxelDataColors[x + Size * (y + Size * z)].R < 25) continue;

                voxelDataColors[x + Size * (y + Size * z)].R += (byte)((voxelDataColors[rx + Size * (ry + Size * rz)].R - voxelDataColors[x + Size * (y + Size * z)].R) * 0.75f);

                int ourLight = voxelDataColors[x + Size * (y + Size * z)].R;

                grabandqueue(x, y, z, -1, 0, 0, ourLight);
                grabandqueue(x, y, z, 1, 0, 0, ourLight);
                grabandqueue(x, y, z, 0, -1, 0, ourLight);
                grabandqueue(x, y, z, 0, 1, 0, ourLight);
                grabandqueue(x, y, z, 0, 0, -1, ourLight);
                grabandqueue(x, y, z, 0, 0, 1, ourLight);

                //if(y == 0)
                //{
                //    if (ourLight > 0) propDownward = true;

                //    previousSkylight[x, z] = int.Clamp(ourLight,0,255);
                //}
            }

            if (propDownward && MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y - 1, chunkPos.z)),out Chunk below))
            {
                below.skylightAbove = previousSkylight;
                below.lightOutOfDate = true;
            }

            voxelDataTexture.SetData(voxelDataColors,0, Size*Size*Size);
        }

        private void MeshLOD(int lod)
        {
            if (MGame.Instance.GraphicsDevice == null) return;

            List<VertexPositionColorNormalTexture> verts = new List<VertexPositionColorNormalTexture>();
            int numVerts = 0;
            int scale = lod!=4? (int)MathF.Pow(2, lod) :1;

            for (int p = 0; p < positionChecks.Length; p++)
            {
                vSidesStart[lod,p] = (ushort)numVerts;
                for (int x = 0; x < Size; x += scale)
                {
                    for (int z = 0; z < Size; z += scale)
                    {
                        int samplex = x + chunkPos.x * Size, samplez = z + chunkPos.z * Size;

                        int terrainHeight = x == 0 || z == 0 || x == Size - 1 || z == Size - 1 ? (int)(GetTerrainHeight(samplex / 2f, samplez / 2f)) : 0;
                        for (int y = 0; y <= MaxY; y += scale)
                        {
                            int sampley = y + chunkPos.y * Size;

                            if (IsOutOfBounds((x, y, z))) continue;

                            if (voxels[x + Size * (y + Size * z)] == 0) continue;
                            if (Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].isTransparent && lod != 4) continue;
                            if (!Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].isTransparent && lod == 4) continue;

                            (int x, int y, int z) checkPos = (positionChecks[p].x*scale + x, positionChecks[p].y * scale + y, positionChecks[p].z * scale + z);
                            bool placeFace = false;

                            Vector3 normal = new Vector3(positionChecks[p].x, positionChecks[p].y, positionChecks[p].z);

                            Color color = new Color((byte)voxels[x + Size * (y + Size * z)],
                                                    (byte)MathF.Min(voxeldata[x + Size * (y + Size * z)].shade,255),
                                                    (byte)Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].shaderEffect,
                                                    (byte)0);

                            Vector2 UVCoords = Vector2.Zero;
                            switch ((p / 2))
                            {
                                case 0:
                                    UVCoords = new Vector2((samplez), (sampley));
                                    break;
                                case 1:
                                    UVCoords = new Vector2((samplez), (samplex));
                                    break;
                                case 2:
                                    UVCoords = new Vector2((samplex), (sampley));
                                    break;
                            }
                            if (UVCoords.X <= 0) UVCoords.X = 16 - (MathF.Abs(UVCoords.X+1) % 16);
                            if (UVCoords.Y <= 0) UVCoords.Y = 16 - (MathF.Abs(UVCoords.Y+1) % 16);

                            if (UVCoords.X > 0)  UVCoords.X = 16 - (MathF.Abs(UVCoords.X+1) % 16);
                            if (UVCoords.Y > 0)  UVCoords.Y = 16 - (MathF.Abs(UVCoords.Y+1) % 16);

                            UVCoords = Vector2.Floor(UVCoords)*2;

                            UVCoords.X %= 16;
                            UVCoords.Y %= 16;

                            UVCoords += new Vector2((voxels[x + Size * (y + Size * z)] - 1) % 16, (voxels[x + Size * (y + Size * z)] - 1) / 16) * 16;

                            UVCoords /= 256f;

                            int vox = 0;

                            if (IsOutOfBounds(checkPos))
                            {
                                int grabbed = MGame.Instance.GrabVoxel(new Vector3(samplex + positionChecks[p].x * (scale), sampley + positionChecks[p].y * (scale), samplez + positionChecks[p].z * (scale)));

                                if (grabbed == -1) vox = GetVoxel(samplex + positionChecks[p].x * (scale), sampley + positionChecks[p].y * (scale), samplez + positionChecks[p].z * (scale), terrainHeight);
                                else vox = grabbed;
                            }
                            else
                            {
                                vox = voxels[checkPos.x + Size * (checkPos.y + Size * checkPos.z)];
                            }
                            if (vox == 0 || (Voxel.voxelTypes[vox].isTransparent && vox != voxels[x + Size * (y + Size * z)]))
                            {
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0]*(scale) + new Vector3(x, y, z), color, normal, UVCoords+uvs[p * 4 + 0]/(16*8)));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 1]*(scale) + new Vector3(x, y, z), color, normal, UVCoords+uvs[p * 4 + 1]/(16*8)));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2]*(scale) + new Vector3(x, y, z), color, normal, UVCoords+uvs[p * 4 + 2]/(16*8)));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0]*(scale) + new Vector3(x, y, z), color, normal, UVCoords+uvs[p * 4 + 0]/(16*8)));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2]*(scale) + new Vector3(x, y, z), color, normal, UVCoords+uvs[p * 4 + 2]/(16*8)));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 3]*(scale) + new Vector3(x, y, z), color, normal, UVCoords+uvs[p * 4 + 3]/(16*8)));
                                numVerts++;
                            }
                        }
                    }
                }
            }

            if(lod != 4) meshUpdated[lod] = true;

            if (verts.Count == 0)
            {
                chunkVertexBuffers[lod] = null;
                return;
            }

            var temp = new VertexBuffer(MGame.Instance.GraphicsDevice, typeof(VertexPositionColorNormalTexture), verts.Count, BufferUsage.WriteOnly);
            temp.SetData(verts.ToArray());

            chunkVertexBuffers[lod] = temp;

            return;
        }

        public int GetLOD()
        {
            return (int)MathF.Floor(MathF.Min(Vector3.Distance(MGame.Instance.cameraPosition, new Vector3(chunkPos.x + 0.5f, chunkPos.y + 0.5f, chunkPos.z + 0.5f) * Size) / (512),3));
        }

        public void Remesh(bool all = false)
        {
            if (CompletelyEmpty) return;
            ReLight(true);
            if (!all)
            {
                MeshLOD(GetLOD());
            }
            else
            {
                for(int i = 0; i < 4; i++)
                    MeshLOD(i);
            }
            MeshLOD(4);
        }

        public bool CheckQueue(bool remesh = true)
        {
            queueModified = false;
            var queuedVoxels = VoxelStructurePlacer.GetQueue(chunkPos);
            if (queuedVoxels == null) return false;
            if (queuedVoxels.Count <= 1) return false;

            if (MGame.Instance.GraphicsDevice == null) return false;

            lightOutOfDate = true;
            if (MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y + 1, chunkPos.z)), out Chunk c))
            {
                c.lightOutOfDate = true;
            }

            List<Chunk> remeshNeighbors = new List<Chunk>();
            while (queuedVoxels.Count > 0)
            {
                if (!queuedVoxels.TryDequeue(out var p)) continue;

                if (p.x < 0 || p.y < 0 || p.z < 0 || p.x >= Size || p.y >= Size || p.z >= Size) continue;
                CompletelyEmpty = false;
                voxels[p.x + Size * (p.y + Size * p.z)] = (byte)p.vox;
                voxeldata[p.x + Size * (p.y + Size * p.z)].shade = (int)(240 * Voxel.voxelTypes[voxels[p.x + Size * (p.y + Size * p.z)]].GetShade((p.x,p.y,p.z)));
                MaxY = Math.Max(MaxY, p.y);
                Chunk neighbor;
                if (p.x == 0        && MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x - 1, chunkPos.y, chunkPos.z)), out neighbor))
                {
                    if(!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }
                if (p.x == Size - 1 && MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x + 1, chunkPos.y, chunkPos.z)), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }

                if (p.y == 0        && MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y - 1, chunkPos.z)), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }
                if (p.y == Size - 1 && MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y + 1, chunkPos.z)), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }

                if (p.z == 0        && MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y, chunkPos.z - 1)), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }
                if (p.z == Size - 1 && MGame.Instance.loadedChunks.TryGetValue(MGame.CCPos((chunkPos.x, chunkPos.y, chunkPos.z + 1)), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }
            }
            GenerateVisibility();
            queuedVoxels.Clear();
            meshUpdated = [false, false, false, false];

            foreach(var n in remeshNeighbors)
            {
                n.meshUpdated = [false, false, false, false];

                if (remesh)
                {
                    n.Remesh();
                }
            }

            if (remesh)
            {
                Remesh();
            }

            return true;
        }

        public void Modify(int x, int y, int z, int newVoxel)
        {
            if (x < 0 || y < 0 || z < 0 || x >= Size || y >= Size || z >= Size) return;

            if (MGame.Instance.GraphicsDevice == null) return;

            modified = true;
            lightOutOfDate = true;

            voxels[x + Size * (y + Size * z)] = (byte)newVoxel;
            voxeldata[x + Size * (y + Size * z)].shade = (int)(240 * Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].GetShade((x, y, z)));

            MaxY = Math.Max(MaxY, y);
            CompletelyEmpty = false;
            GenerateVisibility();
            meshUpdated = new bool[4] { false, false, false, false };
            Remesh();
        }
        public void ModifyData(int x, int y, int z, VoxelData newVoxel)
        {
            if (x < 0 || y < 0 || z < 0 || x >= Size || y >= Size || z >= Size) return;

            if (MGame.Instance.GraphicsDevice == null) return;

            modified = true;
            lightOutOfDate = true;

            voxeldata[x + Size * (y + Size * z)] = newVoxel;
            voxeldata[x + Size * (y + Size * z)].shade = (int)(240 * Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].GetShade((x, y, z)));
        }
        public void ModifyQueue(int x, int y, int z, int newVoxel)
        {
            modified = true;

            VoxelStructurePlacer.Enqueue(chunkPos,(x,y,z,newVoxel));
            queueModified = true;
            CompletelyEmpty = false;
        }
        public void GenerateVisibility(bool alreadyPopulatedPropogation = false)
        {
            if (CompletelyEmpty)
            {
                // Initialize visibility array
                for (int i = 0; i < 6; i++)
                {
                    facesVisibleAtAll[i] = true;
                    for (int j = 0; j < 6; j++)
                        sidesVisible[i, j] = true;
                }

                return;
            }

            Queue<(int x, int y, int z)> internalProp = new Queue<(int x, int y, int z)>();

            bool[,,] visited = new bool[Size, Size, Size];

            // Initialize visibility array
            for (int i = 0; i < 6; i++)
            {
                facesVisibleAtAll[i] = true;
                for (int j = 0; j < 6; j++)
                    sidesVisible[i, j] = false;
            }

            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        if (!alreadyPopulatedPropogation)
                        {
                            visibilityPropogation[x, y, z] = voxels[x + Size * (y + Size * z)] == 0 || Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].isTransparent;
                        }

                        if (!visibilityPropogation[x, y, z]) continue;

                        bool[] touchedByFlood = new bool[6];

                        visited[x,y,z] = true;

                        internalProp.Clear();
                        // Check neighboring positions and queue valid ones
                        for (int i = 0; i < 6; i++)
                        {
                            (int x, int y, int z) p = (positionChecks[i].x + x, positionChecks[i].y + y, positionChecks[i].z + z);

                            // Check boundaries
                            if (IsOutOfBounds(p))
                            {
                                touchedByFlood[i] = true; // Mark that we touched an edge
                                continue;
                            }

                            // If it's not a solid voxel, add to internal propagation queue
                            if (voxels[p.x + Size * (p.y + Size * p.z)] == 0 || Voxel.voxelTypes[voxels[p.x + Size * (p.y + Size * p.z)]].isTransparent)
                            {
                                internalProp.Enqueue(p);
                            }
                            if (touchedByFlood[0]&& touchedByFlood[1]&& touchedByFlood[2]&& touchedByFlood[3]&& touchedByFlood[4]&& touchedByFlood[5]) break;
                        }

                        // Process the internal propagation queue
                        while (internalProp.Count > 0)
                        {
                            (int x, int y, int z) pos = internalProp.Dequeue();

                            if (visited[pos.x,pos.y,pos.z]) continue;

                            visited[pos.x, pos.y, pos.z] = true;

                            for (int i = 0; i < 6; i++)
                            {
                                (int x, int y, int z) p = (positionChecks[i].x + pos.x, positionChecks[i].y + pos.y, positionChecks[i].z + pos.z);

                                if (IsOutOfBounds(p))
                                {
                                    touchedByFlood[i] = true;
                                    continue;
                                }

                                if (voxels[p.x + Size * (p.y + Size * p.z)] == 0 || Voxel.voxelTypes[voxels[p.x + Size * (p.y + Size * p.z)]].isTransparent)
                                {
                                    internalProp.Enqueue(p);
                                }
                            }
                            if (touchedByFlood[0] && touchedByFlood[1] && touchedByFlood[2] && touchedByFlood[3] && touchedByFlood[4] && touchedByFlood[5]) break;
                        }
                        // Update sidesVisible if any edge was touched
                        for (int i = 0; i < 6; i++)
                        {
                            if (touchedByFlood[i])
                            {
                                facesVisibleAtAll[i] = true;
                                for (int j = 0; j < 6; j++)
                                {
                                    if (touchedByFlood[j])
                                    {
                                        sidesVisible[i, j] = true;
                                        sidesVisible[j, i] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Helper method to check if a position is out of bounds
        private bool IsOutOfBounds((int x, int y, int z) pos)
        {
            return pos.x < 0 || pos.x >= Size || pos.y < 0 || pos.y >= Size || pos.z < 0 || pos.z >= Size;
        }
        public bool TestVisibility(int sideFrom, int sideTo)
        {
            if (sideFrom < 0 || sideTo < 0) return true;
            if (sideFrom == sideTo) return false;
            return sidesVisible[sideFrom, sideTo];
        }
        public static int FindSide(Vector3 relativePosition)
        {
            if (relativePosition.X < 0) return 0;
            if (relativePosition.X >= Size) return 1;
            if (relativePosition.Y < 0) return 2;
            if (relativePosition.Y >= Size) return 3;
            if (relativePosition.Z < 0) return 4;
            if (relativePosition.Z >= Size) return 5;
            return -1;
        }
        public static int FindOppositeSide(Vector3 relativePosition)
        {
            if (relativePosition.X < 0) return 1;
            if (relativePosition.X >= Size) return 0;
            if (relativePosition.Y < 0) return 3;
            if (relativePosition.Y >= Size) return 2;
            if (relativePosition.Z < 0) return 5;
            if (relativePosition.Z >= Size) return 4;
            return -1;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VoxelVertex : IVertexType
    {
        public Vector3 Position;

        public Color Color;

        public Vector3 Normal;

        public Vector2 TextureCoordinate;

        public static readonly VertexDeclaration VertexDeclaration;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        //
        // Summary:
        //     Creates an instance of Microsoft.Xna.Framework.Graphics.VertexPositionColorTexture.
        //
        //
        // Parameters:
        //   position:
        //     Position of the vertex.
        //
        //   color:
        //     Color of the vertex.
        //
        //   normal:
        //     The vertex normal.
        //
        //   textureCoordinate:
        //     Texture coordinate of the vertex.
        public VoxelVertex(Vector3 position, Color color, Vector3 normal, Vector2 textureCoordinate)
        {
            Position = position;
            Color = color;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
        }

        public override int GetHashCode()
        {
            return (((((Position.GetHashCode() * 397) ^ Color.GetHashCode()) * 397) ^ Normal.GetHashCode()) * 397) ^ TextureCoordinate.GetHashCode();
        }

        public override string ToString()
        {
            string[] obj = new string[9] { "{{Position:", null, null, null, null, null, null, null, null };
            Vector3 position = Position;
            obj[1] = position.ToString();
            obj[2] = " Color:";
            Color color = Color;
            obj[3] = color.ToString();
            obj[4] = " Normal:";
            position = Normal;
            obj[5] = position.ToString();
            obj[6] = " TextureCoordinate:";
            Vector2 textureCoordinate = TextureCoordinate;
            obj[7] = textureCoordinate.ToString();
            obj[8] = "}}";
            return string.Concat(obj);
        }

        //
        // Summary:
        //     Returns a value that indicates whether two Microsoft.Xna.Framework.Graphics.VertexPositionColorNormalTexture
        //     are equal
        //
        // Parameters:
        //   left:
        //     The object on the left of the equality operator.
        //
        //   right:
        //     The object on the right of the equality operator.
        //
        // Returns:
        //     true if the objects are the same; false otherwise.
        public static bool operator ==(VoxelVertex left, VoxelVertex right)
        {
            if (left.Position == right.Position && left.Color == right.Color && left.Normal == right.Normal)
            {
                return left.TextureCoordinate == right.TextureCoordinate;
            }

            return false;
        }

        //
        // Summary:
        //     Returns a value that indicates whether two Microsoft.Xna.Framework.Graphics.VertexPositionColorNormalTexture
        //     are different
        //
        // Parameters:
        //   left:
        //     The object on the left of the inequality operator.
        //
        //   right:
        //     The object on the right of the inequality operator.
        //
        // Returns:
        //     true if the objects are different; false otherwise.
        public static bool operator !=(VoxelVertex left, VoxelVertex right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return this == (VoxelVertex)obj;
        }

        static VoxelVertex()
        {
            VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Short4, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0), new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
        }
    }
}
