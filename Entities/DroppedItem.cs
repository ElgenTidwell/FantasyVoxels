using FantasyVoxels.ItemManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;

namespace FantasyVoxels.Entities
{
    internal class DroppedItem : Entity
    {
        public Item item;
        bool renderAsSprite, fromBlockColors;

        float pickupAnim;
        float wait = 0.5f;

        bool playAnim;
        float animTime;
        float scale = 0.25f;
        Vector3 animStartPos;

        private VertexPositionNormalTexture[] vertices;
        
        public DroppedItem(Item id)
        {
            this.item = new Item { itemID = id.itemID, properties = id.properties, stack = 1 };
        }

        public override void Destroyed()
        {
        }

        public override void Render()
        {
            MGame.Instance.GrabVoxelData((Vector3)position, out var voxelData);

            float ourLight = (voxelData.skyLight / 255f)*MGame.Instance.daylightPercentage + voxelData.blockLight / 255f;
            MGame.Instance.GetEntityShader().Parameters["tint"].SetValue(ourLight);
            MGame.Instance.GetEntityShader().Parameters["mainTexture"].SetValue(fromBlockColors ? MGame.Instance.colors : MGame.Instance.items);

            Matrix rotation = Matrix.CreateRotationX(this.rotation.Y) * Matrix.CreateRotationY(this.rotation.Z) * Matrix.CreateRotationZ(this.rotation.X);

            if(renderAsSprite)
            {
                Vector3 camForward = MGame.Instance.cameraForward;
                camForward.Y = 0;
                camForward.Normalize();

                rotation = Matrix.CreateTranslation(Vector3.UnitY * 0.5f)*Matrix.CreateTranslation(Vector3.UnitX * -0.5f) * Matrix.CreateRotationY(MathHelper.ToRadians(90f)) * Matrix.CreateWorld(Vector3.Zero,-camForward,Vector3.Up);
            }

            Matrix world = Matrix.CreateTranslation(Vector3.One*-0.5f) *
                           rotation *
                           Matrix.CreateScale(renderAsSprite? scale*2: scale) *
                           Matrix.CreateWorld((Vector3)position,Vector3.Forward,Vector3.Up);

            MGame.Instance.GetEntityShader().Parameters["World"].SetValue(world* MGame.Instance.world);

            foreach (var pass in MGame.Instance.GetEntityShader().CurrentTechnique.Passes)
            {
                pass.Apply();
                MGame.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
            }
        }
        public override void Update()
        {
            if (grounded) velocity = Maths.MoveTowards(velocity, Vector3.Zero, MGame.dt * 25);

            float playerdist = Vector3.Distance((Vector3)MGame.Instance.player.position - Vector3.UnitY * 1f, (Vector3)position);

            if (playAnim)
            {
                animTime += MGame.dt*6f;

                scale = (1 - animTime) * 0.25f;

                if(animTime >= 1)
                {
                    EntityManager.DeleteEntity(this);
                }

                velocity += Vector3.Normalize((Vector3)MGame.Instance.player.position - (Vector3)position) * (2- playerdist);

                base.Update();
                return;
            }
            
            if(wait >= 0) wait -= MGame.dt;

            if(playerdist < 1.8f && wait < 0 && MGame.Instance.player.PickupItem(item))
            {
                playAnim = true;
                animStartPos = (Vector3)position;
                return;
            }

            base.Update();
        }
        public override void Start()
        {
            //change UVS
            if(item.itemID != -2)
            {
                bool block = ItemManager.GetItemFromID(item.itemID).type == ItemType.Block;
                fromBlockColors = block;
                bool spr = ItemManager.GetItemFromID(item.itemID).alwaysRenderAsSprite || !block;
                vertices = new VertexPositionNormalTexture[(spr ? 6 : 6 * 6)];
                renderAsSprite = spr;
                for (int i = 0; i < (spr ? 1 : 6); i++)
                {
                    int tex = 0;
                    
                    if(block)
                    {
                        switch (i)
                        {
                            case 0: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(item.itemID).placement].rightTexture; break;
                            case 1: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(item.itemID).placement].leftTexture; break;
                            case 2: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(item.itemID).placement].topTexture; break;
                            case 3: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(item.itemID).placement].bottomTexture; break;
                            case 4: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(item.itemID).placement].frontTexture; break;
                            case 5: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(item.itemID).placement].backTexture; break;
                        }
                    }
                    else
                    {
                        tex = ItemManager.GetItemFromID(item.itemID).texture;
                    }

                    vertices[i * 6 + 0] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x,Chunk.positionChecks[i].y,Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (block?MGame.AtlasSize:MGame.ItemAtlasSize));
                    vertices[i * 6 + 1] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 1], new Vector3(Chunk.positionChecks[i].x,Chunk.positionChecks[i].y,Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (block?MGame.AtlasSize:MGame.ItemAtlasSize));
                    vertices[i * 6 + 2] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x,Chunk.positionChecks[i].y,Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (block?MGame.AtlasSize:MGame.ItemAtlasSize));
                    vertices[i * 6 + 3] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x,Chunk.positionChecks[i].y,Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (block?MGame.AtlasSize:MGame.ItemAtlasSize));
                    vertices[i * 6 + 4] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x,Chunk.positionChecks[i].y,Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (block?MGame.AtlasSize:MGame.ItemAtlasSize));
                    vertices[i * 6 + 5] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 3], new Vector3(Chunk.positionChecks[i].x,Chunk.positionChecks[i].y,Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (block?MGame.AtlasSize:MGame.ItemAtlasSize));
                }
            }
            else
            {
                if(item.properties is ToolProperties)
                {
                    bool block = false;
                    fromBlockColors = block;
                    bool spr = true;
                    vertices = new VertexPositionNormalTexture[12];
                    renderAsSprite = spr;

                    int head = ((ToolProperties)item.properties).toolHead;
                    int handle = ((ToolProperties)item.properties).toolHandle;

                    int tex = 0;
                    tex = ItemManager.GetItemFromID(head).texture;

                    int i = 0;

                    vertices[0 + 0] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[0 + 1] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 1], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[0 + 2] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[0 + 3] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[0 + 4] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[0 + 5] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 3], new Vector3(Chunk.positionChecks[i].x, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));

                    tex = ItemManager.GetItemFromID(handle).texture;
                    vertices[6 + 0] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x-0.01f, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[6 + 1] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 1], new Vector3(Chunk.positionChecks[i].x-0.01f, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 1] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[6 + 2] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x-0.01f, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[6 + 3] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 0], new Vector3(Chunk.positionChecks[i].x-0.01f, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 0] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[6 + 4] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 2], new Vector3(Chunk.positionChecks[i].x-0.01f, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 2] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                    vertices[6 + 5] = new VertexPositionNormalTexture(Chunk.vertsPerCheck[i * 4 + 3], new Vector3(Chunk.positionChecks[i].x-0.01f, Chunk.positionChecks[i].y, Chunk.positionChecks[i].z), (Chunk.uvs[i * 4 + 3] * 16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / (MGame.ItemAtlasSize));
                }
            }

            bounds = new BoundingBox(Vector3.One * -0.125f, Vector3.One * 0.125f);

            rotation.Z = (float)Random.Shared.NextDouble()*360f;
        }
        public override void RestoreCustomSaveData(object data)
        {
        }
        public override object CaptureCustomSaveData()
        {
            return null;
        }
    }
}
