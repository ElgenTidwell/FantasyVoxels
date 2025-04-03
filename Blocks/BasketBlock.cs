using FantasyVoxels.Entities;
using FantasyVoxels.ItemManagement;
using Microsoft.Xna.Framework;
using System;

namespace FantasyVoxels.Blocks
{
    public class BasketBlock : Block
    {
        public override void Init()
        {
        }

        protected override bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk)
        {
            return false;
        }

        public override void PlaceBlock((int x, int y, int z) posInChunk, Chunk chunk)
        {
            blockCustomData.Add(GetPos(posInChunk.x, posInChunk.y, posInChunk.z,chunk.ID), new ContainerBlockData(16,ContainerBlockData.ContainerDisplay.Basket));
        }
        public override void BreakBlock((int x, int y, int z) posInChunk, Chunk chunk)
        {
            if (blockCustomData.TryGetValue(GetPos(posInChunk.x, posInChunk.y, posInChunk.z, chunk.ID), out object data))
            {
                SpitContents(ref ((ContainerBlockData)data).items, new Vector3(posInChunk.x + chunk.chunkPos.x*Chunk.Size, 
                                                                               posInChunk.y + chunk.chunkPos.y*Chunk.Size, 
                                                                               posInChunk.z + chunk.chunkPos.z*Chunk.Size));
            }
            blockCustomData.Remove(GetPos(posInChunk.x, posInChunk.y, posInChunk.z, chunk.ID));
        }
        public override bool UseBlock((int x, int y, int z) posInChunk, Chunk chunk, Entity from)
        {
            if (from is not Player) return false;

            Player player = from as Player;

            if(blockCustomData.TryGetValue(GetPos(posInChunk.x, posInChunk.y, posInChunk.z, chunk.ID), out object data))
            {
                player.OpenBlockContainer((ContainerBlockData)data);

                return true;
            }
            return false;
        }
        void SpitContents(ref ItemContainer container, Vector3 position)
        {
            for (int j = 0; j < container.GetAllItems().Length; j++)
            {
                if (container.PeekItem(j).stack > 0)
                {
                    var item = container.TakeItemStack(j);
                    container.SetItem(new Item { itemID = -1, stack = 0 }, j);

                    if (item.itemID != -1 && item.stack != 0)
                    {
                        for (int i = 0; i < item.stack; i++)
                        {
                            var droppedItem = new DroppedItem(item);
                            droppedItem.position = (Vector3)position + Vector3.One*0.5f;
                            droppedItem.velocity = new Vector3(Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle(), Random.Shared.NextSingle() * 2 - 1) * (3 + (float)Random.Shared.NextDouble());
                            droppedItem.gravity = droppedItem.velocity.Y;

                            EntityManager.SpawnEntity(droppedItem);
                        }
                    }
                }
            }
        }
    }
    [System.Serializable]
    public class ContainerBlockData
    {
        public enum ContainerDisplay
        {
            Basket, //simple 4x4 grid
            Forge,
        }

        public ItemContainer items;
        public ContainerDisplay displayMode;
        public ContainerBlockData(int containerSize, ContainerDisplay displayMode)
        {
            items = new ItemContainer(containerSize);
            this.displayMode = displayMode;
        }
    }
}
