﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FantasyVoxels.Entities
{
    public abstract class AIEntity : Entity
    {
        protected List<AIGoal> aiGoals = new List<AIGoal>();
        protected Vector3Double moveTo;
        protected float curSpeed;
        protected bool onPath;
        protected float pathTolerance = 3;
        float pathTime = 0;

        public override void Update()
        {
            if(health <= 0) { base.Update(); return; }

            foreach (var goal in aiGoals)
            {
                goal.UpdateEntity(this);
            }
            if (!onPath)
            {
                moveTo = position;
                curSpeed = 0f;
                bool any = false;
                foreach (var goal in aiGoals)
                {
                    if (goal.GetTargetPoint(out moveTo, out curSpeed, this)) { any = true; break; }
                }
                onPath = any;
                pathTime = 0f;
            }

            //try and walk to the target.
            Vector3 wishDir = (Vector3)moveTo - (Vector3)position;
            wishDir.Y = 0;
            wishDir.Normalize();

            Vector3 targFlat = (Vector3)moveTo; targFlat.Y = 0;
            Vector3 targPos = (Vector3)position; targPos.Y = 0;

            pathTime += MGame.dt;

            float targdist = Vector3.Distance(targPos, targFlat);
            if (targdist < pathTolerance || pathTime > 5)
            {
                pathTime = 0f;
                wishDir = Vector3.Zero;
                onPath = false;
            }

            velocity = Maths.MoveTowards(velocity, wishDir * (swimming ? curSpeed*0.5f : curSpeed), MGame.dt * (swimming?4f:10));

            Vector3 minPos = (Vector3)position + bounds.Min.Y*Vector3.Up;

            if (velocity.LengthSquared() > 0 && MGame.Instance.GrabVoxel(minPos+wishDir) != 0 && grounded)
            {
                gravity = 7f;
                grounded = false;
            }

            base.Update();
        }
    }
}
