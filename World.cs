using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IslandGame
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
            },true,1),
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
        public bool ignoreCollision;
        public int shaderEffect;

        public Voxel(Func<(int x, int y, int z), float> GetShade, bool ignoreCollision = false, int shaderEffect = 0)
        {
            this.GetShade = GetShade;
            this.ignoreCollision = ignoreCollision;
            this.shaderEffect = shaderEffect;
        }
    }
    public class Chunk
    {
        public const int Size = 32;
        public (int x, int y, int z) chunkPos;
        public int[] voxels = new int[Size*Size*Size];

        public bool queueModified,queueInWorks;

        private Color[] voxelsAC = new Color[Size*Size*Size];
        public Texture3D voxelTexture;
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
            0, 2, 1, //face front
	        0, 3, 2,
            2, 3, 4, //face top
	        2, 4, 5,
            1, 2, 5, //face right
	        1, 5, 6,
            0, 7, 4, //face left
	        0, 4, 3,
            5, 4, 7, //face back
	        5, 7, 6,
            0, 6, 7, //face bottom
	        0, 1, 6
        ];
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
        public static IndexBuffer indexBuffer;

        public bool[,] sidesVisible = new bool[6,6];
        public bool generated = false;
        public int MaxY;
        Random tRandom;
        public Chunk()
        {
            voxelTexture = new Texture3D(MGame.Instance.GraphicsDevice,Size,Size,Size,false,SurfaceFormat.Color);

            tRandom = new Random(MGame.Instance.seed + 4);
        }
        float GetOctaveNoise(float x, float z, float frequency, int octaveCount, float persistence, float lacunarity, int seedOffset = 0)
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
        float GetOctaveNoise3D(float x, float y, float z, float frequency, int octaveCount, float persistence, float lacunarity, int seedOffset = 0)
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
        int GetTerrainHeight(float samplex, float samplez)
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
        public int GetVoxel(float x, float y, float z, int terrainHeight)
        {
            int voxel = 0;
            // Main terrain voxel assignment with 3D noise layers
            voxel = y <= terrainHeight ? 2 : 0;

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
            Array.Fill(voxelsAC,Color.Black);
            HashSet<(int x, int y, int z)> propogate = new HashSet<(int x, int y, int z)>();
            int elementsCount = 0;
            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    int samplex = x + chunkPos.x * Size, samplez = z + chunkPos.z * Size;

                    //float ocean = (IcariaNoise.GradientNoise(samplex * 0.001f, samplez * 0.001f, 1 + MGame.Instance.seed) +1f);
                    float ocean = 0f;
                    int terrainHeight = (int)(GetTerrainHeight(samplex/2f, samplez/2f)*(1-ocean) + (ocean*2));;

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
                        else
                        {
                            propogate.Add((x, y, z));
                        }


                        voxelsAC[x + Size * (y + Size * z)] = new Color(
                            voxels[x + Size * (y + Size * z)], 
                            (int)((MathF.Min(MathF.Max(terrainHeight - 10,0), 100)*0.2f + 180) * Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].GetShade((samplex, sampley, samplez))),
                            Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].shaderEffect, 
                            255);
                        elementsCount++;
                    }
                    //"Dirt Pass"
                    bool grassed = false;
                    for (int y = MaxY; y >= 0; y--)
                    {
                        int sampley = y + chunkPos.y * Size;

                        grassed = GetVoxel(samplex, sampley + 2, samplez, terrainHeight) == 2 || sampley<terrainHeight-1 || sampley > (IcariaNoise.GradientNoise(samplex*0.1f,sampley * 0.1f,MGame.Instance.seed-10)*100+410);
                        if (voxels[x + Size * (y + Size * z)] == 2 && (!grassed || MGame.Instance.worldRandom.Next(-100, 100) == 10))
                        {
                            grassed = true;
                            voxels[x + Size * (y + Size * z)] = sampley<10?4:1;
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

                        voxelsAC[x + Size * (y + Size * z)] = new Color(
                            voxels[x + Size * (y + Size * z)],
                            (int)((MathF.Min(MathF.Max(terrainHeight - 10, 0), 100) * 0.2f + 180) * Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].GetShade((samplex, sampley, samplez))),
                            Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].shaderEffect,
                            255);
                    }
                }
            }

            if (!CheckQueue())
            {
                voxelTexture.SetData(voxelsAC, 0, voxelsAC.Length);
                GenerateVisibility(propogate);
            }

            generated = true;
        }

        public bool CheckQueue()
        {
            queueModified = false;
            var queuedVoxels = VoxelStructurePlacer.GetQueue(chunkPos);
            if (queuedVoxels == null) return false;
            if (queuedVoxels.Count <= 1) return false;
            while (queuedVoxels.Count > 0)
            {
                if (!queuedVoxels.TryDequeue(out var p)) continue;

                if (p.x < 0 || p.y < 0 || p.z < 0 || p.x >= Size || p.y >= Size || p.z >= Size) continue;
                CompletelyEmpty = false;
                voxels[p.x + Size * (p.y + Size * p.z)] = p.vox;
                MaxY = Math.Max(MaxY, p.y);

                voxelsAC[p.x + Size * (p.y + Size * p.z)] = new Color(
                    (int)voxels[p.x + Size * (p.y + Size * p.z)],
                    (int)(250* Voxel.voxelTypes[voxels[p.x + Size * (p.y + Size * p.z)]].GetShade((p.x, p.y, p.z))),
                    Voxel.voxelTypes[voxels[p.x + Size * (p.y + Size * p.z)]].shaderEffect,
                    255);
            }
            queuedVoxels.Clear();
            GenerateVisibility();
            voxelTexture.SetData(voxelsAC, 0, voxelsAC.Length);
            return true;
        }

        public void Modify(int x, int y, int z, int newVoxel)
        {
            if (x < 0 || y < 0 || z < 0 || x >= Size || y >= Size || z >= Size) return;

            voxels[x + Size * (y + Size * z)] = newVoxel;

            voxelsAC[x + Size * (y + Size * z)] = new Color(
                (int)voxels[x + Size * (y + Size * z)],
                (int)(250 * Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].GetShade((x, y, z))),
                Voxel.voxelTypes[voxels[x + Size * (y + Size * z)]].shaderEffect,
                255);
            MaxY = Math.Max(MaxY, y);
            GenerateVisibility();
            voxelTexture.SetData(voxelsAC, 0, voxelsAC.Length);
            CompletelyEmpty = false;
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
                    for (int j = 0; j < 6; j++)
                        sidesVisible[i, j] = true;

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

            bool[] touchedByFlood = new bool[6];

            // Flood fill
            foreach (var rootPos in propogate)
            {
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
                    if (voxels[p.x + Size * (p.y + Size * p.z)] == 0)
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

                        if (voxels[p.x + Size * (p.y + Size * p.z)] == 0)
                        {
                            internalProp.Enqueue(p);
                        }
                    }
                    if (touchedByFlood[0] && touchedByFlood[1] && touchedByFlood[2] && touchedByFlood[3] && touchedByFlood[4] && touchedByFlood[5]) break;
                }
                if (touchedByFlood[0] && touchedByFlood[1] && touchedByFlood[2] && touchedByFlood[3] && touchedByFlood[4] && touchedByFlood[5]) break;
            }

            // Initialize visibility array
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    sidesVisible[i, j] = false;

            // Update sidesVisible if any edge was touched
            for (int i = 0; i < 6; i++)
            {
                if (touchedByFlood[i])
                {
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

        // Helper method to check if a position is out of bounds
        private bool IsOutOfBounds((int x, int y, int z) pos)
        {
            return pos.x < 0 || pos.x >= Size || pos.y < 0 || pos.y >= Size || pos.z < 0 || pos.z >= Size;
        }
        public bool TestVisibility(int sideFrom, int sideTo)
        {
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
