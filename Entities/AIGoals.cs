using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Entities
{
    public abstract class AIGoal
    {
        public abstract bool GetTargetPoint(out Vector3Double targetPos, out float speed, Entity from);
        public abstract void UpdateEntity(Entity self);
    }
    public class RandomWanderGoal : AIGoal
    {
        private float waitTime = 0f;
        private float walkSpeed;
        public RandomWanderGoal(float walkSpeed)        {
            this.walkSpeed = walkSpeed;
        }

        public override bool GetTargetPoint(out Vector3Double targetPos, out float speed, Entity from)
        {
            waitTime -= MGame.dt;

            targetPos = from.position;
            speed = 0f;

            if(waitTime <= 0f)
            {
                waitTime = Random.Shared.NextSingle() * 8 + 1f;

                targetPos = from.position + new Vector3Double(((Random.Shared.NextSingle() * 2) - 1) * 7, 0, ((Random.Shared.NextSingle() * 2) - 1) * 7);

                speed = walkSpeed;

                return true;
            }
            return false;
        }

        public override void UpdateEntity(Entity self)
        {
        }
    }
    public class AutoSwimGoal : AIGoal
    {
        public override bool GetTargetPoint(out Vector3Double targetPos, out float speed, Entity from)
        {
            targetPos = from.position;
            speed = 0;
            return false;
        }

        public override void UpdateEntity(Entity self)
        {
            if (self.swimming)
            {
                self.gravity = MathF.Min(self.gravity + 28 * MGame.dt, 4.5f);
                self.grounded = false;
            }
        }
    }
}
