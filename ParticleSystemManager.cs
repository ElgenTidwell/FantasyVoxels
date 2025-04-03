using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels
{
	public class FixedList<T>
	{
		private readonly T[] _items;
		private readonly Stack<int> _freeSlots;
		private readonly HashSet<int> _occupiedSlots;
		private int _count;
		private bool _dirty = true;
		private T[] _valuecache;

		public int Count => _count;
		public int Capacity => _items.Length;

		public FixedList(int size)
		{
			_items = new T[size];
			_freeSlots = new Stack<int>(Enumerable.Range(0, size));
			_occupiedSlots = new HashSet<int>();
		}
		public int Add(T item)
		{
			if (_freeSlots.Count == 0)
            {
                _occupiedSlots.Remove(0);
                _freeSlots.Push(0);
            }
			int slot = _freeSlots.Pop();
			_items[slot] = item;
			_occupiedSlots.Add(slot);
			_dirty = true;
			_count++;
			return slot;
		}
		public void Remove(int index)
		{
			if (!_occupiedSlots.Contains(index)) return;

			_items[index] = default;
			_occupiedSlots.Remove(index);
			_freeSlots.Push(index);
			_dirty = true;
			_count--;
		}
		public void Remove(T item)
		{
			Remove(Array.FindIndex(_items, i => EqualityComparer<T>.Default.Equals(i, item)));
		}
		public Span<T> GetValues()
		{
			if (_dirty)
			{
				var val = _occupiedSlots.Select(i => _items[i]).ToArray();
				_valuecache = val;
			}
			return _valuecache;
		}
		public int FindIndex(Predicate<T> match)
		{
			return (Array.FindIndex(_items, match));
		}
		public T this[int id]
		{
			get { return _items[id]; }
			set
			{
				_items[id] = value;
				if (value == null) Remove(id);
			}
		}
	}
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
            ParticleAtlas
        }
        public TextureProvider textureProvider;
        public int textureIndex, textureFinalIndex = -1;
        public float lifetime;
        public float gravity;
        public float curlife;
        public float uvScale;
        public float tint = 0.0f;
        public float scale;

        public ParticleSystem(int particles, TextureProvider provider, int texIndex, Vector3Double origin, Vector3 velocity, float lifetime, float gravity, Vector3 randomPosScalar, Vector3 randomVelScalar, int endTexture = -1, float uvScale = 1, float particleScale = 0.12f)
        {
            this.particleCount = particles;
            this.particlePositions = new Vector3Double[particles];
            this.particleVelocities = new Vector3[particles];
            for (int i = 0; i < particles; i++)
            {
                this.particlePositions[i] = origin + new Vector3(Random.Shared.NextSingle()*2-1, Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle() * 2 - 1)*randomPosScalar;
                this.particleVelocities[i] = velocity + new Vector3(Random.Shared.NextSingle()*2-1, Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle() * 2 - 1)* randomVelScalar;
            }
            this.textureProvider = provider;
            this.textureIndex = texIndex;
            this.textureFinalIndex = endTexture<0?texIndex: endTexture;
            this.gravity = gravity;
            this.lifetime = lifetime;
            this.uvScale = uvScale;
            this.scale = particleScale;
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
        private static FixedList<ParticleSystem> systems = new FixedList<ParticleSystem>(15);

        public static void AddSystem(ParticleSystem system)
        {
            systems.Add(system);
        }
        public static void UpdateSystems()
        {
            foreach(var system in systems.GetValues())
            {
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

			foreach (var system in systems.GetValues())
			{
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
                    case ParticleSystem.TextureProvider.ParticleAtlas:

                        shader.Parameters["mainTexture"].SetValue(MGame.Instance.particleAtlas);
                        shader.Parameters["atlasSize"].SetValue(MGame.ParticleAtlasSize / 16);

                        break;
                }
                shader.Parameters["texIndex"].SetValue((int)float.Round(float.Lerp(system.textureIndex, system.textureFinalIndex, (system.curlife / system.lifetime))));
                if(system.textureProvider == ParticleSystem.TextureProvider.BlockAtlas||
                    system.textureProvider == ParticleSystem.TextureProvider.ItemAtlas) shader.Parameters["uvScale"].SetValue(0.25f);
                else shader.Parameters["uvScale"].SetValue(system.uvScale);
                shader.Parameters["tint"].SetValue(ourLight);
                shader.Parameters["blocklightTint"].SetValue(voxelData.blockLight / 255f);

                for (int p = 0; p < system.particleCount; p++)
                {
                    Matrix m = Matrix.CreateScale(system.scale) *Matrix.CreateRotationY(MathHelper.ToRadians(90f))*Matrix.CreateWorld((Vector3)system.particlePositions[p],MGame.Instance.cameraForward,Vector3.Up);

                    shader.Parameters["World"].SetValue(m*MGame.Instance.world);
                    if (system.textureProvider == ParticleSystem.TextureProvider.BlockAtlas ||
                    system.textureProvider == ParticleSystem.TextureProvider.ItemAtlas) shader.Parameters["uvOffset"].SetValue(new Vector2(p % 4, p / 4) / 4f);
                    else shader.Parameters["uvOffset"].SetValue(Vector2.Zero);

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
