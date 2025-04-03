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
    public class FleeWhenAttacked : AIGoal
    {
        private float waitTime = 0f;
        private float walkSpeed;
        Vector3Double curTargDir;
        public bool flee;
        public FleeWhenAttacked(float walkSpeed)        {
            this.walkSpeed = walkSpeed;
        }
        public void SetInitTargDir(Vector3Double dir)
        {
            curTargDir = dir;
        }
        public override bool GetTargetPoint(out Vector3Double targetPos, out float speed, Entity from)
        {
            targetPos = from.position;
            speed = 0;

            if (!flee) return false;

            waitTime -= MGame.dt;

            targetPos = from.position + curTargDir;
            speed = walkSpeed;

            if (waitTime <= 0f)
            {
                waitTime = Random.Shared.NextSingle() + 0.25f;

                curTargDir = new Vector3Double(((Random.Shared.NextSingle() * 2) - 1) * 7, 0, ((Random.Shared.NextSingle() * 2) - 1) * 7);
            }

            return true;
        }

        public override void UpdateEntity(Entity self)
        {
        }
	}
	public class ChaseGoal : AIGoal
	{
		private float walkSpeed;
        public Entity target;
		public ChaseGoal(float walkSpeed)
		{
			this.walkSpeed = walkSpeed;
		}
		public override bool GetTargetPoint(out Vector3Double targetPos, out float speed, Entity from)
		{
			targetPos = from.position;
			speed = 0;

			if (target is null) return false;

			targetPos = target.position;
			speed = walkSpeed;

			return true;
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
                self.gravity = MathF.Min(self.gravity + 24 * MGame.dt, 2.5f);
                self.grounded = false;
            }
        }
    }
}
