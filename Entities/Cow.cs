using FantasyVoxels.Entities.EntityModels;
using FmodForFoxes.Studio;
using MessagePack;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Entities
{
    internal class Cow : AIEntity
    {
        [JsonIgnore]
        EntityModel model;

        Vector3Double target;

        const float walkSpeed = 2f;
        float time = 0;
        float randomLook = 0;
        float tinttime = 0;
        float fleetime = 0;
        float punchAngle = 0;
        float despawnTime = 60*5;

        FleeWhenAttacked fleeGoal;

        double angleToPlayer = 0;
        double angleToPlayerY = 0;
        bool wasGrounded = false;
        bool step;

        EventInstance curSound;

        private void PlaySound(string id)
        {
            if (Vector3.Distance(MGame.Instance.cameraPosition, (Vector3)position) > 16) return;

            curSound?.Stop();

            curSound = MGame.soundBank[id].CreateInstance();
            curSound.Start();
        }

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
            model.Render(Matrix.CreateRotationZ(rotation.Z+tinttime) * Matrix.CreateRotationX(rotation.Z/4f) * Matrix.CreateRotationY(rotation.Y)*Matrix.CreateTranslation((Vector3)position));
        }

        public override void Start()
        {
            model = new EntityModel("Models/cow", "Textures/cow_var1", 0.75f);
            bounds = new BoundingBox(new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0.9f, 0.5f));

            health = 10;
            maxHealth = 10;

            aiGoals.Add(new AutoSwimGoal());
            fleeGoal = new FleeWhenAttacked(4f);
            aiGoals.Add(fleeGoal);
            aiGoals.Add(new RandomWanderGoal(2f));
        }

        public override void Update()
        {
            if (Vector3.Distance(MGame.Instance.cameraPosition, (Vector3)position) > 48) { despawnTime-=MGame.dt; if (despawnTime <= 0) { EntityManager.DeleteEntity(this); return; } }

            time += MGame.dt*8;
            if(curSound is not null) curSound.Position3D = (Vector3)position;

            if (wasGrounded != grounded) playstepsound();
            if (tinttime > 0)
            {
                tinttime -= MGame.dt;
                model.tint = new Vector3(1.5f, 0.8f, 0.8f);

                if(health <= 0 && tinttime > 0.75f)
                {
                    rotation.Z += MGame.dt*4;
                }

                if (tinttime < 0)
                {
                    model.tint = Vector3.One;
                }
            }

            if(tinttime <= 0 && health <= 0)
            {
                EntityManager.DeleteEntity(this);
                ParticleSystemManager.AddSystem(new ParticleSystem(25, ParticleSystem.TextureProvider.ParticleAtlas, 7, new Vector3Double(position.X, position.Y, position.Z), Vector3.Up, 0.5f, -2f, Vector3.Zero, Vector3.One * 1, 4, 0.5f, 0.25f));
            }
            if (health <= 0)
            {
                base.Update();
                return;
            }

            if (fleetime > 0)
            {
                fleetime -= MGame.dt;
            }
            if(fleeGoal != null) fleeGoal.flee = fleetime > 0;

            if (onPath)
            {
                target = moveTo;
            }
            else
            {
                randomLook -= MGame.dt;
                if (randomLook < 0f)
                {
                    if(Random.Shared.Next(0,6) == 2)
                    {
                        PlaySound("Animals/cow_idle");
                    }

                    randomLook = Random.Shared.NextSingle() * 6;
                    target = new Vector3Double(position.X + (Random.Shared.NextSingle() * 2) - 1, position.Y + 1.6f, position.Z + (Random.Shared.NextSingle() * 2) - 1);
                    if (EntityManager.loadedEntities.TryGetValue(parentChunk, out var otherEntities))
                    {
                        foreach (var other in otherEntities)
                        {
                            if (Random.Shared.Next(0, 10) == 2) target = other.position;
                        }
                    }
                }
            }

            float stepsin = MathF.Sin(time);

            if(velocity.LengthSquared() > 0.4f && float.Abs(stepsin) > 0.9f && !step)
            {
                step = true;
                playstepsound();
            }
            if (step && float.Abs(stepsin) < 0.9f )
            {
                step = false;
            }

            CalculateModelMatrices();

            wasGrounded = grounded;

            base.Update();
        }
        void playstepsound()
        {
            int v = MGame.Instance.GrabVoxel(new Vector3((float)position.X, (float)(bounds.Min.Y + position.Y - 0.8f), (float)position.Z));
            if (v >= 0 && Voxel.voxelTypes[v].surfaceType != Voxel.SurfaceType.None && health > 0)
                MGame.PlayWalkSound(Voxel.voxelTypes[v], (Vector3)position + Vector3.Up * bounds.Min.Y);
        }
        public override void OnTakeDamage(DamageInfo info)
        {
            if (health <= 0) return;

            base.OnTakeDamage(info);

            if(tinttime <= 0)
            {
                tinttime = 0.25f;
                PlaySound("Animals/cow_pain");
            }

            if(grounded)
            {
                grounded = false;
                gravity = 7f;
            }
            if(health < maxHealth/2 && Random.Shared.Next(0,4) == 2) //Start shedding meat
            {
                var item = new DroppedItem(new Item("rawbeef", 1));
                item.position = position;
                item.gravity = 7f;
                EntityManager.SpawnEntity(item);
            }

            if (((Vector3)info.from) == Vector3.Zero) return;

            fleetime = 3f;

            velocity = Vector3.Normalize((Vector3)position - (Vector3)info.from)*7;

            fleeGoal.SetInitTargDir(velocity);
        }
        public override void Die()
        {
            base.Die();
            tinttime = 1;
            PlaySound("Animals/cow_pain");
            var item = new DroppedItem(new Item("rawbeef",1));
            item.position = position;
            item.gravity = 7f;
            EntityManager.SpawnEntity(item);
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

            float movespeed = float.Min(new Vector2(velocity.X, velocity.Z).Length() / 4,2f);

            Vector3 bodytranslation = new Vector3((float.Cos(time) * 0.05f * movespeed), (float.Abs(float.Sin(time)) * 0.05f * movespeed), (float.Abs(float.Sin(time)) *0.05f * movespeed));

            Matrix matrix = Matrix.CreateTranslation(bodytranslation*2)
                * Matrix.CreateRotationX(-(float)angleToPlayerY) 
                * Matrix.CreateRotationZ((float)angleToPlayer) 
                * Matrix.CreateRotationX(float.Abs(MathF.Cos(time-0.2f)) * -0.05f * movespeed) 
                * Matrix.CreateRotationZ(MathF.Sin(time - 0.2f) * 0.05f*movespeed)
                * Matrix.CreateRotationY(MathF.Cos(time - 0.2f) * 0.01f*movespeed)
                * Matrix.CreateTranslation(0,0,float.Sin(time*0.2f)*0.05f);
            model?.SetPartMatrix("head", matrix);
            matrix = Matrix.CreateTranslation((float.Cos(time + 0.5f) * 0.05f * movespeed), 
                                              0, 
                                              (float.Abs(float.Sin(time + 0.5f)) * 0.05f * movespeed)*2) * 
                     Matrix.CreateRotationZ(MathF.Sin(time - 0.5f)*0.2f * movespeed) *
                     Matrix.CreateRotationY(MathF.Cos(time - 0.5f) *0.1f * movespeed) *
                     Matrix.CreateTranslation(0, 0, float.Sin(time * 0.2f-1) * 0.05f);
            model?.SetPartMatrix("body", matrix);

            matrix = Matrix.CreateTranslation((float.Cos(time + 0.5f) * 0.05f * movespeed)*3,
                                              0,
                                              (float.Abs(float.Sin(time + 0.5f)) * 0.05f * movespeed) * 2)
                * Matrix.CreateRotationY(MathF.Cos(time-0.4f) * -0.5f * movespeed)
                * Matrix.CreateRotationY(MathF.Sin(time) * 0.02f * movespeed)
                * Matrix.CreateTranslation(0, 0, float.Sin(time * 0.2f - 1) * 0.05f);
            model?.SetPartMatrix("tail", matrix);

            float footz = float.Max(MathF.Sin(time) * movespeed,0)*0.5f;
            float footy = MathF.Cos(time) * movespeed*0.5f;

            matrix = Matrix.CreateRotationX(footy * 0.5f) * Matrix.CreateTranslation(0, footy, footz);
            model?.SetPartMatrix("ffootr", matrix);
            model?.SetPartMatrix("bfootl", matrix);

            footz = float.Max(MathF.Sin(time+float.DegreesToRadians(180)) * movespeed, 0) * 0.5f;
            footy = MathF.Cos(time + float.DegreesToRadians(180)) * movespeed * 0.5f;

            matrix = Matrix.CreateRotationX(footy * 0.5f) * Matrix.CreateTranslation(0, footy, footz);
            model?.SetPartMatrix("ffootl", matrix);
            model?.SetPartMatrix("bfootr", matrix);
        }
    }
}
