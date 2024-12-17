using FantasyVoxels.Entities.EntityModels;
using MessagePack;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Entities
{
    internal class WanderAITest : AIEntity
    {
        [JsonIgnore]
        EntityModel model;

        Vector3Double target;

        const float walkSpeed = 2f;
        float time = 0;
        float randomLook = 0;

        double angleToPlayer = 0;
        double angleToPlayerY = 0;

        public override object CaptureCustomSaveData()
        {
            return null;
        }
        public override void RestoreCustomSaveData(object data)
        {
        }

        public override void Destroyed()
        {
        }

        public override void Render()
        {
            model.Render(Matrix.CreateRotationY(rotation.Y)*Matrix.CreateTranslation((Vector3)position));
        }

        public override void Start()
        {
            model = new EntityModel("Models/roboPlayer", "Textures/RBBody", 0.25f);
            bounds = new BoundingBox(new Vector3(-0.2f, 0, -0.2f), new Vector3(0.2f, 1.6f, 0.2f));

            aiGoals.Add(new AutoSwimGoal());
            aiGoals.Add(new RandomWanderGoal(2f));
        }

        public override void Update()
        {
            time += MGame.dt*8;

            if(onPath)
            {
                target = moveTo;
            }
            else
            {
                randomLook -= MGame.dt;
                if(randomLook < 0f)
                {
                    randomLook = Random.Shared.NextSingle()*6;
                    target = new Vector3Double(position.X+ (Random.Shared.NextSingle()*2)-1,position.Y+1.6f, position.Z + (Random.Shared.NextSingle() * 2) - 1);
                    if(EntityManager.loadedEntities.TryGetValue(parentChunk, out var otherEntities))
                    {
                        foreach(var other in otherEntities)
                        {
                            if (Random.Shared.Next(0, 10) == 2) target = other.position;
                        }
                    }
                }
            }

            CalculateModelMatrices();

            base.Update();
        }
        public override void OnTakeDamage(DamageInfo info)
        {
            base.OnTakeDamage(info);
            if(grounded)
            {
                grounded = false;
                gravity = 5f;
            }
            velocity = ((Vector3)position - (Vector3)info.from)*2f;
            target = info.from;
        }
        void CalculateModelMatrices()
        {
            Vector3 lookTarget = (Vector3)target;

            double a = Math.Atan2(lookTarget.X - position.X, lookTarget.Z - position.Z);

            rotation.Y = MathHelper.ToRadians(Maths.MoveTowardsAngle(MathHelper.ToDegrees(rotation.Y), MathHelper.ToDegrees((float)a), MGame.dt * 100));

            a -= rotation.Y;

            double aY = Math.Atan2(lookTarget.Y - (position.Y+1.6f), Vector2.Distance(new Vector2((float)lookTarget.X, (float)lookTarget.Z), new Vector2((float)position.X, (float)position.Z)));

            angleToPlayer = MathHelper.ToRadians(Maths.MoveTowardsAngle(MathHelper.ToDegrees((float)angleToPlayer), MathHelper.ToDegrees((float)a), MGame.dt*250));
            angleToPlayerY = MathHelper.ToRadians(Maths.MoveTowardsAngle(MathHelper.ToDegrees((float)angleToPlayerY), MathHelper.ToDegrees((float)aY), MGame.dt*250));

            float movespeed = float.Min(new Vector2(velocity.X, velocity.Z).Length() / 2,2f);

            Vector3 bodytranslation = new Vector3(0,0,-float.Abs(MathF.Cos(time))*0.2f * movespeed);

            Matrix matrix = Matrix.CreateTranslation(bodytranslation) * Matrix.CreateRotationZ((float)angleToPlayer);
            model?.SetPartMatrix("neck",matrix);
            matrix = Matrix.CreateTranslation(bodytranslation*2)
                * Matrix.CreateRotationX(-(float)angleToPlayerY) 
                * Matrix.CreateRotationZ((float)angleToPlayer) 
                * Matrix.CreateRotationX(float.Abs(MathF.Cos(time-0.2f)) * -0.05f * movespeed) 
                * Matrix.CreateRotationZ(MathF.Sin(time - 0.2f) * 0.05f*movespeed)
                * Matrix.CreateRotationY(MathF.Cos(time - 0.2f) * 0.01f*movespeed);
            model?.SetPartMatrix("head", matrix);
            matrix = Matrix.CreateTranslation(bodytranslation) * Matrix.CreateRotationZ(MathF.Cos(time)*0.1f * movespeed) * Matrix.CreateRotationY(MathF.Sin(time)*0.02f * movespeed);
            model?.SetPartMatrix("body", matrix);

            float footy = MathF.Sin(time) * movespeed;
            float rfooty = -MathF.Sin(time) * movespeed;
            float footz = float.Max(-MathF.Cos(time),0) * movespeed;
            float rfootz = float.Max(MathF.Cos(time),0) * movespeed;

            matrix = Matrix.CreateRotationX(footy * 0.5f) * Matrix.CreateTranslation(0, footy, footz);
            model?.SetPartMatrix("lfoot", matrix);
            model?.SetPartMatrix("ltoe", matrix);

            matrix = Matrix.CreateRotationX(rfooty * 0.5f) * Matrix.CreateTranslation(0, rfooty, rfootz);
            model?.SetPartMatrix("rfoot", matrix);
            model?.SetPartMatrix("rtoe", matrix);

            footy = -MathF.Sin(time) * movespeed;
            footz = (-float.Abs(MathF.Cos(time))+1) * movespeed * 0.2f;
            matrix = Matrix.CreateRotationX(MathF.Cos(time) * 0.4f * movespeed) * Matrix.CreateTranslation(0, footy, footz) * Matrix.CreateRotationZ(footy*0.2f);
            model?.SetPartMatrix("lhand", matrix);

            footy = MathF.Sin(time) * movespeed;
            footz = (-float.Abs(-MathF.Cos(time))+1) * movespeed * 0.2f;
            matrix = Matrix.CreateRotationX(-MathF.Cos(time) * 0.4f * movespeed) * Matrix.CreateTranslation(0, footy, footz) * Matrix.CreateRotationZ(footy * -0.2f);
            model?.SetPartMatrix("rhand", matrix);
        }
    }
}
