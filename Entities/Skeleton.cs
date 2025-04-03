using FantasyVoxels.Entities.EntityModels;
using FantasyVoxels.Entities;
using FantasyVoxels;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FantasyVoxels.Entity;
using FmodForFoxes.Studio;
using Newtonsoft.Json;

namespace Solovox.Entities
{
	internal class Skeleton : AIEntity
	{
		[JsonIgnore]
		EntityModel model;

		Vector3Double target;

		const float walkSpeed = 2f;
		float time = 0;
		float randomLook = 0;
		float tinttime = 0;
		float fleetime = 0;
		float attacktime = 0;
		float punchAngle = 0;
		float daylightDamage = 0.25f;

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
			model.Render(Matrix.CreateRotationZ(rotation.Z + tinttime) * Matrix.CreateRotationX(rotation.Z / 4f) * Matrix.CreateRotationY(rotation.Y) * Matrix.CreateTranslation((Vector3)position));
		}

		public override void Start()
		{
			model = new EntityModel("Models/skeleton", "Textures/skeleton", 1);
			bounds = new BoundingBox(new Vector3(-0.25f, 0, -0.25f), new Vector3(0.25f, 1.8f, 0.25f));

			health = 10;
			maxHealth = 10;

			aiGoals.Add(new ChaseGoal(4f));
			aiGoals.Add(new AutoSwimGoal());
			aiGoals.Add(new RandomWanderGoal(3f));
		}

		public override void Update()
		{
			time += MGame.dt * 8;

			if (aiGoals == null || aiGoals.Count == 0) return;

			if (curSound is not null) curSound.Position3D = (Vector3)position;

			if (wasGrounded != grounded) playstepsound();
			if (tinttime > 0)
			{
				tinttime -= MGame.dt;
				model.tint = new Vector3(1.5f, 0.8f, 0.8f);

				if (health <= 0 && tinttime > 0.75f)
				{
					rotation.Z += MGame.dt * 4;
				}

				if (tinttime < 0)
				{
					model.tint = Vector3.One;
				}
			}
			attacktime -= MGame.dt;
			daylightDamage -= MGame.dt;

			if (!WorldTimeManager.NightTime && health > 0 && daylightDamage < 0)
			{
				daylightDamage = 0.5f;
				if(MGame.Instance.GrabVoxelData((Vector3)position, out var vData) && vData.skyLight > 200) OnTakeDamage(new DamageInfo { damage = 1 });
			}

			if (tinttime <= 0 && health <= 0)
			{
				EntityManager.DeleteEntity(this);
				ParticleSystemManager.AddSystem(new ParticleSystem(25, ParticleSystem.TextureProvider.ParticleAtlas, 7, new Vector3Double(position.X, position.Y, position.Z), Vector3.Up, 0.5f, -2f, Vector3.Zero, Vector3.One * 1, 4, 0.5f, 0.25f));
			}
			if (health <= 0)
			{
				base.Update();
				return;
			}

			if (onPath)
			{
				target = moveTo;
			}
			else
			{
				randomLook -= MGame.dt;
				if (randomLook < 0f)
				{
					//if (Random.Shared.Next(0, 6) == 2)
					//{
					//	PlaySound("Animals/cow_idle");
					//}

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

			if (velocity.LengthSquared() > 0.4f && float.Abs(stepsin) > 0.9f && !step)
			{
				step = true;
				playstepsound();
			}
			if (step && float.Abs(stepsin) < 0.9f)
			{
				step = false;
			}

			CalculateModelMatrices();

			wasGrounded = grounded;

			if(Vector3.Distance((Vector3)MGame.Instance.player.position,(Vector3)position) < 8 && (aiGoals[0] is ChaseGoal c))
			{
				c.target = MGame.Instance.player;
			}

			pathTolerance = 3;
			if((aiGoals[0] is ChaseGoal g) && g.target is not null)
			{
				target = g.target.position;
				pathTolerance = 1;

				if(attacktime < 0 && Vector3.Distance((Vector3)g.target.position, (Vector3)position) < 2f)
				{
					g.target.OnTakeDamage(new DamageInfo { damage = 0, from = this.position, fromEntity = this });
					attacktime = 0.5f;
				}
				if (Vector3.Distance((Vector3)g.target.position, (Vector3)position) > 12)
				{
					g.target = null;
				}
			}

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

			if (tinttime <= 0)
			{
				tinttime = 0.25f;
				PlaySound("Animals/cow_pain");
			}

			if (grounded)
			{
				grounded = false;
				gravity = 7f;
			}

			if (((Vector3)info.from) == Vector3.Zero) return;

			fleetime = 3f;

			velocity = Vector3.Normalize((Vector3)position - (Vector3)info.from) * 7;
		}
		public override void Die()
		{
			base.Die();
			tinttime = 1;
			PlaySound("Animals/cow_pain");
		}
		void CalculateModelMatrices()
		{
			Vector3 lookTarget = (Vector3)target;

			double a = Math.Atan2(lookTarget.X - position.X, lookTarget.Z - position.Z);

			rotation.Y = MathHelper.ToRadians(Maths.MoveTowardsAngle(MathHelper.ToDegrees(rotation.Y), MathHelper.ToDegrees((float)a), MGame.dt * 100));

			a -= rotation.Y;

			double aY = Math.Atan2(lookTarget.Y - (position.Y + 1.6f), Vector2.Distance(new Vector2((float)lookTarget.X, (float)lookTarget.Z), new Vector2((float)position.X, (float)position.Z)));

			angleToPlayer = MathHelper.ToRadians(Maths.MoveTowardsAngle(MathHelper.ToDegrees((float)angleToPlayer), MathHelper.ToDegrees((float)a), MGame.dt * 250));
			angleToPlayerY = MathHelper.ToRadians(Maths.MoveTowardsAngle(MathHelper.ToDegrees((float)angleToPlayerY), -MathHelper.ToDegrees((float)aY), MGame.dt * 250));

			float movespeed = float.Min(new Vector2(velocity.X, velocity.Z).Length() / 4, 2f);

			Vector3 bodytranslation = new Vector3((float.Cos(time) * 0.05f * movespeed), (float.Abs(float.Sin(time)) * 0.05f * movespeed), (float.Abs(float.Sin(time)) * 0.05f * movespeed));

			Matrix matrix = Matrix.CreateTranslation(bodytranslation * 2)
				* Matrix.CreateRotationX(-(float)angleToPlayerY)
				* Matrix.CreateRotationY((float)angleToPlayer)
				* Matrix.CreateRotationX(float.Abs(MathF.Cos(time - 0.2f)) * -0.05f * movespeed)
				* Matrix.CreateRotationZ(MathF.Sin(time - 0.2f) * 0.05f * movespeed)
				* Matrix.CreateRotationY(MathF.Cos(time - 0.2f) * 0.01f * movespeed)
				* Matrix.CreateTranslation(0, float.Sin(time * 0.2f) * 0.05f, 0);
			model?.SetPartMatrix("head", matrix);

			matrix = Matrix.CreateRotationX(-MathF.Sin(time * 0.2f + 0.4f) * 0.1f)
				* Matrix.CreateTranslation(bodytranslation * 2)
				* Matrix.CreateRotationX(float.Abs(MathF.Cos(time - 0.2f)) * -0.05f * movespeed)
				* Matrix.CreateRotationZ(MathF.Sin(time - 0.2f) * 0.05f * movespeed)
				* Matrix.CreateRotationY(MathF.Cos(time - 0.2f) * 0.01f * movespeed)
				* Matrix.CreateTranslation(0, float.Sin(time * 0.2f) * 0.05f, 0);

			model?.SetPartMatrix("jaw", matrix);

			matrix = Matrix.CreateTranslation(bodytranslation)
				* Matrix.CreateRotationX(float.Abs(MathF.Cos(time - 0.2f)) * -0.05f * movespeed)
				* Matrix.CreateRotationZ(MathF.Sin(time - 0.2f) * 0.05f * movespeed)
				* Matrix.CreateRotationY(MathF.Cos(time - 0.2f) * 0.01f * movespeed)
				* Matrix.CreateTranslation(0, float.Sin(time * 0.2f - 0.5f) * 0.02f, 0);

			model?.SetPartMatrix("torso", matrix);

			float armRot = float.Clamp(float.Clamp(movespeed, 0, 1) + 0.1f + float.Max(attacktime,0) *4,0,1.25f) + float.Max(attacktime, 0);

			matrix = Matrix.CreateRotationY(-1.6f)
				   * Matrix.CreateRotationX(float.Abs(float.Sin(time + 0.6f)) * movespeed * 0.3f-0.2f)
				   * Matrix.CreateRotationX(float.DegreesToRadians(armRot*90-90))
				   * Matrix.CreateTranslation(bodytranslation * 1);

			model?.SetPartMatrix("uarml", matrix);

			matrix = Matrix.CreateRotationY(-1.6f)
				   * Matrix.CreateTranslation(0.5f, 0, 0)
				   * Matrix.CreateRotationX(float.DegreesToRadians(armRot * 90 - 90) + float.Abs(float.Sin(time-0.2f) * movespeed) * 0.4f - 0.3f)
				   * Matrix.CreateTranslation(matrix.Translation - matrix.Right * 0.4f);

			model?.SetPartMatrix("larml", matrix);

			matrix = Matrix.CreateRotationY(1.6f)
				   * Matrix.CreateRotationX(float.Abs(float.Sin(time + 0.6f)) * movespeed * 0.3f - 0.2f)
				   * Matrix.CreateRotationX(float.DegreesToRadians(armRot * 90 - 90))
				   * Matrix.CreateTranslation(bodytranslation * 1);

			model?.SetPartMatrix("uarmr", matrix);

			matrix = Matrix.CreateRotationY(1.6f)
				   * Matrix.CreateTranslation(-0.5f, 0, 0)
				   * Matrix.CreateRotationX(float.DegreesToRadians(armRot * 90 - 90) + float.Abs(float.Sin(time - 0.2f) * movespeed) * 0.4f - 0.3f)
				   * Matrix.CreateTranslation(matrix.Translation + matrix.Right * 0.4f);

			model?.SetPartMatrix("larmr", matrix);


			float legposx, legposz;

			legposz = (float.Sin(time)*0.1f) * movespeed;
			legposx = -float.Abs(float.Cos(time) * 0.1f) * movespeed;

			matrix = Matrix.CreateTranslation(bodytranslation*0.5f)
					* Matrix.CreateTranslation(0, legposx, legposz)
					* Matrix.CreateRotationX(-legposz*5);

			model?.SetPartMatrix("ulegl", matrix);

			matrix = Matrix.CreateRotationX(float.Cos(time+0.8f) * movespeed)
					* Matrix.CreateTranslation(matrix.Translation)
					* Matrix.CreateTranslation(0, 0, legposz*2);

			model?.SetPartMatrix("llegl", matrix);

			legposz = -(float.Sin(time) * 0.1f) * movespeed;
			legposx = -float.Abs(float.Cos(time) * 0.1f) * movespeed;

			matrix = Matrix.CreateTranslation(bodytranslation * 0.5f)
					* Matrix.CreateTranslation(0, legposx, legposz)
					* Matrix.CreateRotationX(-legposz * 5);

			model?.SetPartMatrix("ulegr", matrix);

			matrix = Matrix.CreateRotationX(-float.Cos(time + 0.8f) * movespeed)
					* Matrix.CreateTranslation(matrix.Translation)
					* Matrix.CreateTranslation(0, 0, legposz * 2);

			model?.SetPartMatrix("llegr", matrix);
		}
	}
}
