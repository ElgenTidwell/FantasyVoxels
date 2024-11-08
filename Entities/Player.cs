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
        static float walk = 4.8f, run = 7.5f, sneak = 2f, swim = 0.5f;

        MouseState oldState;

        Vector3 forward, right;
        float curwalkspeed = 12;
        float desiredFOV = 75f;

        float autoDigTime = 0f;

        bool running,crouched;


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
        float xsin, ysin;
        float bobTime;
        float bob;

        public override void Start()
        {
            bounds = new BoundingBox(new Vector3(-0.2f,-1.6f,-0.2f), new Vector3(0.2f, 0.2f, 0.2f));

            vertBuffer = new VertexBuffer(MGame.Instance.GraphicsDevice, typeof(VertexPosition), voxelBoxVert.Length, BufferUsage.WriteOnly);
            vertBuffer.SetData(voxelBoxVert);

            effect = new BasicEffect(MGame.Instance.GraphicsDevice);
            effect.TextureEnabled = false;
            effect.LightingEnabled = false;
            effect.FogEnabled = false;
            effect.VertexColorEnabled = false;

            oldState = Mouse.GetState();
        }

        public override void Update()
        {
            if (MathF.Abs(velocity.X + velocity.Z) < 4f) running = false;
            running = (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || running) && !crouched;

            crouched = Keyboard.GetState().IsKeyDown(Keys.LeftShift);

            disallowWalkingOffEdge = crouched;

            float speed = new Vector2(velocity.X, velocity.Z).Length();
            float bobMulti = grounded?speed / run:0;

            bobTime += MGame.dt * ((curwalkspeed / walk-1)*0.6f+1);

            xsin = MathF.Sin(bobTime * 6.8f) * bob;
            ysin = MathF.Sin(bobTime * 6.8f * 2) * bob;

            bob = Maths.MoveTowards(bob, bobMulti*1.2f, MGame.dt * 2);
            bob = Maths.MoveTowards(bob, bobMulti*1.2f, MGame.dt * 2);

            var state = Mouse.GetState();
            rotation.Y += MathHelper.ToRadians(oldState.X - state.X) * 0.1f;
            rotation.X += MathHelper.ToRadians(oldState.Y - state.Y) * 0.1f;

            rotation.X = MathHelper.Clamp(rotation.X, MathHelper.ToRadians(-90f), MathHelper.ToRadians(90f));

            Mouse.SetPosition(MGame.Instance.GraphicsDevice.Viewport.Width / 2, MGame.Instance.GraphicsDevice.Viewport.Height / 2);
            oldState = Mouse.GetState();

            float cameraY = position.Y + MathF.Abs(xsin) * 0.18f;

            var rotmat = Matrix.CreateRotationY(rotation.Y);
            var rotmat_cam = Matrix.CreateRotationX(rotation.X)*Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            right = rotmat.Right;

            MGame.Instance.FOV = Maths.MoveTowards(MGame.Instance.FOV, desiredFOV + (running && MathF.Abs(velocity.X) + MathF.Abs(velocity.Z) > walk ? 5 : 0),MGame.dt*30);
            MGame.Instance.cameraPosition = new Vector3(position.X, cameraY, position.Z);
            MGame.Instance.view =
                Matrix.CreateRotationY(-rotation.Y) *
                Matrix.CreateRotationX(-rotation.X + MathF.Abs(xsin) * 0.004f + velocity.Y*0.0006f) *
                Matrix.CreateRotationZ(-rotation.Z + (xsin) * 0.002f);
            MGame.Instance.world = Matrix.CreateWorld(-(new Vector3(position.X, cameraY, position.Z)), Vector3.Forward, Vector3.Up);

            Vector3 wishDir = Vector3.Zero;
            if (Keyboard.GetState().IsKeyDown(Keys.W)) wishDir += forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.S)) wishDir -= forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.D)) wishDir += right * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.A)) wishDir -= right * MGame.dt;

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && (grounded || swimming))
            {
                gravity = swimming?gravity+28*MGame.dt:6.75f;
                gravity = swimming ? MathF.Min(gravity, 4.5f) : gravity;
                grounded = false;
            }
            if(crouched && swimming)
            {
                gravity = -3f;
            }

            curwalkspeed = (crouched ? sneak : running ? run : walk) * (swimming?swim:1);

            rotmat = Matrix.CreateRotationX(rotation.X)*Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            MGame.Instance.cameraForward = forward;
            right = rotmat.Right;

            voxelHit = Maths.Raycast(position,forward,5,out prevHitTile, out hitTile);

            autoDigTime -= MGame.dt;
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && voxelHit && autoDigTime <= 0)
            {
                autoDigTime = 0.25f;
                MGame.Instance.SetVoxel(hitTile, 0);
            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed && voxelHit && autoDigTime <= 0)
            {
                BoundingBox placeBox = new BoundingBox(prevHitTile - position+Vector3.One*0.1f, prevHitTile + Vector3.One*0.9f-position);
                if (placeBox.Contains(bounds) == ContainmentType.Disjoint)
                {
                    autoDigTime = 0.25f;
                    MGame.Instance.SetVoxel(prevHitTile, 10);
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
            float addSpeed = MathHelper.Clamp((grounded||swimming? curwalkspeed*10 : 15) - curSpeed, 0, float.MaxValue);

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
            if (voxelHit)
            {
                MGame.Instance.GraphicsDevice.SetVertexBuffer(vertBuffer);

                effect.Alpha = 1f;
                effect.DiffuseColor = Color.Black.ToVector3();

                effect.Projection = MGame.Instance.projection;
                effect.View = MGame.Instance.view;

                effect.World = Matrix.CreateScale(1.01f) * MGame.Instance.world * Matrix.CreateTranslation(hitTile-Vector3.One*0.005f);

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    MGame.Instance.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, voxelBoxVert.Length / 2);
                }
            }
        }

        public void RenderUI()
        {
            MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp,blendState:MGame.crosshair);

            MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, MGame.Instance.GraphicsDevice.Viewport.Bounds.Size.ToVector2() / 2, new Rectangle(20, 0, 9, 9), Color.White, 0f, Vector2.Zero, 3, SpriteEffects.None, 0);

            MGame.Instance.spriteBatch.End();
        }

        public override void Destroyed()
        {
        }
    }
}
