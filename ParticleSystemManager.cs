using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels
{
    public class ParticleSystem
    {
        public int particleCount;
        public Vector3Double[] particlePositions;
        public Vector3[] particleVelocities;
        public float[] particleRotations;
        public Vector3 origin;
        public enum TextureProvider
        {
            BlockAtlas,
            ItemAtlas,
            CustomTexture
        }
        public TextureProvider textureProvider;
        public int textureIndex;
        public float lifetime;
        public float gravity;
        public float curlife;
        public float tint = 0.0f;

        public ParticleSystem(int particles, TextureProvider provider, int texIndex, Vector3Double origin, Vector3 velocity, float lifetime, float gravity, Vector3 randomPosScalar, Vector3 randomVelScalar)
        {
            this.particleCount = particles;
            this.particlePositions = new Vector3Double[particles];
            this.particleVelocities = new Vector3[particles];
            this.particleRotations = new float[particles];
            for (int i = 0; i < particles; i++)
            {
                this.particlePositions[i] = origin + new Vector3(Random.Shared.NextSingle()*2-1, Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle() * 2 - 1)*randomPosScalar;
                this.particleVelocities[i] = velocity + new Vector3(Random.Shared.NextSingle()*2-1, Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle() * 2 - 1)* randomVelScalar;
                this.particleRotations[i] = Random.Shared.NextSingle();
            }
            this.textureProvider = provider;
            this.textureIndex = texIndex;
            this.gravity = gravity;
            this.lifetime = lifetime;
            this.origin = (Vector3)origin;
        }
    }
    public static class ParticleSystemManager
    {
        private static VertexPositionTexture[] vertices =
        [
            new(new Vector3(-0.5f,-0.5f, 0.5f), new Vector2(0, 1)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1, 1)),
            new(new Vector3(-0.5f, 0.5f,-0.5f), new Vector2(1, 0)),
            new(new Vector3(-0.5f,-0.5f, 0.5f), new Vector2(0, 1)),
            new(new Vector3(-0.5f, 0.5f,-0.5f), new Vector2(1, 0)),
            new(new Vector3(-0.5f,-0.5f,-0.5f), new Vector2(0, 0)),
        ];
        private static List<ParticleSystem> systems = new List<ParticleSystem>();

        public static void AddSystem(ParticleSystem system)
        {
            systems.Add(system);
        }
        public static void UpdateSystems()
        {
            for(int i = systems.Count-1; i >= 0; i--)
            {
                ParticleSystem system = systems[i];

                system.curlife += MGame.dt;

                if(system.lifetime > 0 && system.curlife > system.lifetime) { systems.Remove(system); continue; }

                int cx = (int)float.Floor(system.origin.X/Chunk.Size);
                int cy = (int)float.Floor(system.origin.Y/Chunk.Size);
                int cz = (int)float.Floor(system.origin.Z/Chunk.Size);
                long rootChunk = MGame.CCPos((cx,cy,cz));
                bool rootChunkExists = MGame.Instance.loadedChunks.ContainsKey(rootChunk);

                for (int p = 0; p < system.particleCount; p++)
                {
                    system.particlePositions[p] += system.particleVelocities[p] * MGame.dt;

                    system.particleVelocities[p].X -= system.particleVelocities[p].X * MGame.dt;
                    system.particleVelocities[p].Z -= system.particleVelocities[p].Z * MGame.dt;

                    system.particleVelocities[p].Y -= system.gravity * MGame.dt;

                    system.particleRotations[p] += system.particleVelocities[p].LengthSquared() * MGame.dt * ((p * 0.5f + 1)*(p%2==0?-1:1))*0.1f;

                    int x = (int)double.Floor(system.particlePositions[p].X) - cx * Chunk.Size;
                    int y = (int)double.Floor(system.particlePositions[p].Y) - cy * Chunk.Size;
                    int z = (int)double.Floor(system.particlePositions[p].Z) - cz * Chunk.Size;

                    bool collision = rootChunkExists ? 
                        Chunk.IsOutOfBounds((x,y,z)) ? (CollisionDetector.IsSolidTile((int)double.Floor(system.particlePositions[p].X), (int)double.Floor(system.particlePositions[p].Y), (int)double.Floor(system.particlePositions[p].Z)))
                        : MGame.Instance.loadedChunks[rootChunk].IsSolid((x, y, z))
                        : false;

                    //if (CollisionDetector.IsSolidTile((int)system.particlePositions[p].X, (int)system.particlePositions[p].Y, (int)system.particlePositions[p].Z))
                    if(collision)
                    {
                        system.particleVelocities[p].Y *= -0.2f;
                        system.particleVelocities[p].X *= 0.2f;
                        system.particleVelocities[p].Z *= 0.2f;
                    }
                }
            }
        }
        public static void RenderSystems()
        {
            var shader = MGame.Instance.GetParticleShader();

            shader.Parameters["View"].SetValue(MGame.Instance.view);
            shader.Parameters["Projection"].SetValue(MGame.Instance.projection);

            for (int i = systems.Count - 1; i >= 0; i--)
            {
                ParticleSystem system = systems[i];

                MGame.Instance.GrabVoxelData(system.origin + Vector3.Up * 0.1f, out var voxelData);

                float ourLight = (voxelData.skyLight / 255f) * MGame.Instance.daylightPercentage;

                switch (system.textureProvider)
                {
                    case ParticleSystem.TextureProvider.BlockAtlas:

                        shader.Parameters["mainTexture"].SetValue(MGame.Instance.colors);
                        shader.Parameters["atlasSize"].SetValue(MGame.AtlasSize/16);

                        break;
                    case ParticleSystem.TextureProvider.ItemAtlas:

                        shader.Parameters["mainTexture"].SetValue(MGame.Instance.items);
                        shader.Parameters["atlasSize"].SetValue(MGame.ItemAtlasSize / 16);

                        break;
                }
                shader.Parameters["texIndex"].SetValue(system.textureIndex);
                shader.Parameters["uvScale"].SetValue(0.25f);
                shader.Parameters["tint"].SetValue(ourLight);
                shader.Parameters["blocklightTint"].SetValue(voxelData.blockLight / 255f);

                for (int p = 0; p < system.particleCount; p++)
                {
                    Matrix m = Matrix.CreateScale(0.125f) * Matrix.CreateRotationX(system.particleRotations[p])*Matrix.CreateRotationY(MathHelper.ToRadians(90f))*Matrix.CreateWorld((Vector3)system.particlePositions[p],MGame.Instance.cameraForward,Vector3.Up);

                    shader.Parameters["World"].SetValue(m*MGame.Instance.world);
                    shader.Parameters["uvOffset"].SetValue(new Vector2(p%4,p/4)/4f);

                    foreach (var pass in shader.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        MGame.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,vertices.Length/3);
                    }
                }
            }
        }
    }
}
