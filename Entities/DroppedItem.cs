using FantasyVoxels.ItemManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;

namespace FantasyVoxels.Entities
{
    internal class DroppedItem : Entity
    {
        public int itemID;
        bool renderAsSprite, fromBlockColors;

        float pickupAnim;
        float wait = 0.5f;

        bool playAnim;
        float animTime;
        float scale = 0.4f;
        Vector3 animStartPos;

        private VertexPositionTexture[] vertices;
        
        public DroppedItem(int id)
        {
            this.itemID = id;

            //change UVS
            if(ItemManager.GetItemFromID(id).type == ItemType.Block)
            {
                fromBlockColors = true;
                vertices = new VertexPositionTexture[(ItemManager.GetItemFromID(id).alwaysRenderAsSprite?6:6*6)];
                renderAsSprite = ItemManager.GetItemFromID(id).alwaysRenderAsSprite;
                for (int i = 0; i < (ItemManager.GetItemFromID(id).alwaysRenderAsSprite ? 1 : 6); i++)
                {
                    int tex = 0;
                    switch (i)
                    {
                        case 0: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].rightTexture; break;
                        case 1: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].leftTexture; break;
                        case 2: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].topTexture; break;
                        case 3: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].bottomTexture; break;
                        case 4: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].frontTexture; break;
                        case 5: tex = Voxel.voxelTypes[ItemManager.GetItemFromID(id).placement].backTexture; break;
                    }
                    vertices[i * 6 + 0] = new VertexPositionTexture(Chunk.vertsPerCheck[i * 4 + 0], (Chunk.uvs[i * 4 + 0]*16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / 256f);
                    vertices[i * 6 + 1] = new VertexPositionTexture(Chunk.vertsPerCheck[i * 4 + 1], (Chunk.uvs[i * 4 + 1]*16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / 256f);
                    vertices[i * 6 + 2] = new VertexPositionTexture(Chunk.vertsPerCheck[i * 4 + 2], (Chunk.uvs[i * 4 + 2]*16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / 256f);
                    vertices[i * 6 + 3] = new VertexPositionTexture(Chunk.vertsPerCheck[i * 4 + 0], (Chunk.uvs[i * 4 + 0]*16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / 256f);
                    vertices[i * 6 + 4] = new VertexPositionTexture(Chunk.vertsPerCheck[i * 4 + 2], (Chunk.uvs[i * 4 + 2]*16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / 256f);
                    vertices[i * 6 + 5] = new VertexPositionTexture(Chunk.vertsPerCheck[i * 4 + 3], (Chunk.uvs[i * 4 + 3]*16 + new Vector2((tex % 16.0f) * 16.0f, tex / 16)) / 256f);
                }
            }
            else
            {
                renderAsSprite = true;
                vertices[0] = new VertexPositionTexture(Chunk.vertsPerCheck[0], Chunk.uvs[0]);
                vertices[1] = new VertexPositionTexture(Chunk.vertsPerCheck[1], Chunk.uvs[1]);
                vertices[2] = new VertexPositionTexture(Chunk.vertsPerCheck[2], Chunk.uvs[2]);
                vertices[3] = new VertexPositionTexture(Chunk.vertsPerCheck[0], Chunk.uvs[0]);
                vertices[4] = new VertexPositionTexture(Chunk.vertsPerCheck[2], Chunk.uvs[2]);
                vertices[5] = new VertexPositionTexture(Chunk.vertsPerCheck[3], Chunk.uvs[3]);
            }

            bounds = new BoundingBox(Vector3.One * -0.125f, Vector3.One * 0.125f);
        }

        public override void Destroyed()
        {
        }

        public override void Render()
        {
            MGame.Instance.GrabVoxelData((Vector3)position, out var voxelData);

            float ourLight = (voxelData.skyLight / 255f)*MGame.Instance.daylightPercentage + voxelData.blockLight / 255f;

            MGame.Instance.basicEffect.VertexColorEnabled = false;
            MGame.Instance.basicEffect.FogEnabled = false;
            MGame.Instance.basicEffect.LightingEnabled = false;
            MGame.Instance.basicEffect.TextureEnabled = true;
            MGame.Instance.basicEffect.Alpha = 1;
            MGame.Instance.basicEffect.DiffuseColor = Vector3.One * ourLight;

            if (fromBlockColors) MGame.Instance.basicEffect.Texture = MGame.Instance.colors;

            Matrix rotation = Matrix.Identity;

            if(renderAsSprite)
            {
                Vector3 camForward = MGame.Instance.cameraForward;
                camForward.Y = 0;
                camForward.Normalize();

                rotation = Matrix.CreateTranslation(Vector3.UnitX * -0.5f) * Matrix.CreateRotationY(MathHelper.ToRadians(90f)) * Matrix.CreateWorld(Vector3.Zero,camForward,Vector3.Up);
            }

            MGame.Instance.basicEffect.World = Matrix.CreateTranslation(Vector3.One*-0.5f)*
                                               rotation *
                                               Matrix.CreateScale(scale) * 
                                               MGame.Instance.world * 
                                               Matrix.CreateWorld((Vector3)position,Vector3.Forward,Vector3.Up);

            MGame.Instance.basicEffect.View = MGame.Instance.view;
            MGame.Instance.basicEffect.Projection = MGame.Instance.projection;

            foreach(var pass in MGame.Instance.basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                MGame.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
            }
        }
        public override void Update()
        {
            if (grounded) velocity = Maths.MoveTowards(velocity, Vector3.Zero, MGame.dt * 25);

            if(playAnim)
            {
                animTime += MGame.dt*6f;

                float t = float.Pow(animTime, 1.4f);

                scale = (1 - animTime) * 0.4f;

                position = Vector3.Lerp(animStartPos, (Vector3)MGame.Instance.player.position, t);

                if(animTime >= 1)
                {
                    EntityManager.DeleteEntity(this);
                }

                return;
            }
            
            if(wait >= 0) wait -= MGame.dt;

            float playerdist = Vector3.Distance((Vector3)MGame.Instance.player.position - Vector3.UnitY * 1f, (Vector3)position);
            if(playerdist < 1.4f && wait < 0)
            {
                MGame.Instance.player.PickupItem(itemID, 1);
                playAnim = true;
                animStartPos = (Vector3)position;
                return;
            }

            base.Update();
        }
        public override void Start()
        {
        }
        public override void RestoreCustomSaveData(JObject data)
        {
        }
        public override object CaptureCustomSaveData()
        {
            return null;
        }
    }
}
