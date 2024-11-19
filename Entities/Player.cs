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
using System.Collections;
using FantasyVoxels.UI;

namespace FantasyVoxels.Entities
{
    public class Player : Entity
    {
        static float walk = 4.0f, run = 6.8f, sneak = 2f, swim = 0.5f;
        static BoundingBox standingBounds = new BoundingBox(new Vector3(-0.2f, -1.6f, -0.2f), new Vector3(0.2f, 0.2f, 0.2f));
        static BoundingBox crouchedBounds = new BoundingBox(new Vector3(-0.2f, -1.5f, -0.2f), new Vector3(0.2f, 0.2f, 0.2f));

        MouseState oldState;

        Vector3 forward, right;
        float curwalkspeed = 12;
        float desiredFOV = 70f;

        float autoDigTime = 0f;

        bool running,crouched;
        bool deathUIshown = false;
        bool accessingInventory;

        public static VertexPosition[] voxelBoxVert =
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
        public VertexPositionNormalTexture[] breakBoxVert;
        public VertexBuffer vertBuffer;
        BasicEffect effect;

        Vector3 hitTile,prevHitTile;
        (int vox, int x, int y, int z, Voxel.PlacementSettings p) hitVoxel, oldHitVoxel;
        bool voxelHit;
        float xsin, ysin;
        float bobTime;
        float bob;

        float painResponse = 0;

        int activeHotbarSlot,oldHotbarSlot;
        int oldScroll;
        ItemContainer cursor = new ItemContainer(1);
        ItemContainer hotbar = new ItemContainer(9);
        ItemContainer inventory = new ItemContainer(20);
        ItemContainer crafting = new ItemContainer(7);
        Item heldItem => hotbar.PeekItem(activeHotbarSlot);
        Item prevHeldItem;

        Vector3 wishDir;
        Vector3 oldRotation;

        VertexPositionNormalTexture[] heldBlockModel;
        bool handFromBlockColors;
        bool handIsSprite;
        bool drawHand;
        bool regenerateHand;

        float vmswayX;
        float vmswayY;
        float vmoffsetY;

        float diggingTimer;
        float waterDamageTimer;

        HandAnimation currentHandAnimation;
        float handAnimationTimer;

        enum HandAnimation
        {
            None = 0,
            Swing = 1,
        }

        static float[] animationSpeeds = [
            0f,
            5f,
        ];

        public Player()
        {
            maxHealth = 16;
            health = maxHealth;
        }

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

            breakBoxVert = new VertexPositionNormalTexture[6*6];
            for (int i = 0; i < 6; i++)
            {
                breakBoxVert[i * 6 + 0] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), Chunk.uvs[i * 4 + 0]);
                breakBoxVert[i * 6 + 1] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 1], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), Chunk.uvs[i * 4 + 1]);
                breakBoxVert[i * 6 + 2] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), Chunk.uvs[i * 4 + 2]);
                breakBoxVert[i * 6 + 3] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), Chunk.uvs[i * 4 + 0]);
                breakBoxVert[i * 6 + 4] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), Chunk.uvs[i * 4 + 2]);
                breakBoxVert[i * 6 + 5] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 3], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), Chunk.uvs[i * 4 + 3]);
            }

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
        public override void OnTakeDamage(DamageInfo info)
        {
            painResponse = (info.damage-1)*0.5f+1;

            base.OnTakeDamage(info);
        }
        private void HandleInput()
        {
            //running = (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || running) && !crouched;
            crouched = Keyboard.GetState().IsKeyDown(Keys.LeftShift);

            if (!crouched)
            {
                if (bounds == crouchedBounds)
                {
                    bounds = standingBounds;
                    position.Y += float.Abs(standingBounds.Min.Y) - float.Abs(crouchedBounds.Min.Y);
                }
            }
            else
            {
                if (bounds == standingBounds)
                {
                    bounds = crouchedBounds;
                    position.Y -= float.Abs(standingBounds.Min.Y) - float.Abs(crouchedBounds.Min.Y);
                }
            }

            var state = Mouse.GetState();
            rotation.Y += MathHelper.ToRadians(oldState.X - state.X) * 0.1f;
            rotation.X += MathHelper.ToRadians(oldState.Y - state.Y) * 0.1f;

            rotation.X = MathHelper.Clamp(rotation.X, MathHelper.ToRadians(-90f), MathHelper.ToRadians(90f));

            var rotmat = Matrix.CreateRotationY(rotation.Y);
            var rotmat_cam = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            right = rotmat.Right;

            if (Keyboard.GetState().IsKeyDown(Keys.W)) wishDir += forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.S)) wishDir -= forward * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.D)) wishDir += right * MGame.dt;
            if (Keyboard.GetState().IsKeyDown(Keys.A)) wishDir -= right * MGame.dt;

            if(wishDir.Length() > 0) wishDir.Normalize();

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && (grounded || swimming))
            {
                gravity = swimming ? MathF.Min(gravity+28*MGame.dt, 4.5f) : 7f;
                grounded = false;
            }
            if (BetterKeyboard.HasBeenPressed(Keys.Q))
            {
                var item = hotbar.TakeItem(activeHotbarSlot, 1);

                if (item.HasValue)
                {
                    var droppedItem = new DroppedItem(item.Value);
                    droppedItem.position = (Vector3)position + MGame.Instance.cameraForward * 0.6f;
                    droppedItem.velocity = velocity + MGame.Instance.cameraForward * 8;
                    droppedItem.gravity = droppedItem.velocity.Y;

                    EntityManager.SpawnEntity(droppedItem);
                }
            }
            if (crouched && swimming)
            {
                gravity = -3f;
            }

            rotmat = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            MGame.Instance.cameraForward = forward;
            right = rotmat.Right;

            oldHitVoxel = hitVoxel;
            voxelHit = Maths.Raycast((Vector3)position, forward, 5, out prevHitTile, out hitTile, out int hitVoxelType, out var voxelData);
            hitVoxel = (hitVoxelType, (int)hitTile.X, (int)hitTile.Y, (int)hitTile.Z, voxelData.placement);

            autoDigTime -= MGame.dt;
            if (Mouse.GetState().LeftButton == ButtonState.Released) diggingTimer = 0;
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && voxelHit)
            {
                if (autoDigTime <= 0)
                {
                    autoDigTime = 0.2f;

                    SetAnimation(HandAnimation.Swing);

                    Vector3 normal = prevHitTile - hitTile;
                    Vector3 plane = (Vector3.One - new Vector3(float.Abs(normal.X), float.Abs(normal.Y), float.Abs(normal.Z)));

                    int tex = Voxel.voxelTypes[hitVoxelType].bottomTexture;

                    ParticleSystemManager.AddSystem(new ParticleSystem(4, ParticleSystem.TextureProvider.BlockAtlas, tex, hitTile+normal*0.55f+Vector3.One*0.5f, normal, 0.45f, 12f, plane * 0.5f, Vector3.One));
                }

                ToolPieceProperties toolHead = new ToolPieceProperties { diggingMultiplier = 1 };
                ToolPieceProperties toolHandle = new ToolPieceProperties { diggingMultiplier = 0 };

                if (hotbar.PeekItem(activeHotbarSlot).itemID == -2 && hotbar.PeekItem(activeHotbarSlot).properties is ToolProperties)
                {
                    var tool = (ToolProperties)hotbar.PeekItem(activeHotbarSlot).properties;
                    toolHead = (ToolPieceProperties)ItemManager.GetItemFromID(tool.toolHead).properties;
                    toolHandle = (ToolPieceProperties)ItemManager.GetItemFromID(tool.toolHandle).properties;
                }

                //if its not the same voxel, reset the timer
                if (hitVoxel != oldHitVoxel)
                {
                    diggingTimer = 0;
                }

                diggingTimer += MGame.dt * (Voxel.voxelTypes[hitVoxel.vox].materialType == toolHead.meantFor ? toolHead.diggingMultiplier+toolHandle.diggingMultiplier : 1);

                if(diggingTimer >= Voxel.voxelTypes[hitVoxel.vox].baseDigTime)
                {
                    diggingTimer = 0;

                    int vID = MGame.Instance.GrabVoxel(hitTile);

                    if (vID > 0)
                    {
                        //TODO: tool levels
                        MGame.Instance.DigVoxel(hitTile, Voxel.voxelTypes[hitVoxel.vox].materialType == toolHead.meantFor? toolHead.toolPieceLevel:0);
                        if (hotbar.UseItem(activeHotbarSlot) && hotbar.PeekItem(activeHotbarSlot).properties is ToolProperties prop)
                        {
                            ParticleSystemManager.AddSystem(new ParticleSystem(10,ParticleSystem.TextureProvider.ItemAtlas,ItemManager.GetItemFromID(prop.toolHead).texture,position+MGame.Instance.cameraForward*0.5f,Vector3.Up,1f,12f,Vector3.One*0.1f,Vector3.One*2));
                        }
                    }
                }
            } 
            else if(BetterMouse.WasLeftPressed())
            {
                SetAnimation(HandAnimation.Swing);
            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed && voxelHit && autoDigTime <= 0)
            {
                int id = hotbar.PeekItem(activeHotbarSlot).itemID;

                if (id != -1)
                {
                    void testitemtype(ItemType type)
                    {
                        switch (type)
                        {
                            case ItemType.Block:

                                BoundingBox placeBox = new BoundingBox(prevHitTile - (Vector3)position + Vector3.One * 0.001f, prevHitTile + Vector3.One * 0.999f - (Vector3)position);

                                int v = MGame.Instance.GrabVoxel(prevHitTile);
                                if (v > 0 && Voxel.voxelTypes[v].ignoreCollision)
                                {
                                    MGame.Instance.SetVoxel(prevHitTile, 0);
                                }

                                Vector3 place = hitTile - prevHitTile;
                                Voxel.PlacementSettings placement = Voxel.PlacementSettings.ANY;
                                if (place.X > 0) placement = Voxel.PlacementSettings.RIGHT;
                                if (place.X < 0) placement = Voxel.PlacementSettings.LEFT;
                                if (place.Y > 0) placement = Voxel.PlacementSettings.TOP;
                                if (place.Y < 0) placement = Voxel.PlacementSettings.BOTTOM;
                                if (place.Z > 0) placement = Voxel.PlacementSettings.FRONT;
                                if (place.Z < 0) placement = Voxel.PlacementSettings.BACK;

                                if ((placeBox.Contains(bounds) == ContainmentType.Disjoint || Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].ignoreCollision)
                                     && Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].AllowsPlacement(placement)
                                     && !(Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].myClass != null && !Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].myClass.CanPlace(placement, hitVoxelType)))
                                {
                                    autoDigTime = 0.25f;

                                    MGame.Instance.SetVoxel(prevHitTile, ItemManager.GetItemFromID(id).placement, placement: placement);
                                    hotbar.TakeItem(activeHotbarSlot, 1);
                                    SetAnimation(HandAnimation.Swing);
                                }

                                break;
                            default:



                                break;
                        }
                    }
                    if (id >= 0) testitemtype(ItemManager.GetItemFromID(id).type);
                }
            }
            if (Mouse.GetState().RightButton == ButtonState.Released && Mouse.GetState().LeftButton == ButtonState.Released) autoDigTime = 0f;

            int swheeldelta = MathF.Sign(Mouse.GetState().ScrollWheelValue - oldScroll);

            if (swheeldelta > 0) activeHotbarSlot--;
            if (swheeldelta < 0) activeHotbarSlot++;

            if (activeHotbarSlot >= 9) activeHotbarSlot -= 9;
            if (activeHotbarSlot < 0) activeHotbarSlot += 9;

            oldScroll = Mouse.GetState().ScrollWheelValue;

            Mouse.SetPosition(MGame.Instance.GraphicsDevice.Viewport.Width / 2, MGame.Instance.GraphicsDevice.Viewport.Height / 2);
            oldState = Mouse.GetState();
        }
        public override void Die()
        {
            deathUIshown = true;

            var forward = MGame.Instance.cameraForward;
            MGame.Instance.cameraForward = Vector3.Up;
            SpitContents(ref inventory, true);
            SpitContents(ref hotbar,true);
            MGame.Instance.cameraForward = forward;

            var respawnButton = new Button("Respawn",ButtonSkin.Alternative,Anchor.Center, new Vector2(800, 70));
            var quitButton = new Button("Quit to Menu", ButtonSkin.Alternative, Anchor.AutoCenter, new Vector2(800, 70));

            MGame.Instance.IsMouseVisible = true;

            UserInterface.Active.AddEntity(respawnButton);
            UserInterface.Active.AddEntity(quitButton);

            respawnButton.OnClick += (GeonBit.UI.Entities.Entity entity) =>
            {
                UserInterface.Active.RemoveEntity(respawnButton);
                UserInterface.Active.RemoveEntity(quitButton);

                this.health = 16;
                this.maxHealth = 16;
                this.position = MGame.Instance.worldSpawnpoint;

                MGame.Instance.IsMouseVisible = false;
            };
            quitButton.OnClick += (GeonBit.UI.Entities.Entity entity) =>
            {
                UserInterface.Active.RemoveEntity(respawnButton);
                UserInterface.Active.RemoveEntity(quitButton);

                PauseMenu.QuitWorld();
            };
        }
        public override void Update()
        {
            if (health <= 0 && !deathUIshown) Die();

            if (health > 0)
            {
                if (MathF.Abs(velocity.X + velocity.Z) < 4f) running = false;
                disallowWalkingOffEdge = crouched;

                float speed = new Vector2(velocity.X, velocity.Z).Length();
                float bobMulti = grounded ? speed / run : 0;

                bobTime += MGame.dt * ((speed / walk - 1) * 0.3f + 1);

                xsin = MathF.Sin(bobTime * 7.5f) * bob;
                ysin = MathF.Sin(bobTime * 7.5f + MathHelper.ToRadians(90)) * bob;

                bob = Maths.MoveTowards(bob, bobMulti * 1.2f, MGame.dt * 12 * float.Abs(bob - bobMulti * 1.2f));

                curwalkspeed = (crouched ? sneak : running ? run : walk) * (swimming ? swim : 1);

                HandleInventory();

                wishDir = Vector3.Zero;
                if (!accessingInventory) HandleInput();
                painResponse = Maths.MoveTowards(painResponse, 0, MGame.dt * 5 * (float.Abs(painResponse) + 0.1f));


                //Water damage
                if(swimming)
                {
                    waterDamageTimer -= MGame.dt;
                    if(waterDamageTimer < 0)
                    {
                        waterDamageTimer = 1;
                        OnTakeDamage(new DamageInfo { damage = 1 });
                    }
                }
            }
            else
            {
                painResponse = Maths.MoveTowards(painResponse, 4, MGame.dt * 5 * (float.Abs(painResponse) + 0.1f));
            }

            vmswayX = Maths.MoveTowards(vmswayX, 0, MGame.dt * 15 * float.Abs(vmswayX));
            vmswayY = Maths.MoveTowards(vmswayY, 0, MGame.dt * 15 * float.Abs(vmswayY));
            vmswayX += (oldRotation.Y - rotation.Y) * 0.2f;
            vmswayY += (oldRotation.X - rotation.X) * 0.2f;

            float cameraY = (float)(position.Y + MathF.Abs(xsin) * 0.16f);

            MGame.Instance.FOV = Maths.MoveTowards(MGame.Instance.FOV, desiredFOV + (running && MathF.Abs(velocity.X) + MathF.Abs(velocity.Z) > walk ? 5 : 0), MGame.dt * 30);
            MGame.Instance.cameraPosition = new Vector3((float)position.X, cameraY, (float)position.Z);
            MGame.Instance.view =
                Matrix.CreateRotationY(-rotation.Y) *
                Matrix.CreateRotationX(-rotation.X + MathF.Abs(xsin) * 0.006f + velocity.Y * 0.0006f) *
                Matrix.CreateRotationZ(-rotation.Z - (ysin) * 0.003f + MathHelper.ToRadians(painResponse * 5));
            MGame.Instance.world = Matrix.CreateWorld(-(new Vector3((float)position.X, cameraY, (float)position.Z)), Vector3.Forward, Vector3.Up);

            vmoffsetY = Maths.MoveTowards(vmoffsetY, 0, MGame.dt * float.Abs(vmoffsetY) * 10);

            //Hand drawing
            if (prevHeldItem.itemID != hotbar.PeekItem(activeHotbarSlot).itemID || prevHeldItem.stack != hotbar.PeekItem(activeHotbarSlot).stack || oldHotbarSlot != activeHotbarSlot)
            {
                regenerateHand = true;
            }

            if (currentHandAnimation != HandAnimation.None)
            {
                handAnimationTimer += MGame.dt * animationSpeeds[(int)currentHandAnimation];

                if (handAnimationTimer >= 1)
                {
                    handAnimationTimer = 0;
                    currentHandAnimation = HandAnimation.None;
                    vmoffsetY = -0.5f;
                }
            }

            if (regenerateHand && currentHandAnimation == HandAnimation.None)
            {
                regenerateHand = false;
                //Regenerate hand model
                int id = hotbar.PeekItem(activeHotbarSlot).itemID;

                vmoffsetY = -0.5f;

                drawHand = false;

                if (id != -1)
                {
                    if(id >= 0)
                    {
                        drawHand = true;
                        bool spr = ItemManager.GetItemFromID(id).type == ItemType.Block?ItemManager.GetItemFromID(id).alwaysRenderAsSprite:true;
                        handIsSprite = spr;
                        handFromBlockColors = ItemManager.GetItemFromID(id).type == ItemType.Block;
                        heldBlockModel = new VertexPositionNormalTexture[(spr ? 6 : 6 * 6)];
                        for (int p = 0; p < (spr ? 1 : 6); p++)
                        {
                            int i = spr ? 1 : p;

                            int tex = 0;
                            if (ItemManager.GetItemFromID(id).type == ItemType.Block)
                            {
                                switch (i)
                                {
                                    case 0: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].rightTexture; break;
                                    case 1: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].leftTexture; break;
                                    case 2: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].topTexture; break;
                                    case 3: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].bottomTexture; break;
                                    case 4: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].frontTexture; break;
                                    case 5: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].backTexture; break;
                                }
                            }
                            else
                            {
                                tex = ItemManager.GetItemFromID(id).texture;
                            }

                            if(!spr)
                            {
                                heldBlockModel[i*6 + 0] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 1] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 1], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 2] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 3] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 4] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 5] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 3], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.AtlasSize);
                            }
                            else
                            {
                                heldBlockModel = GenerateSpriteModel(tex, new Vector3(0.5f,0,0),Vector3.One);
                            }
                        }
                    }
                    else
                    {
                        if(hotbar.PeekItem(activeHotbarSlot).properties is ToolProperties)
                        {
                            int head = ((ToolProperties)hotbar.PeekItem(activeHotbarSlot).properties).toolHead;
                            int handle = ((ToolProperties)hotbar.PeekItem(activeHotbarSlot).properties).toolHandle;

                            drawHand = true;
                            bool spr = true;
                            handIsSprite = spr;
                            handFromBlockColors = false;
                            heldBlockModel = new VertexPositionNormalTexture[12];

                            int i = 1;

                            int tex = 0;
                            tex = ItemManager.GetItemFromID(head).texture;

                            List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();

                            verts.AddRange(GenerateSpriteModel(tex, new Vector3(0.5f, 0, 0), Vector3.One));

                            tex = ItemManager.GetItemFromID(handle).texture;

                            verts.AddRange(GenerateSpriteModel(tex, new Vector3(0.5f+0.01f, 0, 0), Vector3.One));

                            heldBlockModel = verts.ToArray();
                        }
                    }
                }
            }
            prevHeldItem = hotbar.PeekItem(activeHotbarSlot);
            oldHotbarSlot = activeHotbarSlot;
            oldRotation = rotation;

            applyVelocity(wishDir);
            base.Update();
        }

        VertexPositionNormalTexture[] GenerateSpriteModel(int tex,Vector3 offset,Vector3 scale)
        {
            List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();

            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+0]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+1]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+1] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+2]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+0]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+2]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+3]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+3] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));

            for(int x = 0; x < 16; x++)
            {
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4*4 + 0]*scale*new Vector3(1/16f,1,1) + offset - Vector3.UnitZ*x/16f, Vector3.UnitZ, (Chunk.uvs[4*4 + 0] * new Vector2(1,16) + new Vector2(15-x,0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4*4 + 1]*scale*new Vector3(1/16f,1,1) + offset - Vector3.UnitZ*x/16f, Vector3.UnitZ, (Chunk.uvs[4*4 + 1] * new Vector2(1,16) + new Vector2(15-x,0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4*4 + 2]*scale*new Vector3(1/16f,1,1) + offset - Vector3.UnitZ*x/16f, Vector3.UnitZ, (Chunk.uvs[4*4 + 2] * new Vector2(1,16) + new Vector2(15-x,0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4*4 + 0]*scale*new Vector3(1/16f,1,1) + offset - Vector3.UnitZ*x/16f, Vector3.UnitZ, (Chunk.uvs[4*4 + 0] * new Vector2(1,16) + new Vector2(15-x,0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4*4 + 2]*scale*new Vector3(1/16f,1,1) + offset - Vector3.UnitZ*x/16f, Vector3.UnitZ, (Chunk.uvs[4*4 + 2] * new Vector2(1,16) + new Vector2(15-x,0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4*4 + 3]*scale*new Vector3(1/16f,1,1) + offset - Vector3.UnitZ*x/16f, Vector3.UnitZ, (Chunk.uvs[4*4 + 3] * new Vector2(1,16) + new Vector2(15-x,0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));

                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 0]*scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 0] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 1]*scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 1] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 2]*scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 2] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 0]*scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 0] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 2]*scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 2] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 3]*scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 3] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
            }

            for (int y = 0; y < 16; y++)
            {
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 0].X,Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 1] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 1].X,Chunk.uvs[2 * 4 + 1].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 2].X,Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 0].X,Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 2].X,Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 3] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 3].X,Chunk.uvs[2 * 4 + 3].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));

                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, -Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 0].X,Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 1] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, -Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 1].X,Chunk.uvs[2 * 4 + 1].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, -Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 2].X,Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, -Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 0].X,Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, -Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 2].X,Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 3] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y/ 16f, -Vector3.UnitY, (new Vector2(1-Chunk.uvs[2 * 4 + 3].X,Chunk.uvs[2 * 4 + 3].Y) * new Vector2(16, 1) + new Vector2(0, 15-y) + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / MGame.ItemAtlasSize));
            }

            return verts.ToArray();
        }

        private void SetAnimation(HandAnimation handAnimation)
        {
            currentHandAnimation = handAnimation;
            handAnimationTimer = 0;
        }
        private void HandleInventory()
        {
            if (BetterKeyboard.HasBeenPressed(Keys.E))
            {
                if (!ExitOtherMenus()) { accessingInventory = true; MGame.Instance.IsMouseVisible = true; }
            }

            if (!accessingInventory) return;
        }
        public bool PickupItem(Item item)
        {
            if (health <= 0) return false;

            if(!hotbar.AddItem(item, out int leftover))
            {
                return inventory.AddItem(item, out leftover);
            }
            else
            {
                return true;
            }
        }
        void applyVelocity(Vector3 wishDir)
        {
            float speed = new Vector2(velocity.X, velocity.Z).Length();
            if (speed != 0)
            {
                float drop = speed * (swimming ? 2f : grounded ? 12 : 1) * MGame.dt;
                velocity *= MathF.Max(speed - drop, 0) / speed;
            }

            float curSpeed = new Vector2(velocity.X, velocity.Z).Length();
            float addSpeed = MathHelper.Clamp(curwalkspeed - curSpeed, 0, (grounded || swimming ? 200 : 10)*MGame.dt);

            Vector3 initWish = addSpeed * wishDir;
            velocity += initWish;

            float multiplier = 1;
            float len = new Vector2(velocity.X, velocity.Z).Length();
            if (len > curwalkspeed*1.5f)
            {
                multiplier = (curwalkspeed * 1.5f) / len;
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

                if(hitVoxel.vox >= 0 && Voxel.voxelTypes[hitVoxel.vox].myClass != null && Voxel.voxelTypes[hitVoxel.vox].myClass.customBounds)
                {
                    var bounds = Voxel.voxelTypes[hitVoxel.vox].myClass.GetCustomBounds(hitVoxel.p);

                    effect.World = Matrix.CreateScale(bounds.Max-bounds.Min+0.001f*Vector3.One) * MGame.Instance.world * Matrix.CreateTranslation((hitTile + bounds.Min) - Vector3.One * 0.0005f);
                }

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    MGame.Instance.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, voxelBoxVert.Length / 2);
                }

                if(diggingTimer > 0)
                {
                    var breaking = MGame.Instance.GetBreakingShader();
                    breaking.Parameters["World"].SetValue(effect.World);
                    breaking.Parameters["View"].SetValue(effect.View);
                    breaking.Parameters["Projection"].SetValue(effect.Projection);

                    breaking.Parameters["mainTexture"].SetValue(MGame.Instance.breaking);

                    breaking.Parameters["frame"].SetValue((int)float.Floor((diggingTimer / Voxel.voxelTypes[hitVoxel.vox].baseDigTime) *8));

                    foreach (var pass in breaking.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        MGame.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, breakBoxVert, 0, breakBoxVert.Length / 3);
                    }
                }
            }
        }
        public void RenderViewmodel()
        {
            if (!drawHand || heldBlockModel.Length < 3) return;

            MGame.Instance.GrabVoxelData((Vector3)position, out var voxelData);

            float ourLight = (voxelData.skyLight / 255f) * MGame.Instance.daylightPercentage;
            MGame.Instance.GetEntityShader().Parameters["tint"].SetValue(ourLight);
            MGame.Instance.GetEntityShader().Parameters["blocklightTint"].SetValue(voxelData.blockLight / 255f);

            MGame.Instance.GetEntityShader().Parameters["mainTexture"].SetValue(handFromBlockColors? MGame.Instance.colors : MGame.Instance.items);

            Matrix animMatrix = Matrix.Identity;

            if (handIsSprite)
            {
                animMatrix =
                           Matrix.CreateRotationY(MathHelper.ToRadians(-25)) *
                           Matrix.CreateRotationX(MathHelper.ToRadians(-25)) *
                           Matrix.CreateTranslation(0, -0.2f, 0.1f);
            }

            switch (currentHandAnimation)
            {
                case HandAnimation.Swing:

                    animMatrix *= Matrix.CreateTranslation(0, 0, -float.Pow(handAnimationTimer,2f)*2-0.1f)*
                                 Matrix.CreateRotationX(MathHelper.ToRadians(float.Pow(handAnimationTimer, 0.8f) * -125 + 15))*
                                 Matrix.CreateRotationY(MathHelper.ToRadians(float.Pow(handAnimationTimer, 0.8f) * 25));

                    break;
            }

            Matrix world = Matrix.CreateTranslation(Vector3.One*0.5f)*
                           Matrix.CreateScale(handIsSprite? 0.8f: 0.6f) *
                           Matrix.CreateRotationY(MathHelper.ToRadians(20))*
                           Matrix.CreateRotationX(MathHelper.ToRadians(0))*
                           animMatrix *
                           Matrix.CreateWorld(new Vector3(0.2f+ ysin * 0.15f*bob, -1.4f-(((float.Pow(float.Abs(xsin),0.8f) * 1.5f)) * bob) * 0.15f + vmoffsetY, -1.6f), Vector3.Forward, Vector3.Up)*
                           Matrix.CreateRotationX(MathHelper.ToRadians(velocity.Y * -0.2f)+ vmswayY) *
                           Matrix.CreateRotationY(vmswayX) *
                           Matrix.CreateRotationZ(painResponse*0.01f);

            MGame.Instance.GetEntityShader().Parameters["World"].SetValue(world);
            MGame.Instance.GetEntityShader().Parameters["View"].SetValue(Matrix.Identity);

            foreach (var pass in MGame.Instance.GetEntityShader().CurrentTechnique.Passes)
            {
                pass.Apply();
                MGame.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, heldBlockModel, 0, heldBlockModel.Length / 3);
            }
        }

        public void RenderUI()
        {
            MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp,blendState:MGame.crosshair);

            MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, MGame.Instance.GraphicsDevice.Viewport.Bounds.Size.ToVector2() / 2, new Rectangle(52, 0, 7, 7), Color.White, 0f, Vector2.One * 2, 3, SpriteEffects.None, 0);

            MGame.Instance.spriteBatch.End();

            MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack);

            float uiScale = float.Floor(4 * UserInterface.Active.GlobalScale);
            int leftmost = (int)(-4.5f * uiScale * 21 + UserInterface.Active.ScreenWidth/2f);

            //hotbar and health
            for(int i = 0; i < 9; i++)
            {
                float horizPos = leftmost + i * uiScale * 21;
                
                if(i == activeHotbarSlot)
                {
                    MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, new Vector2(horizPos - 1*uiScale, UserInterface.Active.ScreenHeight - 24 * uiScale), new Rectangle(22, 0, 30, 30), Color.White, 0, Vector2.Zero, uiScale, SpriteEffects.None, 1f);
                }
                MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, new Vector2(horizPos, UserInterface.Active.ScreenHeight - 23 * uiScale), new Rectangle(0, 0, 22, 22), Color.White, 0, Vector2.Zero, uiScale, SpriteEffects.None, 0f);
                int id = hotbar.PeekItem(i).itemID;
                if (id != -1)
                {
                    DrawItem(new Vector2(horizPos,UserInterface.Active.ScreenHeight),uiScale,id,hotbar.PeekItem(i).stack, hotbar.PeekItem(i));
                }
            }
            float healthFloat = health / 4f;
            for (int h = 0; h < maxHealth/4; h++)
            {
                float horizPos = leftmost + h * uiScale * 10;

                int index = health - (h * 4+4);

                index = -int.Clamp(index,-4,0);

                int bounce = health == 0?0 : (int)(float.Max((float.Sin(MGame.totalTime * 16 + h * 1)), 0) * (maxHealth / health) * 0.5f);

                MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, new Vector2(horizPos, UserInterface.Active.ScreenHeight - 34 * uiScale - bounce*uiScale), new Rectangle(59+ index*11, 0, 11, 10), Color.White, 0, Vector2.Zero, uiScale, SpriteEffects.None, 0f);
            }
            MGame.Instance.spriteBatch.End();

            if (accessingInventory)
            {
                MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack);

                //black background
                MGame.Instance.spriteBatch.Draw(MGame.Instance.white, MGame.Instance.GraphicsDevice.Viewport.Bounds, null, new Color(Color.Black, 200), 0f, Vector2.Zero, SpriteEffects.None, 0.05f);

                int scale = (int)(uiScale * 1);
                int backpackX = (int)(-103 * scale + UserInterface.Active.ScreenWidth / 2f);
                int backpackY = (int)(60 * scale + UserInterface.Active.ScreenHeight / 2f);
                MGame.Instance.spriteBatch.Draw(MGame.Instance.uiBackback, new Vector2(backpackX, backpackY - 142*scale), new Rectangle(0, 0, 206, 142), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.1f);

                void RenderTooltip(Item item)
                {
                    if (item.itemID != -1)
                    {
                        string displayname = item.itemID >= 0 ? ItemManager.GetItemFromID(item.itemID).displayName :
                                                                ItemManager.GetItemFromID(((ToolProperties)item.properties).toolHead).displayName.TrimEnd("Tool-Head".ToCharArray()).Trim();

                        StringBuilder extras = new StringBuilder();

                        if(item.itemID == -2)
                        {
                            if (item.properties is ToolProperties prop)
                            {
                                extras.AppendLine($"Fabricated with:");
                                extras.AppendLine($"{ItemManager.GetItemFromID(prop.toolHead).displayName} + {ItemManager.GetItemFromID(prop.toolHandle).displayName}");
                                extras.AppendLine($"Tool level: {((ToolPieceProperties)ItemManager.GetItemFromID(prop.toolHead).properties).toolPieceLevel}");
                                extras.AppendLine($"Breaks: {((ToolPieceProperties)ItemManager.GetItemFromID(prop.toolHead).properties).meantFor}");
                                extras.AppendLine($"Durability: {prop.durability}/{prop.maxDurability}");
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(ItemManager.GetItemFromID(item.itemID).description)) extras.AppendLine(ItemManager.GetItemFromID(item.itemID).description);

                            if (ItemManager.GetItemFromID(item.itemID).properties is ToolPieceProperties prop)
                            {
                                if(prop.slot == ToolPieceSlot.Head)
                                {
                                    extras.AppendLine($"Tool level: {prop.toolPieceLevel}");
                                    extras.AppendLine($"Breaks: {prop.meantFor}");
                                }
                                extras.AppendLine($"Speculative Durability: {prop.durability}");
                            }
                        }

                        string extraInfo = UIUtils.WrapText(Resources.Instance.Fonts[(int)FontStyle.Regular], extras.ToString(), 200);

                        Vector2 size = (Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(displayname) + Vector2.One * 2) * scale;

                        if(!string.IsNullOrWhiteSpace(extraInfo)) size = Vector2.Max((Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(extraInfo) + new Vector2(2,32)) * (scale/2f),size);

                        Vector2 pos = (Vector2.Floor((Mouse.GetState().Position.ToVector2()) / scale)) * scale + new Vector2(8, 8)*scale;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.white, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X + scale * 5, (int)size.Y), null, Color.Black, 0f, Vector2.Zero, SpriteEffects.None, 0.99f);
                        MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], displayname, pos + Vector2.UnitX * scale * 4, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                        if (!string.IsNullOrWhiteSpace(extraInfo)) MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], extraInfo, pos + Vector2.UnitX * scale * 4 + Vector2.UnitY * scale * 16, Color.Yellow, 0f, Vector2.Zero, scale/2, SpriteEffects.None, 1f);
                    }
                }

                //hotbar
                for (int i = 0; i < 9; i++)
                {
                    var slotbounds = new Rectangle(backpackX + 10 * scale + i * scale * 21, backpackY - 6 * scale - scale * 21, scale * 18, scale * 18);
                    bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                    int id = hotbar.PeekItem(i).itemID;
                    if (id != -1)
                    {
                        DrawItem(new Vector2(backpackX+8 * scale + i * scale * 21, backpackY-6 * scale), scale, id, hotbar.PeekItem(i).stack, hotbar.PeekItem(i));
                    }

                    if (isSelected)
                    {
                        RenderTooltip(hotbar.PeekItem(i));

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                        TryTransferContainerToCursor(ref hotbar, i);
                    }
                }

                //inventory
                for (int i = 0; i < 20; i++)
                {
                    var slotbounds = new Rectangle(backpackX + 10 * scale + (i % 5) * scale * 21, backpackY - 111 * scale + (i / 5) * scale * 21 - scale * 21, scale * 18, scale * 18);
                    bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                    int id = inventory.PeekItem(i).itemID;
                    if (id != -1)
                    {
                        DrawItem(new Vector2(backpackX + 8 * scale + (i%5) * scale * 21, backpackY - 111 * scale + (i/5)*scale*21), scale, id, inventory.PeekItem(i).stack, inventory.PeekItem(i));
                    }

                    if (isSelected)
                    {
                        RenderTooltip(inventory.PeekItem(i));

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                        TryTransferContainerToCursor(ref inventory, i);
                    }
                }

                //crafting
                for (int i = 0; i < 7; i++)
                {
                    int x = backpackX + 134 * scale + (i < 6 ? i % 3 : 1) * scale * 21;
                    int y = backpackY - 111 * scale + (i < 6 ? (i / 3) : 3) * scale * 21;

                    var slotbounds = new Rectangle(x + 2*scale, y - scale * 21, scale * 18, scale * 18);
                    bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                    int id = crafting.PeekItem(i).itemID;
                    if (id != -1)
                    {
                        DrawItem(new Vector2(x, y), scale, id, crafting.PeekItem(i).stack, crafting.PeekItem(i));
                    }

                    if (isSelected)
                    {
                        RenderTooltip(crafting.PeekItem(i));

                        if (i < 6) TryTransferContainerToCursor(ref crafting, i);
                        else
                        {
                            if (BetterMouse.WasLeftPressed())
                            {
                                var add = crafting.PeekItem(i);
                                if (add.itemID != -1 && cursor.TestAddItem(add, 0))
                                {
                                    cursor.AddItem(add, 0, out int remainder);

                                    for(i = 0; i < 6; i++)
                                    {
                                        crafting.TakeItem(i,1);
                                    }
                                    if(remainder > 0)
                                    {
                                        crafting.SetItem(new Item { itemID = add.itemID, stack = (byte)remainder},i);
                                    }
                                    else
                                    {
                                        crafting.TakeItemStack(i);
                                    }
                                }
                            }
                        }

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                    }

                    if(i == 6)
                    {
                        crafting.SetItem(CraftingManager.TryCraft(crafting),i);
                    }
                }
                //clicked outside the window
                if (BetterMouse.WasLeftPressed() && !new Rectangle(backpackX, backpackY - 142 * scale, 206*scale, 142*scale).Contains(Mouse.GetState().Position))
                {
                    SpitContents(ref cursor);
                }

                if (cursor.PeekItem(0).itemID != -1)
                {
                    DrawItem(Vector2.Floor(Mouse.GetState().Position.ToVector2() / scale) * scale + new Vector2(-12,12) * scale, scale, cursor.PeekItem(0).itemID, cursor.PeekItem(0).stack,cursor.PeekItem(0),0.9f);
                }

                MGame.Instance.spriteBatch.End();
            }
        }
        void TryTransferContainerToCursor(ref ItemContainer container, int i, bool noplace = false)
        {
            if (BetterMouse.WasLeftPressed())
            {
                if (cursor.PeekItem(0).itemID != -1 && container.TestAddItem(cursor.PeekItem(0), i) && !noplace)
                {
                    container.AddItem(cursor.PeekItem(0), i, out int leftover);

                    if (leftover == 0) cursor.SetItem(new Item { itemID = -1, stack = 0 }, 0);
                    else cursor.SetItem(new Item { itemID = cursor.PeekItem(0).itemID, stack = (byte)leftover }, 0);
                }
                else if (container.PeekItem(i).itemID != -1)
                {
                    Item copy = new Item { itemID = -1, stack = 0 };
                    if (cursor.PeekItem(0).itemID != -1)
                    {
                        copy = cursor.PeekItem(0);
                    }

                    cursor.SetItem(container.TakeItemStack(i), 0);

                    container.SetItem(copy, i);
                }
            }
            else
            if (BetterMouse.WasRightPressed())
            {
                if (cursor.PeekItem(0).itemID != -1 && container.TestAddItem(new Item { itemID = cursor.PeekItem(0).itemID, stack = 1 }, i) && !noplace)
                {
                    container.AddItem(cursor.TakeItem(0, 1).Value, i, out int leftover);
                }
                else if (container.PeekItem(i).itemID != -1 && (cursor.PeekItem(0).itemID == container.PeekItem(i).itemID || cursor.PeekItem(0).itemID != -1))
                {
                    cursor.SetItem(container.TakeItem(i, (byte)float.Ceiling(container.PeekItem(i).stack / 2f)).Value, 0);
                }
            }
        }

        private void DrawItem(Vector2 pos, float uiScale, int id, int stack, Item bitem, float depth = 0.5f)
        {
            //Compound item
            if(id == -2)
            {
                if(bitem.properties is ToolProperties)
                {
                    var item = ItemManager.GetItemFromID(((ToolProperties)bitem.properties).toolHandle);
                    if (item.type == ItemType.Block)
                    {
                        int tex = Voxel.voxelTypes[item.placement].frontTexture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.colors,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((tex % 16) * 16, (tex / 16) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth);
                    }
                    else
                    {
                        int tex = item.texture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.items,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((tex % 16) * 16, (tex / 16) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth);
                    }
                    item = ItemManager.GetItemFromID(((ToolProperties)bitem.properties).toolHead);
                    if (item.type == ItemType.Block)
                    {
                        int tex = Voxel.voxelTypes[item.placement].frontTexture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.colors,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((tex % 16) * 16, (tex / 16) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth + 0.001f);
                    }
                    else
                    {
                        int tex = item.texture;

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.items,
                                                        new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                        new Rectangle((tex % 16) * 16, (tex / 16) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                        Vector2.One * uiScale,
                                                        SpriteEffects.None,
                                                        depth+0.001f);
                    }
                }
            }
            else
            {
                var item = ItemManager.GetItemFromID(id);
                if (item.type == ItemType.Block)
                {
                    int tex = Voxel.voxelTypes[item.placement].frontTexture;

                    MGame.Instance.spriteBatch.Draw(MGame.Instance.colors,
                                                    new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                    new Rectangle((tex % 16) * 16, (tex / 16) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                    Vector2.One * uiScale,
                                                    SpriteEffects.None,
                                                    depth);
                }
                else
                {
                    int tex = item.texture;

                    MGame.Instance.spriteBatch.Draw(MGame.Instance.items,
                                                    new Vector2(pos.X + 3 * uiScale, pos.Y - 20 * uiScale),
                                                    new Rectangle((tex % 16) * 16, (tex / 16) * 16, 16, 16), Color.White, 0, Vector2.Zero,
                                                    Vector2.One * uiScale,
                                                    SpriteEffects.None,
                                                    depth);
                }

                if (stack <= 1) return;

                Vector2 shift = Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(stack.ToString()) * uiScale;

                MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], stack.ToString(), new Vector2(pos.X + 21 * uiScale, pos.Y - 0 * uiScale) - shift, Color.Black, 0f, Vector2.Zero, Vector2.One * (uiScale), SpriteEffects.None, depth + 0.01f);
                MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], stack.ToString(), new Vector2(pos.X + 20 * uiScale, pos.Y - 1 * uiScale) - shift, Color.White, 0f, Vector2.Zero, Vector2.One * (uiScale), SpriteEffects.None, depth + 0.02f);
            }
        }
        void SpitContents(ref ItemContainer container, bool random = false)
        {
            for(int j = 0; j < container.GetAllItems().Length; j++)
            {
                if (container.PeekItem(j).stack > 0)
                {
                    var item = container.TakeItemStack(j);
                    container.SetItem(new Item { itemID = -1, stack = 0 },j);

                    if (item.itemID != -1 && item.stack != 0)
                    {
                        for (int i = 0; i < item.stack; i++)
                        {
                            var droppedItem = new DroppedItem(item);
                            droppedItem.position = (Vector3)position + MGame.Instance.cameraForward * 0.6f;
                            droppedItem.velocity = random ? new Vector3(Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle(), Random.Shared.NextSingle() * 2 - 1) * (3 + (float)Random.Shared.NextDouble()) : velocity + MGame.Instance.cameraForward * (9 + (float)Random.Shared.NextDouble());
                            droppedItem.gravity = droppedItem.velocity.Y;

                            EntityManager.SpawnEntity(droppedItem);
                        }
                    }
                }
            }
        }
        public bool ExitOtherMenus()
        {
            if(accessingInventory)
            {
                accessingInventory = false;

                MGame.Instance.IsMouseVisible = false;
                Mouse.SetPosition(MGame.Instance.GraphicsDevice.Viewport.Width / 2, MGame.Instance.GraphicsDevice.Viewport.Height / 2);

                SpitContents(ref cursor);
                SpitContents(ref crafting);

                return true;
            }

            //on death screen
            if (health <= 0) return true;

            //not in any menus
            return false;
        }
        public bool OtherMenusActive()
        {
            return accessingInventory;
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

        public override void RestoreCustomSaveData(object data)
        {
            PlayerSaveData pData = (PlayerSaveData)data;

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
