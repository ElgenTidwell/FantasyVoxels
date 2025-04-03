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
using FmodForFoxes;
using FmodForFoxes.Studio;
using static FantasyVoxels.Voxel;
using FantasyVoxels.Blocks;
using System.Reflection;
using Solovox.Entities;

namespace FantasyVoxels.Entities
{
    public class Player : Entity
    {
        static float walk = 4, run = 6.8f, sneak = 2f, swim = 0.5f;
        static BoundingBox standingBounds = new BoundingBox(new Vector3(-0.2f, -1.6f, -0.2f), new Vector3(0.2f, 0.2f, 0.2f));
        static BoundingBox crouchedBounds = new BoundingBox(new Vector3(-0.2f, -1.5f, -0.2f), new Vector3(0.2f, 0.2f, 0.2f));
		HashSet<int> learnedItemIDs = new HashSet<int>();
        MultiValueDictionary<RecipeType, int> learnedRecipes = new MultiValueDictionary<RecipeType, int>();

        MouseState oldState;

        Vector3 forward, right;
        float curwalkspeed = 12;
        float desiredFOV = 70f;

        float autoDigTime = 0f;

        bool running,crouched;
        bool deathUIshown = false;
        bool accessingInventory;
        bool accessingIdeasBook;
		bool accessingBlockContainer;

        ContainerBlockData blockContainer;

        bool wasGrounded = false;

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
        Entity hitEntity;
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
        ItemContainer crafting = new ItemContainer(10);
        Item heldItem => hotbar.PeekItem(activeHotbarSlot);
        Item prevHeldItem;

        Vector3 wishDir;
        Vector3 oldRotation;

        VertexPositionNormalTexture[] heldBlockModel;
        bool handFromBlockColors;
        bool handIsSprite;
        bool handIsEmpty;
        bool drawHand;
        bool regenerateHand;

        float vmswayX;
        float vmswayY;
        float vmoffsetY;
        float vmoffsetSwayY;
        float vmoffsetSwayX;

        float diggingTimer;
        float waterDamageTimer;
        float displayHealthBarPerc = 1;

        HandAnimation currentHandAnimation;
        float handAnimationTimer;
        float swingAngle = 0;

        enum HandAnimation
        {
            None = 0,
            Swing = 1,
        }

        static float[] animationSpeeds = [
            0f,
            5f,
        ];
        bool step = false;

        EventInstance robotPainNoise;

        public Player()
        {
            maxHealth = 20;
            health = maxHealth;
            //fly = true;
        }

        public void EducateOnItem(int id)
        {
            if (!learnedItemIDs.Contains(id))
            {
				learnedItemIDs.Add(id);
                
                var recipes = CraftingManager.GetRecipes(RecipeType.Crafting);
                foreach(var recipe in recipes.FindAll(r => r.itemInput.Contains(id)))
				{
					int rid = recipes.FindIndex(r => r == recipe);

					if (!learnedRecipes.ContainsKey(RecipeType.Crafting)) learnedRecipes.Add(RecipeType.Crafting, rid);
                    else
					if (!learnedRecipes[RecipeType.Crafting].Contains(rid)) learnedRecipes[RecipeType.Crafting].Add(rid);

					learnedRecipes[RecipeType.Crafting] = learnedRecipes[RecipeType.Crafting].DistinctBy(e => recipes[e].itemOutput).ToList();
					learnedRecipes[RecipeType.Crafting].Sort((a, b) =>
                    {
                        return string.Compare(ItemManager.GetItemFromID(recipes[a].itemOutput.itemID).name, ItemManager.GetItemFromID(recipes[b].itemOutput.itemID).name);
                    });
				}
				recipes = CraftingManager.GetRecipes(RecipeType.Cooking);
				foreach (var recipe in recipes.FindAll(r => r.itemInput.Contains(id)))
				{
                    int rid = recipes.FindIndex(r=> r == recipe);

					if (!learnedRecipes.ContainsKey(RecipeType.Cooking)) learnedRecipes.Add(RecipeType.Cooking, rid);
					else
					if (!learnedRecipes[RecipeType.Cooking].Contains(rid)) learnedRecipes[RecipeType.Cooking].Add(rid);

					learnedRecipes[RecipeType.Cooking] = learnedRecipes[RecipeType.Cooking].DistinctBy(e => recipes[e].itemOutput).ToList();
					learnedRecipes[RecipeType.Cooking].Sort((a, b) =>
					{
						return string.Compare(ItemManager.GetItemFromID(recipes[a].itemOutput.itemID).name, ItemManager.GetItemFromID(recipes[b].itemOutput.itemID).name);
					});
				}
			}
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

            robotPainNoise = MGame.robotPainEvent.CreateInstance();
		}
        public override void OnTakeDamage(DamageInfo info)
        {
            painResponse = (info.damage-1)*0.5f+1;
            robotPainNoise.Stop();
            robotPainNoise.Start();

			gravity = 5f;
			grounded = false;

			if (info.fromEntity is not null)
			{
				velocity -= Vector3.Normalize((Vector3)info.from - (Vector3)position)*4;
			}

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

            if (Keyboard.GetState().IsKeyDown(Keys.Space) && (grounded || swimming||fly))
            {
                gravity = swimming ? (grounded? 4.5f : MathF.Min(gravity + 28 * MGame.dt, 4.5f)) : (fly? 12f : 7f);
                velocity.Y = gravity;
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
            if (crouched && (swimming||fly))
            {
                gravity = fly?-10f:-3f;
            }

            rotmat = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y);

            forward = rotmat.Forward;
            MGame.Instance.cameraForward = forward;
            right = rotmat.Right;

            oldHitVoxel = hitVoxel;
            voxelHit = Maths.Raycast((Vector3)position, forward, 5, out prevHitTile, out hitTile, out int hitVoxelType, out var voxelData);
            hitVoxel = (hitVoxelType, (int)hitTile.X, (int)hitTile.Y, (int)hitTile.Z, voxelData.placement);
            hitEntity = Maths.RaycastEntities((Vector3)position, forward, 5, this);
            if (hitEntity is not null) voxelHit = false;

            //if its not the same voxel, reset the timer
            if (hitVoxel != oldHitVoxel)
            {
                diggingTimer = 0;
            }
            autoDigTime -= MGame.dt;
            if (Mouse.GetState().LeftButton == ButtonState.Released) diggingTimer = Maths.MoveTowards(diggingTimer,0,MGame.dt*6);
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && voxelHit && hitEntity is null)
            {
                if (autoDigTime <= 0)
                {
                    autoDigTime = 0.2f;

                    SetAnimation(HandAnimation.Swing);

                    Vector3 normal = prevHitTile - hitTile;
                    Vector3 plane = (Vector3.One - new Vector3(float.Abs(normal.X), float.Abs(normal.Y), float.Abs(normal.Z)));

                    int tex = Voxel.voxelTypes[hitVoxelType].bottomTexture;

                    MGame.PlayWalkSound(Voxel.voxelTypes[hitVoxelType], hitTile + Vector3.One * 0.5f);

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
                if (hitEntity is not null)
                {
                    hitEntity.OnTakeDamage(new DamageInfo { from = position, damage = 1 });
                }
            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed && autoDigTime <= 0)
            {
                int id = hotbar.PeekItem(activeHotbarSlot).itemID;

                bool TryInteractWithBlock()
                {
                    return voxelHit && MGame.Instance.UseVoxel(hitTile, this);
                }

                if (id != -1)
                {
                    bool testitemtype(ItemType type)
                    {
                        switch (type)
                        {
                            case ItemType.Block:

                                if (!voxelHit) return false;

                                BoundingBox placeBox = new BoundingBox(prevHitTile - (Vector3)position + Vector3.One * 0.001f, prevHitTile + Vector3.One * 0.999f - (Vector3)position);

                                int v = MGame.Instance.GrabVoxel(prevHitTile);
                                if (v > 0 && Voxel.voxelTypes[v].ignoreCollision)
                                {
                                    MGame.Instance.SetVoxel(prevHitTile, 0);
                                }
                                PlacementMode mode = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].placementMode;

                                Vector3 place = Vector3.Zero;

                                if(mode == PlacementMode.BlockFace)
                                {
                                    place = hitTile - prevHitTile;
                                }
                                else
                                {
                                    Vector3 dir = hitTile - Vector3.Floor((Vector3)position);

                                    if(dir.X > dir.Z)
                                    {
                                        if (dir.X > dir.Y)
                                        {
                                            place.X = dir.X;
                                        }
                                        else
                                        {
                                            place.Y = dir.Y;
                                        }
                                    }
                                    else
                                    {
                                        if (dir.Z > dir.Y)
                                        {
                                            place.Z = dir.Z;
                                        }
                                        else
                                        {
                                            place.Y = dir.Y;
                                        }
                                    }
                                }

                                PlacementSettings placement= PlacementSettings.ANY;
                                if (place.X > 0) placement = PlacementSettings.RIGHT;
                                if (place.X < 0) placement = PlacementSettings.LEFT;
                                if (place.Y > 0) placement = PlacementSettings.TOP;
                                if (place.Y < 0) placement = PlacementSettings.BOTTOM;
                                if (place.Z > 0) placement = PlacementSettings.FRONT;
                                if (place.Z < 0) placement = PlacementSettings.BACK;

                                if ((placeBox.Contains(bounds) == ContainmentType.Disjoint || Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].ignoreCollision)
                                     && Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].AllowsPlacement(placement)
                                     && !(Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].myClass != null && !Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].myClass.CanPlace(placement, hitVoxelType)))
                                {
                                    autoDigTime = 0.25f;

                                    MGame.PlayDigSound(Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement],prevHitTile);
                                    MGame.Instance.SetVoxel(prevHitTile, ItemManager.GetItemFromID(id).placement, placement: placement);
                                    hotbar.TakeItem(activeHotbarSlot, 1);
                                    SetAnimation(HandAnimation.Swing);
                                }

                                return true;
                            case ItemType.Food:

                                if(health < maxHealth)
                                {
                                    autoDigTime = 0.25f;

                                    var foodprop = (FoodProperties)ItemManager.GetItemFromID(hotbar.TakeItem(activeHotbarSlot, 1).Value.itemID).properties;

                                    health = (byte)(health + foodprop.saturation);
                                    return true;
                                }

                                return false;

                            default:

                                return false;
                        }
                    }
                    if (id >= 0)
                    {
                        //testitemtype(ItemManager.GetItemFromID(id).type) 

                        if(crouched)
                        {
                            if (!testitemtype(ItemManager.GetItemFromID(id).type)) TryInteractWithBlock();
                        }
                        else
                        {
                            if (!TryInteractWithBlock()) testitemtype(ItemManager.GetItemFromID(id).type);
                        }
                    }
                    else
                    {
                        TryInteractWithBlock();
                    }
                }
                else
                {
                    TryInteractWithBlock();
                }
            }
            if (Mouse.GetState().RightButton == ButtonState.Released && Mouse.GetState().LeftButton == ButtonState.Released) autoDigTime = 0f;

            int swheeldelta = MathF.Sign(Mouse.GetState().ScrollWheelValue - oldScroll);

            if (swheeldelta > 0) activeHotbarSlot--;
            if (swheeldelta < 0) activeHotbarSlot++;

            if (activeHotbarSlot >= 9) activeHotbarSlot -= 9;
            if (activeHotbarSlot < 0) activeHotbarSlot += 9;

            if(Keyboard.GetState().GetPressedKeyCount() > 0)
            {
				switch (Keyboard.GetState().GetPressedKeys()[0])
				{
                    default:
                        break;
                    case Keys.D1:
                        activeHotbarSlot = 0;
                        break;
					case Keys.D2:
						activeHotbarSlot = 1;
						break;
					case Keys.D3:
						activeHotbarSlot = 2;
						break;
					case Keys.D4:
						activeHotbarSlot = 3;
						break;
					case Keys.D5:
						activeHotbarSlot = 4;
						break;
					case Keys.D6:
						activeHotbarSlot = 5;
						break;
					case Keys.D7:
						activeHotbarSlot = 6;
						break;
					case Keys.D8:
						activeHotbarSlot = 7;
						break;
					case Keys.D9:
						activeHotbarSlot = 8;
						break;
				}
			}

            oldScroll = Mouse.GetState().ScrollWheelValue;

            Mouse.SetPosition(MGame.Instance.GraphicsDevice.Viewport.Width / 2, MGame.Instance.GraphicsDevice.Viewport.Height / 2);
            oldState = Mouse.GetState();

            rotmat = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y);

            var lforward = rotmat.Forward;
            var lup = rotmat.Up;

            MGame.listener.Position3D = (Vector3)position;
            MGame.listener.ForwardOrientation = lforward;
            MGame.listener.UpOrientation = -lup;
        }
        public override void Die()
        {
            deathUIshown = true;

            SpitContents(ref inventory, true);
            SpitContents(ref hotbar,true);

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
        void playstepsound()
        {
            int v = MGame.Instance.GrabVoxel(new Vector3((float)position.X, (float)(bounds.Min.Y + position.Y - 0.8f), (float)position.Z));
            if (v >= 0 && Voxel.voxelTypes[v].surfaceType != Voxel.SurfaceType.None)
                MGame.PlayWalkSound(Voxel.voxelTypes[v], (Vector3)position + Vector3.Up * bounds.Min.Y);
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

                bobTime += MGame.dt * ((speed / walk - 1) * 0.8f + 1);

                xsin = MathF.Sin(bobTime * 7.5f) * bob;
                ysin = MathF.Cos(bobTime * 7.5f) * bob;

                if (wasGrounded != grounded) playstepsound();

                if((float.Abs(ysin) > 0.9f * bob && !step && bob >= 0.1f))
                {
                    step = true;
                    playstepsound();
                }
                if(step && float.Abs(ysin) < 0.9f * bob)
                {
                    step = false;
                }

                bob = Maths.MoveTowards(bob, bobMulti * 1.2f, MGame.dt * 12 * float.Abs(bob - bobMulti * 1.2f));

                curwalkspeed = ((crouched ? sneak : running ? run : walk) * (swimming ? swim : 1))*(fly?6:1);

                HandleInventory();

                wishDir = Vector3.Zero;
                if (!accessingInventory && !accessingIdeasBook) HandleInput();
                painResponse = Maths.MoveTowards(painResponse, 0, MGame.dt * 5 * (float.Abs(painResponse) + 0.1f));


                //Water damage, really unfun. Probably gonna remove that
                //if(swimming)
                //{
                //    waterDamageTimer -= MGame.dt;
                //    if(waterDamageTimer < 0)
                //    {
                //        waterDamageTimer = 1;
                //        OnTakeDamage(new DamageInfo { damage = 1 });
                //    }
                //}
            }
            else
            {
                painResponse = Maths.MoveTowards(painResponse, 4, MGame.dt * 5 * (float.Abs(painResponse) + 0.1f));
            }

            //if(BetterKeyboard.HasBeenPressed(Keys.R))
            //{
            //    Skeleton test = new Skeleton();
            //    test.position = position + forward;

            //    EntityManager.SpawnEntity(test);
            //}

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
                handIsEmpty = false;

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
                                float size = (MGame.AtlasSize / 16);
                                heldBlockModel[i*6 + 0] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 1] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 1], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 2] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 3] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 4] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.AtlasSize);
                                heldBlockModel[i*6 + 5] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 3], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.AtlasSize);
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
                //else
                //{
                //    handIsEmpty = true;
                //    drawHand = true;

                //    List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();

                //    int i = 4;
                //    const float pix = 1 / 16f;
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0]/(pix*16))* new Vector3(pix * 6,pix*10,pix*10)+ new Vector3(0.2f,-0.1f,0), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(6,10) + new Vector2(3,3))*pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 1]/(pix*16))* new Vector3(pix * 6,pix*10,pix*10)+ new Vector3(0.2f,-0.1f,0), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * new Vector2(6,10)  + new Vector2(3,3))*pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2]/(pix*16))* new Vector3(pix * 6,pix*10,pix*10)+ new Vector3(0.2f,-0.1f,0), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(6,10)  + new Vector2(3,3))*pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0]/(pix*16))* new Vector3(pix * 6,pix*10,pix*10)+ new Vector3(0.2f,-0.1f,0), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(6,10)  + new Vector2(3,3))*pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2]/(pix*16))* new Vector3(pix * 6,pix*10,pix*10)+ new Vector3(0.2f,-0.1f,0), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(6,10)  + new Vector2(3,3))*pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 3]/(pix*16))* new Vector3(pix * 6,pix*10,pix*10)+ new Vector3(0.2f,-0.1f,0), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * new Vector2(6,10)  + new Vector2(3,3))*pix));

                //    i = 1;
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0] / (pix * 16)) * new Vector3(pix * 6,pix*10,pix*3)+ new Vector3(0.2f,-0.1f,pix*7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(3,10) + new Vector2(0, 3)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 1] / (pix * 16)) * new Vector3(pix * 6,pix*10,pix*3)+ new Vector3(0.2f,-0.1f,pix*7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * new Vector2(3,10) + new Vector2(0, 3)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2] / (pix * 16)) * new Vector3(pix * 6,pix*10,pix*3)+ new Vector3(0.2f,-0.1f,pix*7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(3,10) + new Vector2(0, 3)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0] / (pix * 16)) * new Vector3(pix * 6,pix*10,pix*3)+ new Vector3(0.2f,-0.1f,pix*7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(3,10) + new Vector2(0, 3)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2] / (pix * 16)) * new Vector3(pix * 6,pix*10,pix*3)+ new Vector3(0.2f,-0.1f,pix*7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(3,10) + new Vector2(0, 3)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 3] / (pix * 16)) * new Vector3(pix * 6,pix*10,pix*3)+ new Vector3(0.2f,-0.1f,pix*7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * new Vector2(3,10) + new Vector2(0, 3)) * pix));

                //    i = 3;
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(3, 3) + new Vector2(3, 9)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 1] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * new Vector2(3, 3) + new Vector2(3, 9)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(3, 3) + new Vector2(3, 9)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(3, 3) + new Vector2(3, 9)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(3, 3) + new Vector2(3, 9)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 3] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * new Vector2(3, 3) + new Vector2(3, 9)) * pix));

                //    i = 2;
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(3, 3) + new Vector2(3, 0)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 1] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * new Vector2(3, 3) + new Vector2(3, 0)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(3, 3) + new Vector2(3, 0)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 0] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * new Vector2(3, 3) + new Vector2(3, 0)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 2] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * new Vector2(3, 3) + new Vector2(3, 0)) * pix));
                //    verts.Add(new VertexPositionNormalTexture((Chunk.vertsPerCheck[i * 4 + 3] / (pix * 16)) * new Vector3(pix * 6, pix * 10, pix * 3) + new Vector3(0.2f, -0.1f, pix * 7), new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * new Vector2(3, 3) + new Vector2(3, 0)) * pix));

                //    heldBlockModel = verts.ToArray();
                //}
            }
            prevHeldItem = hotbar.PeekItem(activeHotbarSlot);
            oldHotbarSlot = activeHotbarSlot;
            oldRotation = rotation;

            wasGrounded = grounded;

            applyVelocity(wishDir);
            base.Update();
        }

        VertexPositionNormalTexture[] GenerateSpriteModel(int tex,Vector3 offset,Vector3 scale)
        {
            List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();

            float size = (MGame.ItemAtlasSize / 16);
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+0]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+0] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+1]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+1] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+2]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+2] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+0]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+0] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+2]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+2] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
            verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4+3]*scale + offset, -Vector3.UnitX, (Chunk.uvs[4+3] * 16 + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));

            for (int x = 0; x < 16; x++)
            {
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4 * 4 + 0] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, Vector3.UnitZ, (Chunk.uvs[4 * 4 + 0] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4 * 4 + 1] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, Vector3.UnitZ, (Chunk.uvs[4 * 4 + 1] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4 * 4 + 2] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, Vector3.UnitZ, (Chunk.uvs[4 * 4 + 2] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4 * 4 + 0] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, Vector3.UnitZ, (Chunk.uvs[4 * 4 + 0] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4 * 4 + 2] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, Vector3.UnitZ, (Chunk.uvs[4 * 4 + 2] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[4 * 4 + 3] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, Vector3.UnitZ, (Chunk.uvs[4 * 4 + 3] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));

                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 0] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 0] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 1] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 1] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 2] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 2] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 0] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 0] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 2] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 2] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[5 * 4 + 3] * scale * new Vector3(1 / 16f, 1, 1) + offset - Vector3.UnitZ * x / 16f, -Vector3.UnitZ, (Chunk.uvs[5 * 4 + 3] * new Vector2(1, 16) + new Vector2(15 - x, 0) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
            }

            for (int y = 0; y < 16; y++)
            {
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * (y - 15) / 16f, Vector3.UnitY, (new Vector2(Chunk.uvs[2 * 4 + 0].X, Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 1] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * (y - 15) / 16f, Vector3.UnitY, (new Vector2(Chunk.uvs[2 * 4 + 1].X, Chunk.uvs[2 * 4 + 1].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * (y - 15) / 16f, Vector3.UnitY, (new Vector2(Chunk.uvs[2 * 4 + 2].X, Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * (y - 15) / 16f, Vector3.UnitY, (new Vector2(Chunk.uvs[2 * 4 + 0].X, Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * (y - 15) / 16f, Vector3.UnitY, (new Vector2(Chunk.uvs[2 * 4 + 2].X, Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[2 * 4 + 3] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * (y - 15) / 16f, Vector3.UnitY, (new Vector2(Chunk.uvs[2 * 4 + 3].X, Chunk.uvs[2 * 4 + 3].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));

                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y / 16f, -Vector3.UnitY, (new Vector2(1 - Chunk.uvs[2 * 4 + 0].X, Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 1] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y / 16f, -Vector3.UnitY, (new Vector2(1 - Chunk.uvs[2 * 4 + 1].X, Chunk.uvs[2 * 4 + 1].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y / 16f, -Vector3.UnitY, (new Vector2(1 - Chunk.uvs[2 * 4 + 2].X, Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 0] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y / 16f, -Vector3.UnitY, (new Vector2(1 - Chunk.uvs[2 * 4 + 0].X, Chunk.uvs[2 * 4 + 0].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 2] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y / 16f, -Vector3.UnitY, (new Vector2(1 - Chunk.uvs[2 * 4 + 2].X, Chunk.uvs[2 * 4 + 2].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
                verts.Add(new VertexPositionNormalTexture(Chunk.vertsPerCheck[3 * 4 + 3] * new Vector3(1 / 16f, 1, 1) + offset + Vector3.UnitY * y / 16f, -Vector3.UnitY, (new Vector2(1 - Chunk.uvs[2 * 4 + 3].X, Chunk.uvs[2 * 4 + 3].Y) * new Vector2(16, 1) + new Vector2(0, 15 - y) + new Vector2((tex % size) * 16.0f, (int)(tex / size)*16)) / MGame.ItemAtlasSize));
            }

            return verts.ToArray();
        }

        private void SetAnimation(HandAnimation handAnimation)
        {
            swingAngle = Random.Shared.NextSingle() * 2 - 1;
			currentHandAnimation = handAnimation;
            handAnimationTimer = 0;
        }
        private void HandleInventory()
        {
            if (BetterKeyboard.HasBeenPressed(Keys.E))
            {
                if (!ExitOtherMenus()) { accessingInventory = true; MGame.Instance.IsMouseVisible = true; }
            }

			if (BetterKeyboard.HasBeenPressed(Keys.R))
			{
				if (!ExitOtherMenus()) { accessingIdeasBook = true; MGame.Instance.IsMouseVisible = true; }
			}

			if (!accessingInventory) return;
        }
        public bool PickupItem(Item item)
        {
            if (health <= 0) return false;

            // See if we already have this item in our inventory, even if there's no stack available
            // that this will fit in, if we already have one of these in our inventory we probably want them all in there.
            if(Array.FindIndex(inventory.items,i => i.itemID > - 1 && i.itemID == item.itemID) != -1)
            {
                return inventory.AddItem(item, out int l) || hotbar.AddItem(item, out l);
            }

            if(!hotbar.AddItem(item, out int leftover))
            {
                return inventory.AddItem(item, out leftover);
            }
            return true;
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
            float addSpeed = MathHelper.Clamp(curwalkspeed - curSpeed, 0, fly?1200:(grounded || swimming ? 200 : 10)*MGame.dt);

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

                    animMatrix *= Matrix.CreateTranslation(0, float.Pow(handAnimationTimer, 2f)+0.2f, -float.Pow(handAnimationTimer,2f)*2-0.1f)*
                                 Matrix.CreateRotationX(MathHelper.ToRadians(float.Pow(handAnimationTimer, 0.8f) * -125 + 15))*
                                 Matrix.CreateRotationY(MathHelper.ToRadians(float.Pow(handAnimationTimer, 0.8f) * 25)) *
                                 Matrix.CreateRotationZ(swingAngle*0.8f);

                    break;
            }

            vmoffsetSwayY = Maths.MoveTowards(vmoffsetSwayY, -float.Abs(vmswayX), MGame.dt*(float.Abs(vmoffsetSwayY- float.Abs(vmswayX))+0.1f)*2f);
            float xtarg = Vector3.Dot(new Vector3(velocity.X, 0, velocity.Z), right)*-0.05f;
            vmoffsetSwayX = Maths.MoveTowards(vmoffsetSwayX, xtarg, MGame.dt*(float.Abs(vmoffsetSwayX-xtarg)+0.01f)*12);

            Matrix world = Matrix.CreateRotationZ(vmoffsetSwayX*0.1f) * 
                           Matrix.CreateTranslation(new Vector3(0, 0.1f,0)) *
                           Matrix.CreateScale(handIsSprite? 1f: 0.6f) *
                           Matrix.CreateRotationY(MathHelper.ToRadians(45))*
                           Matrix.CreateRotationX(MathHelper.ToRadians(8))*
                           animMatrix *
                           Matrix.CreateWorld(new Vector3(0.2f+ ysin * 0.15f*bob, -1.2f-(((float.Abs(xsin) * 1.0f)) * bob) * 0.1f + vmoffsetY + bob*-0.16f, -1.2f), Vector3.Forward, Vector3.Up)*
                           Matrix.CreateTranslation(vmoffsetSwayX*0.1f, vmoffsetSwayY-vmoffsetSwayX*0.04f, 0) *
                           Matrix.CreateRotationX(MathHelper.ToRadians(velocity.Y * -0.2f)+ vmswayY) *
                           Matrix.CreateRotationY(vmswayX) *
                           Matrix.CreateRotationZ(painResponse*0.01f);


            MGame.Instance.GrabVoxelData((Vector3)position, out var voxelData);

            float ourLight = (voxelData.skyLight / 255f) * MGame.Instance.daylightPercentage;
            var effect = MGame.Instance.GetEntityShader();
            effect.Parameters["tint"].SetValue(ourLight);
            effect.Parameters["blocklightTint"].SetValue(voxelData.blockLight / 255f);
            effect.Parameters["colorTint"].SetValue(Vector3.One);

            effect.Parameters["mainTexture"].SetValue(handIsEmpty ? MGame.Instance.handSpr : handFromBlockColors ? MGame.Instance.colors : MGame.Instance.items);

            effect.Parameters["World"].SetValue(world);
            effect.Parameters["View"].SetValue(Matrix.Identity);
            effect.Parameters["RotWorld"].SetValue(Matrix.Transpose(Matrix.Invert(world)));

            foreach (var pass in effect.CurrentTechnique.Passes)
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
                    UIUtils.DrawItem(new Vector2(horizPos,UserInterface.Active.ScreenHeight),uiScale,id,hotbar.PeekItem(i).stack, hotbar.PeekItem(i));
                }
            }
            float healthFloat = (float)health / maxHealth;
            displayHealthBarPerc = Maths.MoveTowards(displayHealthBarPerc,healthFloat,MGame.dt*float.Abs(displayHealthBarPerc-healthFloat)*32);

            MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures, 
                                            new Vector2(14 * uiScale, UserInterface.Active.ScreenHeight - 24 * uiScale), 
                                            new Rectangle(59, 0, 68, 12), 
                                            Color.White, 0, Vector2.Zero, uiScale, SpriteEffects.None, 0f);
            MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures,
                                            new Vector2(16 * uiScale, UserInterface.Active.ScreenHeight - 22 * uiScale),
                                            new Rectangle(59, 12, (int)(65* displayHealthBarPerc), 8),
                                            Color.White, 0, Vector2.Zero, uiScale, SpriteEffects.None, 0.01f);

            //string healthStr = $"{health}/{maxHealth}";

            //float xOffset = Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(healthStr).X;

            //MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular],
            //                                      healthStr,
            //                                      new Vector2(48 * uiScale - (xOffset / 2) * (uiScale*0.5f), UserInterface.Active.ScreenHeight - 22 * uiScale),
            //                                      Color.White, 0f, Vector2.Zero, Vector2.One * (uiScale * 0.5f), SpriteEffects.None, 0.02f);
            //Use this layout if you add a second bar!
            //MGame.Instance.spriteBatch.Draw(MGame.Instance.uiTextures,
            //                                new Vector2(14 * uiScale, UserInterface.Active.ScreenHeight - 13 * uiScale),
            //                                new Rectangle(59, 0, 68, 12),
            //                                Color.White, 0, Vector2.Zero, uiScale, SpriteEffects.None, 0f);

            MGame.Instance.spriteBatch.End();

			if (accessingIdeasBook)
			{
				int scale = (int)float.Floor(4 * UserInterface.Active.GlobalScale);
				DrawIdeasBook(scale);
			}

			if (accessingInventory)
            {
                MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack);

                //black background
                MGame.Instance.spriteBatch.Draw(MGame.Instance.white, MGame.Instance.GraphicsDevice.Viewport.Bounds, null, new Color(Color.Black, 200), 0f, Vector2.Zero, SpriteEffects.None, 0.05f);

                int scale = (int)(uiScale * 1);
                int backpackX = (int)(-103 * scale + UserInterface.Active.ScreenWidth / 2f);

                //Make room!!
                if (accessingBlockContainer) backpackX = 0;

                int backpackY = (int)(50 * scale + UserInterface.Active.ScreenHeight / 2f);
                MGame.Instance.spriteBatch.Draw(MGame.Instance.uiBackback, new Vector2(backpackX, backpackY - 142*scale), new Rectangle(0, 0, 206, 164), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.1f);

                //hotbar
                for (int i = 0; i < 9; i++)
                {
                    var slotbounds = new Rectangle(backpackX + 10 * scale + i * scale * 21, backpackY - 6 * scale, scale * 18, scale * 18);
                    bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                    int id = hotbar.PeekItem(i).itemID;
                    if (id != -1)
                    {
                        UIUtils.DrawItem(new Vector2(backpackX+8 * scale + i * scale * 21, backpackY - 6 * scale + scale * 21), scale, id, hotbar.PeekItem(i).stack, hotbar.PeekItem(i));
                    }

                    if (isSelected)
                    {
                        UIUtils.RenderTooltip(hotbar.PeekItem(i),scale);

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
                        UIUtils.DrawItem(new Vector2(backpackX + 8 * scale + (i%5) * scale * 21, backpackY - 111 * scale + (i/5)*scale*21), scale, id, inventory.PeekItem(i).stack, inventory.PeekItem(i));
                    }

                    if (isSelected)
                    {
                        UIUtils.RenderTooltip(inventory.PeekItem(i), scale);

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                        TryTransferContainerToCursor(ref inventory, i);
                    }
                }

                //crafting
                for (int i = 0; i < 10; i++)
                {
                    const int backpackSize = 9;
                    int x = backpackX + 134 * scale + (i < backpackSize ? i % 3 : 1) * scale * 21;
                    int y = backpackY - 111 * scale + (i < backpackSize ? (i / 3) : 4) * scale * 21;

                    var slotbounds = new Rectangle(x + 2*scale, y - scale * 21, scale * 18, scale * 18);
                    bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                    int id = crafting.PeekItem(i).itemID;
                    if (id != -1)
                    {
                        UIUtils.DrawItem(new Vector2(x, y), scale, id, crafting.PeekItem(i).stack, crafting.PeekItem(i));
                    }

                    if (isSelected)
                    {
                        UIUtils.RenderTooltip(crafting.PeekItem(i), scale);

                        if (i < backpackSize) TryTransferContainerToCursor(ref crafting, i);
                        else
                        {
                            if (BetterMouse.WasLeftPressed())
                            {
                                var add = crafting.PeekItem(i);
                                if (add.itemID != -1 && cursor.TestAddItem(add, 0))
                                {
                                    cursor.AddItem(add, 0, out int remainder);

                                    for(i = 0; i < backpackSize; i++)
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
									EducateOnItem(add.itemID);
								}
                            }
                        }

                        MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                    }

                    if(i == backpackSize)
                    {
                        crafting.SetItem(CraftingManager.TryCraft(crafting),i);
                    }
                }
                
                if(accessingBlockContainer)
                {
                    DrawBlockContainer(scale);
                }

				//clicked outside the window
				if (BetterMouse.WasLeftPressed() && !new Rectangle(backpackX, backpackY - 142 * scale, 206*scale, 164*scale).Contains(Mouse.GetState().Position) && !accessingBlockContainer)
                {
                    SpitContents(ref cursor);
                }

                if (cursor.PeekItem(0).itemID != -1)
                {
                    UIUtils.DrawItem(Vector2.Floor(Mouse.GetState().Position.ToVector2() / scale) * scale + new Vector2(-12,12) * scale, scale, cursor.PeekItem(0).itemID, cursor.PeekItem(0).stack,cursor.PeekItem(0),0.9f);
                }

                MGame.Instance.spriteBatch.End();
            }
        }
        void DrawBlockContainer(int scale)
        {
            int containerX = (int)(-122 * scale + UserInterface.Active.ScreenWidth);
            int containerY = (int)(50 * scale + UserInterface.Active.ScreenHeight / 2f);

            switch (blockContainer.displayMode)
            {
                case ContainerBlockData.ContainerDisplay.Basket:

                    MGame.Instance.spriteBatch.Draw(MGame.Instance.uiBasket, new Vector2(containerX, containerY - 142 * scale), new Rectangle(0, 0, 101, 100), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.1f);

                    for(int i = 0; i < blockContainer.items.GetAllItems().Length; i++)
                    {
                        int x = containerX + 8 * scale + (i % 4) * scale * 21;
                        int y = containerY - 111 * scale + (i / 4) * scale * 21;

                        var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);
                        bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                        int id = blockContainer.items.PeekItem(i).itemID;
                        if (id != -1)
                        {
                            UIUtils.DrawItem(new Vector2(x, y), scale, id, blockContainer.items.PeekItem(i).stack, blockContainer.items.PeekItem(i));
                        }

                        if (isSelected)
                        {
                            UIUtils.RenderTooltip(blockContainer.items.PeekItem(i), scale);

                            MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                            TryTransferContainerToCursor(ref blockContainer.items, i);
                        }
                    }

                    break;
                case ContainerBlockData.ContainerDisplay.Forge:

                    containerX = (int)(-142 * scale + UserInterface.Active.ScreenWidth);
                    MGame.Instance.spriteBatch.Draw(MGame.Instance.uiForge, new Vector2(containerX, containerY - 142 * scale), new Rectangle(0, 0, 121, 100), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.1f);

                    //Fuel
                    for (int i = 0; i < 2; i++)
                    {
                        int x = containerX + 8 * scale + (i % 4) * scale * 21;
                        int y = containerY - 48 * scale + (i / 4) * scale * 21;

                        var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);
                        bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                        int id = blockContainer.items.PeekItem(i).itemID;
                        if (id != -1)
                        {
                            UIUtils.DrawItem(new Vector2(x, y), scale, id, blockContainer.items.PeekItem(i).stack, blockContainer.items.PeekItem(i));
                        }

                        if (isSelected)
                        {
                            UIUtils.RenderTooltip(blockContainer.items.PeekItem(i), scale);

                            MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                            TryTransferContainerToCursor(ref blockContainer.items, i);
                        }
                    }

                    int barFuel = (int)(((blockContainer as ForgeBlock.ForgeCustomData).fuelTimeRemaining/ForgeBlock.ForgeCustomData.FUEL_DURATION) *16);

                    if(barFuel > 0)
                    {
						MGame.Instance.spriteBatch.Draw(MGame.Instance.white,
														new Rectangle(containerX + 29 * scale, containerY - 99 * scale + (16-barFuel)*scale, 3 * scale, barFuel * scale),
														null,
														new Color(255, 69, 20, 10), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
					}

					//Input
					for (int i = 0; i < 2; i++)
                    {
                        int x = containerX + 8 * scale + i * scale * 21;
                        int y = containerY - 111 * scale;

                        var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);
                        bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                        int id = blockContainer.items.PeekItem(2+i).itemID;
                        if (id != -1)
                        {
                            UIUtils.DrawItem(new Vector2(x, y), scale, id, blockContainer.items.PeekItem(2 + i).stack, blockContainer.items.PeekItem(2 + i));
                        }

                        if (isSelected)
                        {
                            UIUtils.RenderTooltip(blockContainer.items.PeekItem(2 + i), scale);

                            MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                            TryTransferContainerToCursor(ref blockContainer.items, 2 + i);
                        }
                    }

                    //Output
                    {
                        int x = containerX + 7 * scale + 4 * scale * 21;
                        int y = containerY - 111 * scale;

                        var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);
                        bool isSelected = slotbounds.Contains(Mouse.GetState().Position);

                        int id = blockContainer.items.PeekItem(4).itemID;
                        if (id != -1)
                        {
                            UIUtils.DrawItem(new Vector2(x, y), scale, id, blockContainer.items.PeekItem(4).stack, blockContainer.items.PeekItem(3));
                        }

                        if (isSelected)
                        {
                            UIUtils.RenderTooltip(blockContainer.items.PeekItem(4), scale);

                            MGame.Instance.spriteBatch.Draw(MGame.Instance.white, slotbounds, null, new Color(Color.Black, 80), 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                            TryTransferContainerToCursor(ref blockContainer.items, 4,true);

                            if(BetterMouse.WasLeftPressed())
								EducateOnItem(blockContainer.items.PeekItem(4).itemID);
						}
                    }

                    break;
            }
        }

        int curIdeaPage = 0;
        RecipeType curIdeaTab = RecipeType.Crafting;
        void DrawIdeasBook(int scale)
        {
			MGame.Instance.spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack, blendState:BlendState.NonPremultiplied);

			//black background
			MGame.Instance.spriteBatch.Draw(MGame.Instance.white, MGame.Instance.GraphicsDevice.Viewport.Bounds, null, new Color(Color.Black, 200), 0f, Vector2.Zero, SpriteEffects.None, 0.05f);

			int backpackX = (int)(-(301/2) * scale + UserInterface.Active.ScreenWidth / 2f);

			int backpackY = (int)(UserInterface.Active.ScreenHeight / 2f);
			MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas, new Vector2(backpackX, backpackY - (184/2) * scale), new Rectangle(0, 0, 301, 184), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.1f);

			var craftTab = new Rectangle(backpackX-7*scale, backpackY - (184 / 2) * scale, 24 * scale, 24 * scale);
			var cookTab = new Rectangle(backpackX-7* scale, backpackY - (184 / 2) * scale + 24*scale, 24 * scale, 24 * scale);

            if (craftTab.Contains(Mouse.GetState().Position))
            {
				craftTab.X -= 16 * scale;
				if (BetterMouse.WasLeftPressed())
                {
					curIdeaTab = RecipeType.Crafting;
					curIdeaPage = 0;
				}
			}
            if (cookTab.Contains(Mouse.GetState().Position))
            {
				cookTab.X -= 16 * scale;
                if (BetterMouse.WasLeftPressed())
                {
					curIdeaTab = RecipeType.Cooking;
					curIdeaPage = 0;
				}
			}

			MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas, craftTab, new Rectangle(383, 0, 24, 24), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.06f);
			MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas, new Rectangle(craftTab.X+6*scale, craftTab.Y + 3 * scale, 16*scale,16 * scale), new Rectangle(364, 24, 16, 16), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.07f);
            MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas, cookTab, new Rectangle(383, 0, 24, 24), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.06f);
			MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas, new Rectangle(cookTab.X + 6 * scale, cookTab.Y + 3 * scale, 16 * scale, 16 * scale), new Rectangle(380, 24, 16, 16), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.07f);

			string tabname = "";

			int end = 0;

			switch (curIdeaTab)
			{
				case RecipeType.Crafting:

                    tabname = "Crafting";

                    if (!learnedRecipes.ContainsKey(RecipeType.Crafting)) break;

					end = learnedRecipes[RecipeType.Crafting].Count / 4;

					//Draw learned crafting recipes, if they exist.
					for (int i = 0; i < 4; i++)
			        {
                        int recipeIndex = curIdeaPage * 4 + i;

                        if (recipeIndex >= learnedRecipes[RecipeType.Crafting].Count) continue;

                        int templateX = backpackX + scale * 7 + i * scale * 74;
                        int templateY = backpackY - (184 / 2) * scale + scale * 52;
				        MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas, 
                                                        new Vector2(templateX, templateY), 
                                                        new Rectangle(301, 0, 64, 104), 
                                                        Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.2f);

						var recipe = learnedRecipes[RecipeType.Crafting][recipeIndex];
                        var recipesTotal = CraftingManager.GetRecipes(RecipeType.Crafting);
						//Normal crafting recipe
						if (recipesTotal[recipe].itemInput.Length == 9)
						{
							for (int j = 0; j < 9; j++)
							{
								const int backpackSize = 9;
								int x = templateX + (j < backpackSize ? j % 3 : 1) * scale * 21;
								int y = templateY + (j < backpackSize ? (j / 3) : 4) * scale * 21 + scale * 21;

								var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);

								int id = recipesTotal[recipe].itemInput[j];
								var item = new Item { itemID = id };

								if (id != -1)
								{
									if (learnedItemIDs.Contains(id)) UIUtils.DrawItem(new Vector2(x, y), scale, id, 1, item);
									else
									{
										MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas,
																		new Vector2(x, y - scale * 21),
																		new Rectangle(364, 1, 19, 19),
																		Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.3f);
									}
									bool isSelected = slotbounds.Contains(Mouse.GetState().Position);
									if (isSelected)
									{
										if (learnedItemIDs.Contains(id)) UIUtils.RenderTooltip(item, scale);
										else UIUtils.RenderTooltip("Unknown", "I'm unsure what might complete this...", scale);
									}
								}
							}
							{
								int x = templateX + (1) * scale * 21;
								int y = templateY + (4) * scale * 21 + scale * 21;

								var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);

								var item = recipesTotal[recipe].itemOutput;
								int id = recipesTotal[recipe].itemOutput.itemID;

								if (id != -1)
								{
									if (learnedItemIDs.Contains(id)) UIUtils.DrawItem(new Vector2(x, y), scale, id, 1, item);
									else
									{
										MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas,
																		new Vector2(x, y - scale * 21),
																		new Rectangle(364, 1, 19, 19),
																		Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.3f);
									}
									bool isSelected = slotbounds.Contains(Mouse.GetState().Position);
									if (isSelected)
									{
										if (learnedItemIDs.Contains(id)) UIUtils.RenderTooltip(item, scale);
										else UIUtils.RenderTooltip("Unknown", "I'm unsure what this might produce...", scale);
									}
								}
							}
						}
				    }

				break;

				case RecipeType.Cooking:

					tabname = "Cooking";

					if (!learnedRecipes.ContainsKey(RecipeType.Cooking)) break;

					end = learnedRecipes[RecipeType.Cooking].Count / 4;
					//Draw learned crafting recipes, if they exist.
					for (int i = 0; i < 4; i++)
					{
						int recipeIndex = curIdeaPage * 4 + i;

						if (recipeIndex >= learnedRecipes[RecipeType.Cooking].Count) continue;

						int templateX = backpackX + scale * 7 + i * scale * 74;
						int templateY = backpackY - (184 / 2) * scale + scale * 52;
						MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas,
														new Vector2(templateX, templateY),
														new Rectangle(407, 0, 42, 63),
														Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.2f);

						var recipe = learnedRecipes[RecipeType.Cooking][recipeIndex];
						var recipesTotal = CraftingManager.GetRecipes(RecipeType.Cooking);
						{
							for (int j = 0; j < 2; j++)
							{
                                if (j >= recipesTotal[recipe].itemInput.Length) continue;

								const int backpackSize = 9;
								int x = templateX + (j < backpackSize ? j % 3 : 1) * scale * 21;
								int y = templateY + (j < backpackSize ? (j / 3) : 4) * scale * 21 + scale * 21;

								var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);

								int id = recipesTotal[recipe].itemInput[j];
								var item = new Item { itemID = id };

								if (id != -1)
								{
									if (learnedItemIDs.Contains(id)) UIUtils.DrawItem(new Vector2(x, y), scale, id, 1, item);
									else
									{
										MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas,
																		new Vector2(x, y - scale * 21),
																		new Rectangle(364, 1, 19, 19),
																		Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.3f);
									}
									bool isSelected = slotbounds.Contains(Mouse.GetState().Position);
									if (isSelected)
									{
										if (learnedItemIDs.Contains(id)) UIUtils.RenderTooltip(item, scale);
										else UIUtils.RenderTooltip("Unknown", "I'm unsure what might complete this...", scale);
									}
								}
							}
							{
								int x = (int)(templateX + (0.5f) * scale * 21);
								int y = templateY + (2) * scale * 21 + scale * 21;

								var slotbounds = new Rectangle(x + 2 * scale, y - scale * 21, scale * 18, scale * 18);

								var item = recipesTotal[recipe].itemOutput;
								int id = recipesTotal[recipe].itemOutput.itemID;

								if (id != -1)
								{
									if (learnedItemIDs.Contains(id)) UIUtils.DrawItem(new Vector2(x, y), scale, id, 1, item);
									else
									{
										MGame.Instance.spriteBatch.Draw(MGame.Instance.uiIdeas,
																		new Vector2(x, y - scale * 21),
																		new Rectangle(364, 1, 19, 19),
																		Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.3f);
									}
									bool isSelected = slotbounds.Contains(Mouse.GetState().Position);
									if (isSelected)
									{
										if (learnedItemIDs.Contains(id)) UIUtils.RenderTooltip(item, scale);
										else UIUtils.RenderTooltip("Unknown", "I'm unsure what this might produce...", scale);
									}
								}
							}
						}
					}

					break;
			}
			MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], tabname, new Vector2(backpackX + 100 * scale, backpackY - (184 / 2) * scale + scale*5), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
			MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], tabname, new Vector2(backpackX + 101 * scale, backpackY - (184 / 2) * scale + scale*6), new Color(Color.Black,0.5f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0.8f);

            string pageindex = $"{curIdeaPage+1}/{end+1}";
            float xoff = Resources.Instance.Fonts[(int)FontStyle.Regular].MeasureString(pageindex).X;

			MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], pageindex, new Vector2(backpackX + 128 * scale + (xoff*0.5f*scale), backpackY + (184 / 2) * scale - scale * 22), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.7f);
			MGame.Instance.spriteBatch.DrawString(Resources.Instance.Fonts[(int)FontStyle.Regular], pageindex, new Vector2(backpackX + 129 * scale + (xoff*0.5f*scale), backpackY + (184 / 2) * scale - scale * 21), new Color(Color.Black, 0.5f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0.6f);

			var backButton = new Rectangle(backpackX + scale * 6, backpackY + scale*70, 15 * scale, 15 * scale);
            var forwardButton = new Rectangle(backpackX + scale * 280, backpackY + scale*70, 15 * scale, 15 * scale);

            var backSelected = backButton.Contains(Mouse.GetState().Position);
            var forwardSelected = forwardButton.Contains(Mouse.GetState().Position);

            if(backSelected)
            {
				MGame.Instance.spriteBatch.Draw(MGame.Instance.white, backButton, null, new Color(Color.Black, 100), 0f, Vector2.Zero, SpriteEffects.None, 0.15f);
                
                if(Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
					MGame.Instance.spriteBatch.Draw(MGame.Instance.white, backButton, null, new Color(Color.Black, 100), 0f, Vector2.Zero, SpriteEffects.None, 0.15f);
					if (BetterMouse.WasLeftPressed()) curIdeaPage--;
				}
			}
            if (forwardSelected)
            {
				MGame.Instance.spriteBatch.Draw(MGame.Instance.white, forwardButton, null, new Color(Color.Black, 100), 0f, Vector2.Zero, SpriteEffects.None, 0.15f);
                
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
				{
					MGame.Instance.spriteBatch.Draw(MGame.Instance.white, forwardButton, null, new Color(Color.Black, 100), 0f, Vector2.Zero, SpriteEffects.None, 0.15f);
					if (BetterMouse.WasLeftPressed()) curIdeaPage++;
				}
			}
            if(curIdeaPage < 0)
            {
                curIdeaPage = end;
			}
            if(curIdeaPage > end)
            {
                curIdeaPage = 0;
            }

			MGame.Instance.spriteBatch.End();
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
                else if (container.PeekItem(i).itemID != -1 && (cursor.PeekItem(0).itemID == container.PeekItem(i).itemID || cursor.PeekItem(0).itemID == -1))
                {
                    cursor.SetItem(container.TakeItem(i, (byte)float.Ceiling(container.PeekItem(i).stack / 2f)).Value, 0);
                }
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
                if(accessingBlockContainer) accessingBlockContainer = false;

                accessingInventory = false;

                MGame.Instance.IsMouseVisible = false;
                Mouse.SetPosition(MGame.Instance.GraphicsDevice.Viewport.Width / 2, MGame.Instance.GraphicsDevice.Viewport.Height / 2);

                SpitContents(ref cursor);
                crafting.SetItem(new Item { itemID = -1, stack = 0},9);
                SpitContents(ref crafting);

                return true;
            }

            if(accessingIdeasBook)
            {
                accessingIdeasBook = false;

				MGame.Instance.IsMouseVisible = false;
				Mouse.SetPosition(MGame.Instance.GraphicsDevice.Viewport.Width / 2, MGame.Instance.GraphicsDevice.Viewport.Height / 2);

                return true;
			}

			//on death screen
			if (health <= 0) return true;

            //not in any menus
            return false;
        }
        public void OpenBlockContainer(ContainerBlockData container)
        {
            accessingInventory = true;
            accessingBlockContainer = true;
            blockContainer = container;
            MGame.Instance.IsMouseVisible = true;
        }
        public bool OtherMenusActive()
        {
            return accessingInventory || accessingIdeasBook;
        }

        public override void Destroyed()
        {
        }

        public override object CaptureCustomSaveData()
        {
            List<int> crafting = new List<int>();
            List<int> cooking = new List<int>();
            if(learnedRecipes.ContainsKey(RecipeType.Crafting)) learnedRecipes.TryGetValue(RecipeType.Crafting, out crafting);
            if(learnedRecipes.ContainsKey(RecipeType.Cooking)) learnedRecipes.TryGetValue(RecipeType.Cooking, out cooking);

			return new PlayerSaveData
            {
                hotbar = hotbar.GetAllItems(),
                inventory = inventory.GetAllItems(),
                hotbarindex = activeHotbarSlot,
                learnedItems = learnedItemIDs.ToArray(),
                learnedCraftingRecipes = crafting.ToArray(),
				learnedCookingRecipes = cooking.ToArray(),
			};
        }

        public override void RestoreCustomSaveData(object data)
        {
            PlayerSaveData pData = (PlayerSaveData)data;

            hotbar.SetAllItems(pData.hotbar);
            inventory.SetAllItems(pData.inventory);
            activeHotbarSlot = pData.hotbarindex;

            if(pData.learnedItems != null) learnedItemIDs = new HashSet<int>(pData.learnedItems);
            if(pData.learnedCraftingRecipes != null) learnedRecipes[RecipeType.Crafting] = new List<int>(pData.learnedCraftingRecipes.DistinctBy(e => CraftingManager.GetRecipes(RecipeType.Crafting)[e].itemOutput));
            if(pData.learnedCookingRecipes != null) learnedRecipes[RecipeType.Cooking] = new List<int>(pData.learnedCookingRecipes.DistinctBy(e=> CraftingManager.GetRecipes(RecipeType.Cooking)[e].itemOutput));
		}
    }
    public struct PlayerSaveData
    {
        public Item[] hotbar;
        public Item[] inventory;
        public int hotbarindex;
        public int[] learnedItems;
        public int[] learnedCraftingRecipes;
        public int[] learnedCookingRecipes;
	}
}
