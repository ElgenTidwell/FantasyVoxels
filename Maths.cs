using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels
{
    public static class Maths
    {
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            Vector3 a = target - current;
            float magnitude = a.Length();
            if (magnitude <= maxDistanceDelta || magnitude == 0f)
            {
                return target;
            }
            return current + a / magnitude * maxDistanceDelta;
        }
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
            {
                return target;
            }
            return current + MathF.Sign(target - current) * maxDelta;
        }
        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            float sqrMag = Vector3.Dot(onNormal, onNormal);
            if (sqrMag < float.Epsilon)
                return Vector3.Zero;
            else
            {
                var dot = Vector3.Dot(vector, onNormal);
                return new Vector3(onNormal.X * dot / sqrMag,
                    onNormal.Y * dot / sqrMag,
                    onNormal.Z * dot / sqrMag);
            }
        }
        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            float sqrMag = Vector3.Dot(planeNormal, planeNormal);
            if (sqrMag < float.Epsilon)
                return vector;
            else
            {
                var dot = Vector3.Dot(vector, planeNormal);
                return new Vector3(vector.X - planeNormal.X * dot / sqrMag,
                    vector.Y - planeNormal.Y * dot / sqrMag,
                    vector.Z - planeNormal.Z * dot / sqrMag);
            }
        }
        public static bool Raycast(Vector3 start, Vector3 direction, float distance, out Vector3 prevHitTile, out Vector3 hitTile, out int voxel, out VoxelData data)
        {
            // Current position in the grid (tilemap coordinates)
            int x = (int)Math.Floor(start.X);
            int y = (int)Math.Floor(start.Y);
            int z = (int)Math.Floor(start.Z);

            // Direction signs
            int stepX = direction.X > 0 ? 1 : -1;
            int stepY = direction.Y > 0 ? 1 : -1;
            int stepZ = direction.Z > 0 ? 1 : -1;

            // Avoid division by zero by replacing 0 with a small value
            Vector3 deltaDist = new Vector3(
                direction.X == 0 ? 0.001f : Math.Abs(1f / direction.X),
                direction.Y == 0 ? 0.001f : Math.Abs(1f / direction.Y),
                direction.Z == 0 ? 0.001f : Math.Abs(1f / direction.Z)
            );

            // Calculate initial side distances
            Vector3 sideDist = new Vector3(
                (stepX > 0 ? (x - start.X + 1f) : (start.X - x)) * deltaDist.X,
                (stepY > 0 ? (y - start.Y + 1f) : (start.Y - y)) * deltaDist.Y,
                (stepZ > 0 ? (z - start.Z + 1f) : (start.Z - z)) * deltaDist.Z
            );

            Vector3 hitPosition = start;
            hitTile = new Vector3(x, y, z);
            prevHitTile = hitTile;

            // Start stepping through the tilemap
            while (Vector3.Distance(hitPosition, start) <= distance)
            {
                prevHitTile = hitTile;
                hitPosition = new Vector3(x, y, z);
                hitTile = new Vector3(x, y, z);

                {
                    // Check if the current tile is not empty
                    if (CollisionDetector.IsSolidTile(x,y,z,true))
                    {
                        voxel = MGame.Instance.GrabVoxel(new (x,y,z));
                        data = MGame.Instance.GrabVoxelData(new(x, y, z), out var _data)?_data:new VoxelData();
                        
                        if(voxel >= 0 && Voxel.voxelTypes[voxel].myClass != null && Voxel.voxelTypes[voxel].myClass.customBounds)
                        {
                            var b = Voxel.voxelTypes[voxel].myClass.GetCustomBounds(data.placement);

                            b.Min += new Vector3(x,y,z);
                            b.Max += new Vector3(x,y,z);
                            if ((b.Intersects(new Ray(start, direction)).HasValue)) return true;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

                // Move to the next tile in the direction of the smallest side distance
                if (sideDist.X < sideDist.Y)
                {
                    if (sideDist.X < sideDist.Z)
                    {
                        sideDist.X += deltaDist.X;
                        x += stepX;
                    }
                    else
                    {
                        sideDist.Z += deltaDist.Z;
                        z += stepZ;
                    }
                }
                else
                {
                    if (sideDist.Y < sideDist.Z)
                    {
                        sideDist.Y += deltaDist.Y;
                        y += stepY;
                    }
                    else
                    {
                        sideDist.Z += deltaDist.Z;
                        z += stepZ;
                    }
                }
            }

            // If no tile was hit
            hitPosition = Vector3.Zero;
            hitTile = new Vector3(-1, -1, -1);
            voxel = 0;
            data = new VoxelData();
            return false;
        }
    }
}
