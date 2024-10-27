using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels
{
    public abstract class Entity
    {
        public Vector3 position, rotation, velocity;
        public BoundingBox bounds;
        public float gravity;
        public bool grounded;

        public abstract void Start();
        public virtual void Update()
        {
            if (grounded) gravity = -4;
            else
            {
                gravity -= 64 * MGame.dt;
            }

            velocity.Y = grounded ? 0 : gravity;

            HandleCollisions();
        }
        public abstract void Render();
        public abstract void Destroyed();


        public void HandleCollisions()
        {
            Vector3 min = bounds.Min + position;
            Vector3 max = bounds.Max + position;

            int cx = (int)MathF.Floor(min.X / Chunk.Size);
            int cy = (int)MathF.Floor(min.Y / Chunk.Size);
            int cz = (int)MathF.Floor(min.Z / Chunk.Size);

            if (!MGame.Instance.loadedChunks.ContainsKey((cx, cy, cz))) return;

            cx = (int)MathF.Floor(max.X / Chunk.Size);
            cy = (int)MathF.Floor(max.Y / Chunk.Size);
            cz = (int)MathF.Floor(max.Z / Chunk.Size);

            if (!MGame.Instance.loadedChunks.ContainsKey((cx, cy, cz))) return;

            for (int i = 0; i < 4; i++)
            {
                position += velocity * MGame.dt * (1 / 4f);

                Vector3 push = CollisionDetector.ResolveCollision(bounds, position, ref velocity, grounded);
                position = push;
            }

            grounded = false;
            bool ceiling = false;

            {
                int minx = (int)MathF.Floor(min.X);
                int minz = (int)MathF.Floor(min.Z);
                int maxx = (int)MathF.Ceiling(max.X);
                int maxz = (int)MathF.Ceiling(max.Z);

                for (int x = minx; x <= maxx; x++)
                {
                    for (int z = minz; z <= maxz; z++)
                    {
                        (int x, int y, int z) checkpos = (x, 0, z);

                        if (x+0.9f <= min.X || x+0.1f >= max.X || z+ 0.9f <= min.Z || z + 0.1f >= max.Z) continue;

                        grounded = CollisionDetector.IsSolidTile(checkpos.x, (int)MathF.Floor(min.Y - 0.5f), checkpos.z) || grounded;
                        ceiling = CollisionDetector.IsSolidTile(checkpos.x, (int)MathF.Floor(max.Y), checkpos.z) || ceiling;
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
                gravity = -4f;
                velocity.Y = gravity;
            }
        }
    }
}
