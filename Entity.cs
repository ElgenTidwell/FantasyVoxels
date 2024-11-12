using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public float gravity;
        public bool grounded;
        public bool swimming;
        public byte health,maxHealth;
        protected bool disallowWalkingOffEdge;
        public abstract void Start();
        public virtual void Update()
        {
            if (grounded) gravity = !swimming?-0.6f:0;
            else
            {
                gravity = swimming? Maths.MoveTowards(gravity, -1f,MGame.dt*14f) : gravity - 20 * MGame.dt;
            }

            velocity.Y = grounded ? 0 : gravity;

            HandleCollisions();
        }
        public abstract void Render();
        public abstract void Destroyed();

        public abstract object CaptureCustomSaveData();
        public abstract void RestoreCustomSaveData(JObject data);

        public void HandleCollisions()
        {
            Vector3Double min = bounds.Min + position;
            Vector3Double max = bounds.Max + position;

            int cx = (int)Math.Floor(min.X / Chunk.Size);
            int cy = (int)Math.Floor(min.Y / Chunk.Size);
            int cz = (int)Math.Floor(min.Z / Chunk.Size);

            if (!MGame.Instance.loadedChunks.ContainsKey(MGame.CCPos((cx, cy, cz)))) return;

            cx = (int)Math.Floor(max.X / Chunk.Size);
            cy = (int)Math.Floor(max.Y / Chunk.Size);
            cz = (int)Math.Floor(max.Z / Chunk.Size);

            if (!MGame.Instance.loadedChunks.ContainsKey(MGame.CCPos((cx, cy, cz)))) return;

            Vector3Double oldPos = position;
            bool wasGrounded = grounded;

            for (int i = 0; i < 8; i++)
            {
                position += velocity * MGame.dt * (1 / 8f);

                Vector3Double push = CollisionDetector.ResolveCollision(bounds, position, ref velocity);
                position = push;
            }
            grounded = false;
            bool ceiling = false;
            swimming = false;

            {
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

            if (wasGrounded && !grounded && disallowWalkingOffEdge)
            {
                position = oldPos;
                grounded = true;
            }
        }
    }
}
