using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FantasyVoxels
{
    public struct Vector3Double
    {
        public double X, Y, Z;
        public Vector3Double(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public static implicit operator Vector3Double(Vector3 a) => new Vector3Double(a.X,a.Y,a.Z);
        public static explicit operator Vector3(Vector3Double a) => new Vector3((float)a.X, (float)a.Y, (float)a.Z);
        public static Vector3Double operator +(Vector3Double a,Vector3Double b) => new Vector3Double(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static Vector3Double operator *(double b, Vector3Double a) => new Vector3Double(a.X*b,a.Y*b,a.Z*b);
    }
    public abstract class Entity
    {
        public Vector3Double position;
        public Vector3 rotation, velocity;
        public BoundingBox bounds;
        public long parentChunk;
        public float gravity;
        public bool grounded;
        public bool swimming;
        public bool fly;
        public byte health,maxHealth;
        protected bool disallowWalkingOffEdge;

        private static VertexPositionTexture[] shadowVertices =
        [
            new(new Vector3(0, 0, 1), new Vector2(0,1)),
            new(new Vector3(1, 0, 1), new Vector2(1,1)),
            new(new Vector3(1, 0, 0), new Vector2(1,0)),
            new(new Vector3(0, 0, 0), new Vector2(0,0)),
        ];
        public struct DamageInfo
        {
            public int damage;
            public Vector3Double from;
        }

        public virtual void Die() { }

        public virtual void OnTakeDamage(DamageInfo info) 
        {
            health = (byte)int.Clamp(health - info.damage,0,maxHealth);

            if (health <= 0)
            {
                Die();
            }
        }
        public abstract void Start();
        public virtual void Update()
        {
            if(fly)
            {
                gravity = Maths.MoveTowards(gravity, 0f, MGame.dt * 18);
            }
            else
            {
                if (grounded) gravity = !swimming ? -0.6f : 0;
                else
                {
                    gravity = swimming ? Maths.MoveTowards(gravity, -2f, MGame.dt * 18) : gravity - 22 * MGame.dt;
                }
            }

            velocity.Y = grounded ? 0 : gravity;

            HandleCollisions();
        }
        public abstract void Render();
        public abstract void Destroyed();

        public abstract object CaptureCustomSaveData();
        public abstract void RestoreCustomSaveData(object data);

        public void HandleCollisions()
        {
            Vector3Double min = bounds.Min + position;
            Vector3Double max = bounds.Max + position;

            int cx = (int)Math.Floor(min.X / Chunk.Size);
            int cy = (int)Math.Floor(min.Y / Chunk.Size);
            int cz = (int)Math.Floor(min.Z / Chunk.Size);

            if (!MGame.Instance.loadedChunks.ContainsKey(MGame.CCPos((cx, cy, cz)))) return;
            if (!MGame.Instance.loadedChunks[MGame.CCPos((cx, cy, cz))].generated) return;

            cx = (int)Math.Floor(max.X / Chunk.Size);
            cy = (int)Math.Floor(max.Y / Chunk.Size);
            cz = (int)Math.Floor(max.Z / Chunk.Size);

            if (!MGame.Instance.loadedChunks.ContainsKey(MGame.CCPos((cx, cy, cz)))) return;
            if (!MGame.Instance.loadedChunks[MGame.CCPos((cx, cy, cz))].generated) return;

            min.X = bounds.Min.X * 0.9f + position.X;
            min.Z = bounds.Min.Z * 0.9f + position.Z;
            max.X = bounds.Max.X * 0.9f + position.X;
            max.Z = bounds.Max.X * 0.9f + position.Z;

            float oldGrav = gravity;
            bool wasGrounded = grounded;
            bool wasSwimming = swimming;

            const int steps = 16;
            const float stsize = 1 / (float)steps;

            for (int i = 0; i < steps; i++)
            {
                var oldpos = position;

                position += velocity * MGame.dt * stsize;

                // Now handle edge detection and position reversion outside of the loop
                if (disallowWalkingOffEdge && grounded)
                {
                    bool hasAdjacentGroundX =
                        CollisionDetector.IsSolidTile((int)Math.Floor(min.X + 0.01f * float.Sign(velocity.X)), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(min.Z)) ||
                        CollisionDetector.IsSolidTile((int)Math.Floor(min.X + 0.01f * float.Sign(velocity.X)), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(max.Z)) ||
                        CollisionDetector.IsSolidTile((int)Math.Floor(max.X + 0.01f * float.Sign(velocity.X)), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(min.Z)) ||
                        CollisionDetector.IsSolidTile((int)Math.Floor(max.X + 0.01f * float.Sign(velocity.X)), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(max.Z));
                    bool hasAdjacentGroundZ =
                        CollisionDetector.IsSolidTile((int)Math.Floor(min.X), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(min.Z + 0.01f * float.Sign(velocity.Z))) ||
                        CollisionDetector.IsSolidTile((int)Math.Floor(max.X), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(min.Z + 0.01f * float.Sign(velocity.Z))) ||
                        CollisionDetector.IsSolidTile((int)Math.Floor(min.X), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(max.Z + 0.01f * float.Sign(velocity.Z))) ||
                        CollisionDetector.IsSolidTile((int)Math.Floor(max.X), (int)Math.Floor(min.Y-0.01f), (int)Math.Floor(max.Z + 0.01f * float.Sign(velocity.Z)));

                    // Revert position and velocity in the direction of movement
                    if (!hasAdjacentGroundX)
                    {
                        position.X -= MGame.dt * (velocity.X) * stsize;
                        velocity.X = 0;
                        grounded = true;
                    }
                    if (!hasAdjacentGroundZ)
                    {
                        position.Z -= MGame.dt * (velocity.Z) * stsize;
                        velocity.Z = 0;
                        grounded = true;
                    }
                }

                Vector3Double push = CollisionDetector.ResolveCollision(bounds, position, ref velocity);
                position = push;
            }
            grounded = false;
            bool ceiling = false;
            swimming = false;

            {
                min = bounds.Min + position;
                max = bounds.Max + position;
                int minx = (int)Math.Floor(min.X);
                int minz = (int)Math.Floor(min.Z);
                int maxx = (int)Math.Ceiling(max.X);
                int maxz = (int)Math.Ceiling(max.Z);

                for (int x = minx; x <= maxx; x++)
                {
                    for (int z = minz; z <= maxz; z++)
                    {
                        (int x, int y, int z) checkpos = (x, 0, z);

                        if (x+0.999f < min.X || x+0.001f > max.X || z+ 0.999f < min.Z || z + 0.001f > max.Z) continue;

                        int v1 = MGame.Instance.GrabVoxel(new Vector3(x, (int)min.Y, z));
                        int v2 = MGame.Instance.GrabVoxel(new Vector3(x, (int)max.Y, z));

                        if ((v1 >= 0 && Voxel.voxelTypes[v1].isLiquid) || (v2 >= 0 && Voxel.voxelTypes[v2].isLiquid)) swimming = true;

                        grounded = CollisionDetector.IsSolidTile(checkpos.x, (int)Math.Floor(min.Y - 0.01), checkpos.z) || grounded;
                        ceiling = CollisionDetector.IsSolidTile(checkpos.x, (int)Math.Floor(max.Y), checkpos.z) || ceiling;
                    }
                }
            }
            if (grounded)
            {
                gravity = 0f;
                velocity.Y = 0f;
            }
            if (ceiling)
            {
                gravity = -1f;
                velocity.Y = gravity;
            }

            if (grounded && !wasGrounded && oldGrav < -12)
            {
                ParticleSystemManager.AddSystem(new ParticleSystem(25, ParticleSystem.TextureProvider.BlockAtlas, Voxel.voxelTypes[MGame.Instance.GrabVoxel(new Vector3((float)position.X, (float)(min.Y), (float)position.Z))].topTexture, new Vector3Double(position.X, min.Y, position.Z), Vector3.Up, 2f, 12f, Vector3.One * 0.25f, Vector3.One * 2));

                OnTakeDamage(new DamageInfo { damage = (int)float.Ceiling((-oldGrav-12) / 1.5f) });
            }

            if(swimming && !wasSwimming)
            {
                ParticleSystemManager.AddSystem(new ParticleSystem(25, ParticleSystem.TextureProvider.BlockAtlas, 2, new Vector3Double(position.X,min.Y,position.Z), Vector3.Up, 2f, 12f, Vector3.One * 0.25f, Vector3.One * 2));
            }
        }
    }
    public static class EntityManager
    {
        public static Dictionary<long, List<Entity>> loadedEntities = new Dictionary<long, List<Entity>>();
        private static Queue<Entity> delete = new Queue<Entity>();

        public static void Clear()
        {
            loadedEntities.Clear();
        }
        public static void SpawnEntity(Entity entity)
        {
            int cx = (int)double.Floor(entity.position.X / Chunk.Size);
            int cy = (int)double.Floor(entity.position.Y / Chunk.Size);
            int cz = (int)double.Floor(entity.position.Z / Chunk.Size);

            entity.parentChunk = MGame.CCPos((cx, cy, cz));

            entity.Start();

            Add(entity);
        }
        public static void DeleteEntity(Entity entity)
        {
            delete.Enqueue(entity);
        }
        public static void UpdateChunk(long chunk)
        {
            while(delete.Count > 0)
            {
                var entity = delete.Dequeue();
                Remove(entity, entity.parentChunk);
                entity.Destroyed();
            }

            if (!loadedEntities.TryGetValue(chunk, out List<Entity> value)) return;

            foreach(var entity in value)
            {
                long oldpos = entity.parentChunk;

                entity.Update();

                if (entity.parentChunk != oldpos)
                {
                    Remove(entity, oldpos);
                    Add(entity);
                }
            }
        }
        public static void RenderChunk(long chunk)
        {
            if (!loadedEntities.TryGetValue(chunk, out List<Entity> value)) return;

            foreach (var entity in value)
            {
                entity.Render();
            }
        }
        private static void Add(Entity entity)
        {
            List<Entity> list;
            if(!loadedEntities.TryGetValue(entity.parentChunk,out list))
            {
                list = new List<Entity>();
                loadedEntities.Add(entity.parentChunk,list);
            }
            list.Add(entity);
        }
        private static void Remove(Entity entity, long pos)
        {
            if (loadedEntities.TryGetValue(pos, out var list) && list.Contains(entity))
            {
                list.Remove(entity);

                if(list.Count == 0) loadedEntities.Remove(pos);
            }
        }
    }
}
