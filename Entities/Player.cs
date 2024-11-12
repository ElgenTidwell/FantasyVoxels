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
using FantasyVoxels.ItemManagement;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Newtonsoft.Json.Linq;

namespace FantasyVoxels.Entities
{
    public class Player : Entity
    {
        static float walk = 4.25f, run = 6.8f, sneak = 2f, swim = 0.5f;

        MouseState oldState;

        Vector3 forward, right;
        float curwalkspeed = 12;
        float desiredFOV = 70f;

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

        int activeHotbarSlot;
        int oldScroll;
        ItemContainer hotbar = new ItemContainer(9);
        ItemContainer inventory = new ItemContainer(20);

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

            maxHealth = 20;
            health = maxHealth;

            //hotbar.SetItem(new Item("planks",255),0);
            //hotbar.SetItem(new Item("planks",255),1);
            //hotbar.SetItem(new Item("cobblestone",255),2);
            //hotbar.SetItem(new Item("cobblestone",255),3);
            //hotbar.SetItem(new Item("wood",255),4);
            //hotbar.SetItem(new Item("wood",255),5);
            //hotbar.SetItem(new Item("stone",255),6);
            //hotbar.SetItem(new Item("grass",255),7);
            //hotbar.SetItem(new Item("lamp",255),8);
        }

        public override void Update()
        {
            if (MathF.Abs(velocity.X + velocity.Z) < 4f) running = false;
            running = (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || running) && !crouched;

            crouched = Keyboard.GetState().IsKeyDown(Keys.LeftShift);

            disallowWalkingOffEdge = crouched;

            float speed = new Vector2(velocity.X, velocity.Z).Length();
            float bobMulti = grounded?speed / run:0;

            bobTime += MGame.dt * ((curwalkspeed / walk-1)*0.3f+1);

            xsin = MathF.Sin(bobTime * 8.1f) * bob;
            ysin = MathF.Sin(bobTime * 8.1f * 2) * bob;

            bob = Maths.MoveTowards(bob, bobMulti*1.2f, MGame.dt * 20 * float.Abs(bob-bobMulti*1.2f));

            var state = Mouse.GetState();
            rotation.Y += MathHelper.ToRadians(oldState.X - state.X) * 0.1f;
            rotation.X += MathHelper.ToRadians(oldState.Y - state.Y) * 0.1f;

            rotation.X = MathHelper.Clamp(rotation.X, MathHelper.ToRadians(-90f), MathHelper.ToRadians(90f));

            Mouse.SetPosition(MGame.Instance.GraphicsDevice.Viewport.Width / 2, MGame.Instance.GraphicsDevice.Viewport.Height / 2);
            oldState = Mouse.GetState();

            float cameraY = (float)(position.Y + MathF.Abs(xsin) * 0.18f);

            var rotmat = Matrix.CreateRotationY(rotation.Y);
            var rotmat_cam = Matrix.CreateRotationX(rotation.X)*Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            right = rotmat.Right;

            MGame.Instance.FOV = Maths.MoveTowards(MGame.Instance.FOV, desiredFOV + (running && MathF.Abs(velocity.X) + MathF.Abs(velocity.Z) > walk ? 5 : 0),MGame.dt*30);
            MGame.Instance.cameraPosition = new Vector3((float)position.X, cameraY, (float)position.Z);
            MGame.Instance.view =
                Matrix.CreateRotationY(-rotation.Y) *
                Matrix.CreateRotationX(-rotation.X + MathF.Abs(xsin) * 0.004f + velocity.Y*0.0006f) *
                Matrix.CreateRotationZ(-rotation.Z + (xsin) * 0.002f);
            MGame.Instance.world = Matrix.CreateWorld(-(new Vector3((float)position.X, cameraY, (float)position.Z)), Vector3.Forward, Vector3.Up);

            Vector3 wishDir = Vector3.Zero;
            if (Keyboard.GetState().IsKeyDown(Keys.W)) wishDir += forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.S)) wishDir -= forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.D)) wishDir += right * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.A)) wishDir -= right * MGame.dt;

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && (grounded || swimming))
            {
                gravity = swimming ? 4 : 6.75f;
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

            voxelHit = Maths.Raycast((Vector3)position,forward,5,out prevHitTile, out hitTile);

            autoDigTime -= MGame.dt;
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && voxelHit && autoDigTime <= 0)
            {
                autoDigTime = 0.25f;

                int vID = MGame.Instance.GrabVoxel(hitTile);

                if(vID > 0)
                {
                    if(Voxel.voxelTypes[vID].droppedItemID >= 0) hotbar.AddItem(new Item { itemID = Voxel.voxelTypes[vID].droppedItemID, stack = 1 });

                    MGame.Instance.SetVoxel(hitTile, 0);
                }
            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed && voxelHit && autoDigTime <= 0)
            {
                int id = hotbar.PeekItem(activeHotbarSlot).itemID;

                if(id != -1)
                {
                    switch (ItemManager.GetItemFromID(id).type)
                    {
                        case ItemType.Block:

                            BoundingBox placeBox = new BoundingBox(prevHitTile - (Vector3)position + Vector3.One * 0.1f, prevHitTile + Vector3.One * 0.9f - (Vector3)position);
                            if (placeBox.Contains(bounds) == ContainmentType.Disjoint)
                            {
                                autoDigTime = 0.25f;
                                MGame.Instance.SetVoxel(prevHitTile, ItemManager.GetItemFromID(id).placement);
                                hotbar.TakeItem(activeHotbarSlot,1);
                            }

                            break;
                    }
                }
            }
            if (Mouse.GetState().RightButton == ButtonState.Released && Mouse.GetState().LeftButton == ButtonState.Released) autoDigTime = 0f;

            int swheeldelta = MathF.Sign(Mouse.GetState().ScrollWheelValue - oldScroll);

            if (swheeldelta > 0) activeHotbarSlot--;
            if (swheeldelta < 0) activeHotbarSlot++;

            if (activeHotbarSlot >= 9) activeHotbarSlot -= 9;
            if (activeHotbarSlot < 0) activeHotbarSlot += 9;

            oldScroll = Mouse.GetState().ScrollWheelValue;

            applyVelocity(wishDir);
            base.Update();
        }
        void applyVelocity(Vector3 wishDir)
        {
            float speed = new Vector2(velocity.X, velocity.Z).Length();
            if (speed != 0)
            {
                float drop = speed * (swimming? 2f : grounded?12f:0.5f) * MGame.dt;
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

                effect.World = Matrix.CreateScale(1.001f) * MGame.Instance.world * Matrix.CreateTranslation(hitTile-Vector3.One*0.0005f);

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

            MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, MGame.Instance.GraphicsDevice.Viewport.Bounds.Size.ToVector2() / 2, new Rectangle(52, 0, 7, 7), Color.White, 0f, Vector2.One * 2, 3, SpriteEffects.None, 0);

            MGame.Instance.spriteBatch.End();


            MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack);

            float hotbarScale = float.Floor(4 * UserInterface.Active.GlobalScale);
            int leftmost = (int)(-4.5f * hotbarScale * 21 + UserInterface.Active.ScreenWidth/2f);

            for(int i = 0; i < 9; i++)
            {
                float horizPos = leftmost + i * hotbarScale * 21;
                
                if(i == activeHotbarSlot)
                {
                    MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, new Vector2(horizPos - 1*hotbarScale, UserInterface.Active.ScreenHeight - 24 * hotbarScale), new Rectangle(22, 0, 30, 30), Color.White, 0, Vector2.Zero, hotbarScale, SpriteEffects.None, 1f);
                }
                MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, new Vector2(horizPos, UserInterface.Active.ScreenHeight - 23 * hotbarScale), new Rectangle(0, 0, 22, 22), Color.White, 0, Vector2.Zero, hotbarScale, SpriteEffects.None, 0f);
                int id = hotbar.PeekItem(i).itemID;
                if (id != -1)
                {
                    var item = ItemManager.GetItemFromID(id);
                    if (item.type == ItemType.Block)
                    {
                        int tex = Voxel.voxelTypes[item.placement].frontTexture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.colors,
                                                        new Vector2(horizPos + 3 * hotbarScale, UserInterface.Active.ScreenHeight - 20 * hotbarScale),
                                                        new Rectangle((tex % 16)*16, (tex / 16)*16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * hotbarScale,
                                                        SpriteEffects.None,
                                                        0.5f);
                    }

                    Vector2 shift = Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(hotbar.PeekItem(i).stack.ToString())*hotbarScale;

                    MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], hotbar.PeekItem(i).stack.ToString(), new Vector2(horizPos + 21 * hotbarScale, UserInterface.Active.ScreenHeight - 0 * hotbarScale)-shift,Color.Black,0f,Vector2.Zero,Vector2.One*(hotbarScale),SpriteEffects.None,0.6f);
                    MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], hotbar.PeekItem(i).stack.ToString(), new Vector2(horizPos + 20 * hotbarScale, UserInterface.Active.ScreenHeight - 1 * hotbarScale)-shift,Color.White,0f,Vector2.Zero,Vector2.One*(hotbarScale),SpriteEffects.None,0.8f);
                }
            }
            float healthFloat = health / 5f;
            for (int h = 0; h < maxHealth/4; h++)
            {
                float horizPos = leftmost + h * hotbarScale * 10;

                MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, new Vector2(horizPos, UserInterface.Active.ScreenHeight - 34 * hotbarScale), new Rectangle(59, 0, 11, 10), Color.White, 0, Vector2.Zero, hotbarScale, SpriteEffects.None, 0f);
            }

            MGame.Instance.spriteBatch.End();
        }

        public override void Destroyed()
        {
        }

        public override object CaptureCustomSaveData()
        {
            return new PlayerSaveData
            {
                hotbar = hotbar.GetAllItems(),
                inventory = inventory.GetAllItems(),
            };
        }

        public override void RestoreCustomSaveData(JObject data)
        {
            PlayerSaveData pData = data.ToObject<PlayerSaveData>();

            hotbar.SetAllItems(pData.hotbar);
            inventory.SetAllItems(pData.inventory);
        }
    }
    public struct PlayerSaveData
    {
        public Item[] hotbar;
        public Item[] inventory;
    }
}
