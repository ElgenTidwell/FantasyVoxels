using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using InternalFPS;
using Icaria.Engine.Procedural;
using Microsoft.Xna.Framework.Graphics;

namespace FantasyVoxels.Entities
{
    public class Player : Entity
    {
        static float walk = 18f, run = 32f, sneak = 8f, swim = 0.5f;

        MouseState oldState;

        Vector3 forward, right;
        float curwalkspeed = 12;
        float desiredFOV = 80f;

        float autoDigTime = 0f;

        bool running,crouched;

        SecondOrderDynamics cameraBounce;


        public VertexPosition[] voxelBoxVert =
        [
            new VertexPosition(new Vector3(0, 0, 0)),
            new VertexPosition(new Vector3(1, 0, 0)),

            new VertexPosition(new Vector3(0, 0, 0)),
            new VertexPosition(new Vector3(0, 1, 0)),

            new VertexPosition(new Vector3(0, 0, 0)),
            new VertexPosition(new Vector3(0, 0, 1)),


            new VertexPosition(new Vector3(0, 1, 0)),
            new VertexPosition(new Vector3(1, 1, 0)),

            new VertexPosition(new Vector3(0, 1, 0)),
            new VertexPosition(new Vector3(0, 1, 1)),

            new VertexPosition(new Vector3(0, 0, 1)),
            new VertexPosition(new Vector3(0, 1, 1)),

            new VertexPosition(new Vector3(1, 0, 0)),
            new VertexPosition(new Vector3(1, 1, 0)),


            new VertexPosition(new Vector3(1, 0, 1)),
            new VertexPosition(new Vector3(1, 0, 0)),

            new VertexPosition(new Vector3(1, 0, 1)),
            new VertexPosition(new Vector3(1, 1, 1)),

            new VertexPosition(new Vector3(1, 0, 1)),
            new VertexPosition(new Vector3(0, 0, 1)),


            new VertexPosition(new Vector3(1, 1, 1)),
            new VertexPosition(new Vector3(1, 1, 0)),

            new VertexPosition(new Vector3(1, 1, 1)),
            new VertexPosition(new Vector3(0, 1, 1)),
        ];
        public VertexBuffer vertBuffer;
        BasicEffect effect;

        Vector3 hitTile,prevHitTile;
        bool voxelHit;

        public override void Start()
        {
            bounds = new BoundingBox(new Vector3(-1.5f,-8,-1.5f), new Vector3(1.5f, 1, 1.5f));
            cameraBounce = new SecondOrderDynamics(3.8f, 0.7f, 0.6f, position.Y);

            vertBuffer = new VertexBuffer(MGame.Instance.GraphicsDevice, typeof(VertexPosition), voxelBoxVert.Length, BufferUsage.WriteOnly);
            vertBuffer.SetData(voxelBoxVert);

            effect = new BasicEffect(MGame.Instance.GraphicsDevice);
            effect.TextureEnabled = false;
            effect.LightingEnabled = false;
            effect.FogEnabled = false;
            effect.VertexColorEnabled = false;
        }

        public override void Update()
        {
            if (MathF.Abs(velocity.X + velocity.Z) < 4f) running = false;
            running = (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || running) && !crouched;

            crouched = Keyboard.GetState().IsKeyDown(Keys.LeftShift);

            float xsin, ysin;
            float bobMulti = !grounded ?0: new Vector2(velocity.X,velocity.Z).Length()/walk;
            xsin = MathF.Sin(MGame.totalTime * (running ? 8 : 6))* bobMulti;
            ysin = MathF.Sin(MGame.totalTime * (running ? 8 : 6) * 2)* bobMulti;

            var state = Mouse.GetState();
            rotation.Y += MathHelper.ToRadians(oldState.X - state.X) * 0.1f;
            rotation.X += MathHelper.ToRadians(oldState.Y - state.Y) * 0.1f;

            rotation.X = MathHelper.Clamp(rotation.X, MathHelper.ToRadians(-90f), MathHelper.ToRadians(90f));

            Mouse.SetPosition(200, 200);
            oldState = Mouse.GetState();

            float cameraY = cameraBounce.Update(MGame.dt, position.Y) + MathF.Abs(xsin) * 0.3f;

            cameraY = MathF.Min(MathF.Max(cameraY, bounds.Min.Y+position.Y), bounds.Max.Y + position.Y);

            var rotmat = Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            right = rotmat.Right;

            MGame.Instance.FOV = Maths.MoveTowards(MGame.Instance.FOV, desiredFOV + (running && MathF.Abs(velocity.X) + MathF.Abs(velocity.Z) > 8f ? 5 : 0),MGame.dt*30);
            MGame.Instance.cameraPosition = new Vector3(position.X+0.5f, cameraY, position.Z + 0.5f) -right*xsin*0.03f;
            MGame.Instance.view =
                Matrix.CreateRotationY(-rotation.Y) *
                Matrix.CreateRotationX(-rotation.X+MathF.Abs(xsin)*0.004f) *
                Matrix.CreateRotationZ(-rotation.Z);
            MGame.Instance.world = Matrix.CreateWorld(-MGame.Instance.cameraPosition, Vector3.Forward, Vector3.Up);

            Vector3 wishDir = Vector3.Zero;
            if (Keyboard.GetState().IsKeyDown(Keys.W)) wishDir += forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.S)) wishDir -= forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.D)) wishDir += right * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.A)) wishDir -= right * MGame.dt;

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && (grounded || swimming))
            {
                gravity = swimming?14:28;
                grounded = false;
            }

            curwalkspeed = crouched? sneak : (running ? run : walk) * (swimming?swim:1);

            rotmat = Matrix.CreateRotationX(rotation.X)*Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            MGame.Instance.cameraForward = forward;
            right = rotmat.Right;

            voxelHit = Maths.Raycast(MGame.Instance.cameraPosition,forward,24,out prevHitTile, out hitTile);

            prevHitTile = Vector3.Floor(prevHitTile / 4) * 4;

            autoDigTime -= MGame.dt;
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && voxelHit && autoDigTime <= 0)
            {
                autoDigTime = 0.25f;
                if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                {
                    MGame.Instance.SetVoxel(hitTile, 0);
                }
                else
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            for (int z = 0; z < 4; z++)
                            {
                                MGame.Instance.SetVoxel_Q(new Vector3(x, y, z) + (Vector3.Floor(hitTile / 4) * 4), 0);
                            }
                        }
                    }
                }
            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed && voxelHit && autoDigTime <= 0)
            {
                BoundingBox placeBox = new BoundingBox(prevHitTile - position+Vector3.One*0.1f, prevHitTile + Vector3.One*3.9f-position);
                if (placeBox.Contains(bounds) == ContainmentType.Disjoint)
                {
                    autoDigTime = 0.25f;
                    for (int x = 0; x < 4; x++)
                    {
                        for (int z = 0; z < 4; z++)
                        {
                            for (int y = 0; y < 4; y++)
                            {
                                MGame.Instance.SetVoxel_Q(new Vector3(x, y, z) + prevHitTile, 10);
                            }
                        }
                    }
                }
            }
            if (Mouse.GetState().RightButton == ButtonState.Released && Mouse.GetState().LeftButton == ButtonState.Released) autoDigTime = 0f;

            applyVelocity(wishDir);
            base.Update();
        }
        void applyVelocity(Vector3 wishDir)
        {
            float speed = new Vector2(velocity.X, velocity.Z).Length();
            if (speed != 0 && (grounded||swimming))
            {
                float drop = speed * (swimming? 2f : 12f) * MGame.dt;
                velocity *= MathF.Max(speed - drop, 0) / speed;
            }

            float multiplier = 1;
            float len = new Vector2(wishDir.X, wishDir.Z).Length();
            if (len > 1)
            {
                multiplier = (1) / len;
            }
            wishDir *= multiplier;

            float curSpeed = Vector3.Dot(wishDir, velocity);
            float addSpeed = MathHelper.Clamp((grounded||swimming? curwalkspeed*10 : 10f) - curSpeed, 0, float.MaxValue);

            Vector3 initWish = addSpeed * wishDir;
            velocity += initWish;


            multiplier = 1;
            len = new Vector2(velocity.X, velocity.Z).Length();
            if (len > curwalkspeed*1f)
            {
                multiplier = (curwalkspeed * 1f) / len;
            }
            velocity.X *= multiplier;
            velocity.Z *= multiplier;
        }
        public override void Render()
        {
            if(voxelHit)
            {
                MGame.Instance.GraphicsDevice.SetVertexBuffer(vertBuffer);

                effect.Alpha = 0.6f;
                effect.DiffuseColor = Color.Black.ToVector3();

                effect.Projection = MGame.Instance.projection;
                effect.View = MGame.Instance.view;

                if(Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                {
                    effect.World = Matrix.CreateScale(1) * MGame.Instance.world * Matrix.CreateTranslation(hitTile);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        MGame.Instance.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, voxelBoxVert.Length / 2);
                    }
                }
                else
                {
                    effect.DiffuseColor = Color.Black.ToVector3();

                    effect.World = Matrix.CreateScale(4.01f) * MGame.Instance.world * Matrix.CreateTranslation(Vector3.Floor(hitTile / 4) * 4 - Vector3.One * 0.005f);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        MGame.Instance.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, voxelBoxVert.Length / 2);
                    }
                    effect.DiffuseColor = Color.Black.ToVector3();
                    effect.Alpha = 0.4f;
                    effect.World = Matrix.CreateScale(3.2f) * MGame.Instance.world * Matrix.CreateTranslation(Vector3.Floor(prevHitTile / 4) * 4+Vector3.One*0.4f);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        MGame.Instance.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, voxelBoxVert.Length / 2);
                    }
                }
            }
        }

        public void RenderUI()
        {
            MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, MGame.Instance.GraphicsDevice.Viewport.Bounds.Size.ToVector2() / 2, new Rectangle(20, 0, 9, 9), Color.White, 0f, Vector2.Zero, 2, SpriteEffects.None, 0);

            MGame.Instance.spriteBatch.End();
        }

        public override void Destroyed()
        {
        }
    }
}
