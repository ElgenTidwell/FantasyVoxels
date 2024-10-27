using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

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
            },shaderEffect:2),
            //Dirt
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble()*0.1f+1);
            }),
            //Water
            new Voxel(((int x, int y, int z) pos) =>
            {
                return MathF.Abs(MathF.Sin((IcariaNoise.GradientNoise3D(pos.x * 0.1f, pos.y * 0.1f, pos.z * 0.1f))))*0.1f + 0.9f + (float)(Random.Shared.NextDouble() * 0.05f);
            },true,1,true),
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
            },true, 2),
            //Leaves
            new Voxel(((int x, int y, int z) pos) =>
            {
                return (float)(Random.Shared.NextDouble() * 0.2f + 0.8f);
            }, shaderEffect: 2),
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
        ];

        public Func<(int x, int y, int z),float> GetShade;
        public bool ignoreCollision,isTransparent;
        public int shaderEffect;

        public Voxel(Func<(int x, int y, int z), float> GetShade, bool ignoreCollision = false, int shaderEffect = 0, bool isTransparent = false)
        {
            this.GetShade = GetShade;
            this.ignoreCollision = ignoreCollision;
            this.isTransparent = isTransparent;
            this.shaderEffect = shaderEffect;
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
        public int[] voxels = new int[Size*Size*Size];
        public VoxelData[] voxeldata = new VoxelData[Size*Size*Size];

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
        static (int x, int y, int z)[] positionChecks =
        {
            (1,0,0),
            (-1,0,0),
            (0,1,0),
            (0,-1,0),
            (0,0,1),
            (0,0,-1),
        };
        public static VertexBuffer chunkBuffer;
        public VertexBuffer[] chunkVertexBuffers;

        public bool[,] sidesVisible = new bool[6,6];
        public bool[] facesVisibleAtAll = new bool[6];
        public bool generated = false;
        public bool[] meshUpdated = new bool[4];
        public int MaxY;
        Random tRandom;
        public Chunk()
        {
            chunkVertexBuffers = new VertexBuffer[5];
            tRandom = new Random(MGame.Instance.seed + 4);
        }
        public static float GetOctaveNoise(float x, float z, float frequency, int octaveCount, float persistence, float lacunarity, int seedOffset = 0)
        {
            float totalNoise = 0;
            float amplitude = 1;
            float maxAmplitude = 0; // Used to normalize the result
            for (int i = 0; i < octaveCount; i++)
            {
                totalNoise += IcariaNoise.GradientNoise(x * frequency, z * frequency, MGame.Instance.seed+seedOffset) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= persistence;    // Decrease amplitude for each octave
                frequency *= lacunarity;     // Increase frequency for each octave
            }
            return totalNoise;
        }
        public static float GetOctaveNoise3D(float x, float y, float z, float frequency, int octaveCount, float persistence, float lacunarity, int seedOffset = 0)
        {
            float totalNoise = 0;
            float amplitude = 1;
            float maxAmplitude = 0; // Used to normalize the result
            for (int i = 0; i < octaveCount; i++)
            {
                totalNoise += IcariaNoise.GradientNoise3D(x * frequency, y*frequency, z * frequency, MGame.Instance.seed + seedOffset) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= persistence;    // Decrease amplitude for each octave
                frequency *= lacunarity;     // Increase frequency for each octave
            }
            return totalNoise;
        }
        public static int GetTerrainHeight(float samplex, float samplez)
        {
            float valleyNoise = GetOctaveNoise(samplex, samplez, 0.00002f, 5, 1.02f, 3.0f,14);
            if (valleyNoise < -0.0f) valleyNoise = 1 - MathF.Pow(1 - valleyNoise, 4);
            else valleyNoise = MathF.Min(valleyNoise*100,1);

            // Base mountainous terrain layer with large-scale ridges
            float elevation = GetOctaveNoise(samplex, samplez, 0.000001f, 6, 1.2f, 2.7f) * 240;

            float baseMountainNoise = GetOctaveNoise(samplex, samplez, 0.00004f, 6, 1.2f, 2.7f) * 80 * valleyNoise + elevation;

            // Jagged, rugged noise for pointy peaks
            float ruggedNoise = IcariaNoise.GradientNoise(samplex * 0.006f, samplez * 0.006f, MGame.Instance.seed + 10) * 15;

            // Cliff and steep drop noise layer
            float cliffNoise = MathF.Pow(MathF.Abs(IcariaNoise.GradientNoise(samplex * 0.00001f, samplez * 0.00001f, MGame.Instance.seed - 5)),10) * 20;

            // Create sharp ridges and pointy features
            float peakNoise = GetOctaveNoise(samplex, samplez, 0.0002f, 3, 1.5f, 3.0f) * 30;

            // Combine layers to form dramatic terrain
            int terrainHeight = (int)(baseMountainNoise + ruggedNoise + cliffNoise + peakNoise);

            // Add additional points to make the terrain more interesting near peaks
            if (terrainHeight > 80)
            {
                terrainHeight = (int)(terrainHeight *(1+MathF.Min(((terrainHeight-80)/2000f),1)*(IcariaNoise.GradientNoise(samplex*0.001f, samplez * 0.001f,MGame.Instance.seed-10)+1)*0.5f));  // Raise terrain further to create peaks
            }

            return terrainHeight;
        }
        public static int GetVoxel(float x, float y, float z, int terrainHeight)
        {
            int voxel = 0;
            // Main terrain voxel assignment with 3D noise layers
            voxel = y <= terrainHeight ? y < 5 ? 4 : 2 : 0;

            // Water voxel assignment for regions below sea level
            if (y < 5 && y >= terrainHeight)
                voxel = 3;

            // 3D noise-based erosion layer with varying frequencies
            if ((GetOctaveNoise3D(x, y*2f, z, 0.008f, 2, 0.5f, 1.4f)) > MathF.Abs(GetOctaveNoise3D(x, y * 2f, z, 0.001f, 1, 0, 0))+0.6f)
            {
                voxel = 0;
            }

            if (y < terrainHeight - 30 - IcariaNoise.GradientNoise(x * 0.9f, z * 0.9f, MGame.Instance.seed - 24) * 20 && voxel != 0)
                voxel = 9;

            return voxel;
        }
        public void Generate()
        {
            CompletelyEmpty = true;
            Array.Fill(voxels,0);
            Array.Fill(voxeldata,new VoxelData());
            HashSet<(int x, int y, int z)> propogate = new HashSet<(int x, int y, int z)>();
            int elementsCount = 0;
            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    int samplex = x + chunkPos.x * Size, samplez = z + chunkPos.z * Size;

                    //float ocean = (IcariaNoise.GradientNoise(samplex * 0.001f, samplez * 0.001f, 1 + MGame.Instance.seed) +1f);
                    float ocean = 0f;
                    int terrainHeight = (int)(GetTerrainHeight(samplex/2f, samplez/2f)*(1-ocean) + (ocean*2));

                    int shortGrassHeight = tRandom.Next(1,4);
                    float shortGrassChance = (float)(tRandom.NextDouble() * 2 - 1);

                    int sunlight = 255;

                    for (int y = 0; y < Size; y++)
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
                            propogate.Add((x, y, z));
                        }
                        elementsCount++;
                    }
                    //"Dirt Pass"
                    bool grassed = false;
                    for (int y = MaxY; y >= 0; y--)
                    {
                        int sampley = y + chunkPos.y * Size;
                        float patchyRandom = IcariaNoise.GradientNoise(samplex * 0.1f, sampley * 0.1f, MGame.Instance.seed - 10);
                        grassed = GetVoxel(samplex, sampley + 2, samplez, terrainHeight) == 2 || sampley<terrainHeight-1 || sampley > (patchyRandom * 100+410);
                        if (voxels[x + Size * (y + Size * z)] == 2 && (!grassed || MGame.Instance.worldRandom.Next(-100, 100) == 10))
                        {
                            grassed = true;

                            voxels[x + Size * (y + Size * z)] = sampley < 10? 4 : 1;
                        }

                        if (voxels[x + Size * (y + Size * z)] == 4 && sampley < patchyRandom*40-45)
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
                    }
                }
            }

            if (!CheckQueue())
            {
                GenerateVisibility(propogate);
                Remesh(false);
            }
            generated = true;
        }

        private VertexPositionColorNormalTexture[] MeshLOD(int lod)
        {
            List<VertexPositionColorNormalTexture> verts = new List<VertexPositionColorNormalTexture>();

            int scale = lod!=4? (int)MathF.Pow(2, lod) :1;

            for (int x = 0; x < Size; x += scale)
            {
                for (int z = 0; z < Size; z += scale)
                {
                    int samplex = x + chunkPos.x * Size, samplez = z + chunkPos.z * Size;

                    int terrainHeight = (int)(GetTerrainHeight(samplex / 2f, samplez / 2f));
                    for (int y = 0; y < Size; y += scale)
                    {
                        int sampley = y + chunkPos.y * Size;

                        if (IsOutOfBounds((x, y, z))) continue;

                        if (voxels[x + Size * (y + Size * z)] == 0) continue;
                        if (Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].isTransparent && lod != 4) continue;
                        if (!Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].isTransparent && lod == 4) continue;

                        for (int p = 0; p < positionChecks.Length; p++)
                        {
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
                                    UVCoords = new Vector2((MathF.Abs(samplez) + 1) % 16, (MathF.Abs(sampley) + 1) % 16);
                                    break;
                                case 1:
                                    UVCoords = new Vector2((MathF.Abs(samplez) + 1) % 16, (MathF.Abs(samplex) + 1) % 16);
                                    break;
                                case 2:
                                    UVCoords = new Vector2((MathF.Abs(samplex) + 1) % 16, (MathF.Abs(sampley) + 1) % 16);
                                    break;
                            }
                            UVCoords += new Vector2((voxels[x + Size * (y + Size * z)] - 1) % 16, (voxels[x + Size * (y + Size * z)] - 1) / 16) * 16 + Vector2.One * 0.1f;

                            UVCoords /= 256f;

                            int vox = 0;

                            if (IsOutOfBounds(checkPos))
                            {
                                int grabbed = MGame.Instance.GrabVoxel(new Vector3(samplex + positionChecks[p].x * (scale), sampley + positionChecks[p].y * (scale), samplez + positionChecks[p].z * (scale)));

                                if (grabbed == -1) vox = GetVoxel(samplex + positionChecks[p].x * (scale), sampley + positionChecks[p].y * (scale), samplez + positionChecks[p].z * (scale), terrainHeight - 2);
                                else vox = grabbed;
                            }
                            else
                            {
                                vox = voxels[checkPos.x + Size * (checkPos.y + Size * checkPos.z)];
                            }
                            if (vox == 0 || (Voxel.voxelTypes[vox].isTransparent && vox != voxels[x + Size * (y + Size * z)]))
                            {
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0]*(scale) + new Vector3(x, y, z), color, normal, UVCoords));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 1]*(scale) + new Vector3(x, y, z), color, normal, UVCoords));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2]*(scale) + new Vector3(x, y, z), color, normal, UVCoords));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 0]*(scale) + new Vector3(x, y, z), color, normal, UVCoords));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 2]*(scale) + new Vector3(x, y, z), color, normal, UVCoords));
                                verts.Add(new VertexPositionColorNormalTexture(vertsPerCheck[p * 4 + 3]*(scale) + new Vector3(x, y, z), color, normal, UVCoords));
                            }
                        }
                    }
                }
            }

            if(lod != 4) meshUpdated[lod] = true;

            if (verts.Count == 0) return verts.ToArray();
            var temp = new VertexBuffer(MGame.Instance.GraphicsDevice, typeof(VertexPositionColorNormalTexture), verts.Count, BufferUsage.WriteOnly);
            temp.SetData(verts.ToArray());

            chunkVertexBuffers[lod] = temp;

            return verts.ToArray();
        }

        public int GetLOD()
        {
            return (int)MathF.Floor(MathF.Min(Vector3.Distance(MGame.Instance.cameraPosition, new Vector3(chunkPos.x + 0.5f, chunkPos.y + 0.5f, chunkPos.z + 0.5f) * Size) / (Size * 16),3));
        }

        public void Remesh(bool all = false)
        {
            if (CompletelyEmpty) return;

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
            List<Chunk> remeshNeighbors = new List<Chunk>();
            while (queuedVoxels.Count > 0)
            {
                if (!queuedVoxels.TryDequeue(out var p)) continue;

                if (p.x < 0 || p.y < 0 || p.z < 0 || p.x >= Size || p.y >= Size || p.z >= Size) continue;
                CompletelyEmpty = false;
                voxels[p.x + Size * (p.y + Size * p.z)] = p.vox;
                voxeldata[p.x + Size * (p.y + Size * p.z)].shade = (int)(240 * Voxel.voxelTypes[voxels[p.x + Size * (p.y + Size * p.z)]].GetShade((p.x,p.y,p.z)));
                MaxY = Math.Max(MaxY, p.y);
                Chunk neighbor;
                if (p.x == 0        && MGame.Instance.loadedChunks.TryGetValue((chunkPos.x - 1, chunkPos.y, chunkPos.z), out neighbor))
                {
                    if(!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }
                if (p.x == Size - 1 && MGame.Instance.loadedChunks.TryGetValue((chunkPos.x + 1, chunkPos.y, chunkPos.z), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }

                if (p.y == 0        && MGame.Instance.loadedChunks.TryGetValue((chunkPos.x, chunkPos.y - 1, chunkPos.z), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }
                if (p.y == Size - 1 && MGame.Instance.loadedChunks.TryGetValue((chunkPos.x, chunkPos.y + 1, chunkPos.z), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }

                if (p.z == 0        && MGame.Instance.loadedChunks.TryGetValue((chunkPos.x, chunkPos.y, chunkPos.z - 1), out neighbor))
                {
                    if (!remeshNeighbors.Contains(neighbor)) remeshNeighbors.Add(neighbor);
                }
                if (p.z == Size - 1 && MGame.Instance.loadedChunks.TryGetValue((chunkPos.x, chunkPos.y, chunkPos.z + 1), out neighbor))
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

            voxels[x + Size * (y + Size * z)] = newVoxel;
            voxeldata[x + Size * (y + Size * z)].shade = (int)(240 * Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].GetShade((x, y, z)));

            MaxY = Math.Max(MaxY, y);
            CompletelyEmpty = false;
            GenerateVisibility();
            meshUpdated = new bool[4] { false, false, false, false };
            Remesh();
        }
        public void ModifyQueue(int x, int y, int z, int newVoxel)
        {
            VoxelStructurePlacer.Enqueue(chunkPos,(x,y,z,newVoxel));
            queueModified = true;
            CompletelyEmpty = false;
        }
        public void GenerateVisibility(HashSet<(int x, int y, int z)> propogate = null)
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
            if(propogate == null)
            {
                propogate = new HashSet<(int x, int y, int z)>();

                for (int x = 0; x < Size; x++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        for (int y = 0; y < Size; y++)
                        {
                            if (voxels[x + Size * (y + Size * z)] == 0)
                            {
                                propogate.Add((x, y, z));
                            }
                        }
                    }
                }
            }

            HashSet<(int x, int y, int z)> visited = new HashSet<(int x, int y, int z)>();

            Queue<(int x, int y, int z)> internalProp = new Queue<(int x, int y, int z)>();


            // Initialize visibility array
            for (int i = 0; i < 6; i++)
            {
                facesVisibleAtAll[i] = true;
                for (int j = 0; j < 6; j++)
                    sidesVisible[i, j] = false;
            }

            // Flood fill
            foreach (var rootPos in propogate)
            {
                bool[] touchedByFlood = new bool[6];

                visited.Add(rootPos);
                internalProp.Clear();
                // Check neighboring positions and queue valid ones
                for (int i = 0; i < 6; i++)
                {
                    (int x, int y, int z) p = (positionChecks[i].x + rootPos.x, positionChecks[i].y + rootPos.y, positionChecks[i].z + rootPos.z);

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

                    if (visited.Contains(pos)) continue;

                    visited.Add(pos);

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
}
