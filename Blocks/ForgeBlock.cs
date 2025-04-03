using FantasyVoxels.Entities;
using FantasyVoxels.ItemManagement;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    public class ForgeBlock : Block
    {
        [System.Serializable]
        public class ForgeCustomData : ContainerBlockData, TickingCustomDataBlock, TextureOverrideCustomDataBlock, BlockLightOverrideCustomDataBlock
		{
			public const float MAX_COOK_TIME = 100;
			public const float FUEL_DURATION = 360;

			public float fuelTimeRemaining = 0;
			public float cookTime = MAX_COOK_TIME;
			public bool isCooking = false;
			public bool alternateFuelSlot = false;
			public bool useBothFuelSlots = true;

			public ForgeCustomData(int containerSize, ContainerDisplay displayMode) : base(containerSize, displayMode) { }

			public byte light => (byte)(isCooking ? 100 : 0);
			public short topTexture => (short)(isCooking ? 26 : -1);
			public short bottomTexture => -1;
			public short leftTexture => -1;
			public short rightTexture => -1;
			public short frontTexture => -1;
			public short backTexture => -1;

			public void tick(CompactBlockPos pos)
			{
				var (itemToCook, inputSlot) = FindItemToCook();

				if (itemToCook.itemID == -1 && fuelTimeRemaining <= 0)
				{
					ResetCooking(pos);
					return;
				}

				ConsumeFuel();

				if (!isCooking)
				{
					StartCooking(pos);
				}

				cookTime--;

				if (cookTime <= 0 && itemToCook.itemID != -1)
				{
					FinishCooking(itemToCook, inputSlot);
				}
			}

			private (Item, int) FindItemToCook()
			{
				int slot = 2;
				Item item = CraftingManager.TryCook(items.PeekItem(slot));

				if (item.itemID == -1) { item = CraftingManager.TryCook(items.PeekItem(3)); slot = 3; }
				if (item.itemID == -1) { item = CraftingManager.TryCook(items.PeekItem(2), items.PeekItem(3)); slot = 2; }

				return (item, slot);
			}

			private void ConsumeFuel()
			{
				if (--fuelTimeRemaining > 0) return;

				alternateFuelSlot = !alternateFuelSlot;
				useBothFuelSlots = items.PeekItem(0).itemID != -1 && items.PeekItem(1).itemID != -1;

				if ((alternateFuelSlot || !useBothFuelSlots) && items.TakeItem(0, 1).HasValue) fuelTimeRemaining = FUEL_DURATION;
				if ((!alternateFuelSlot || !useBothFuelSlots) && items.TakeItem(1, 1).HasValue) fuelTimeRemaining = FUEL_DURATION;
			}

			private void StartCooking(CompactBlockPos pos)
			{
				isCooking = true;
				MarkChunkDirty(pos.chunkID);

				useBothFuelSlots = items.PeekItem(0).itemID != -1 && items.PeekItem(1).itemID != -1;
				if (alternateFuelSlot || !useBothFuelSlots) items.TakeItem(0, 1);
				if (!alternateFuelSlot || !useBothFuelSlots) items.TakeItem(1, 1);

				alternateFuelSlot = !alternateFuelSlot;
			}

			private void FinishCooking(Item cookedItem, int inputSlot)
			{
				cookTime = MAX_COOK_TIME;
				if (items.AddItem(cookedItem, 4, out _) && inputSlot != -1)
				{
					items.TakeItem(inputSlot, 1);
				}
			}

			private void ResetCooking(CompactBlockPos pos)
			{
				cookTime = MAX_COOK_TIME;
				fuelTimeRemaining = 0;
				if (isCooking)
				{
					isCooking = false;
					MarkChunkDirty(pos.chunkID);
				}
			}
		}
        public override void Init()
        {
        }

        protected override bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk)
        {
            return false;
        }

        public override void PlaceBlock((int x, int y, int z) posInChunk, Chunk chunk)
        {
            blockCustomData.Add(GetPos(posInChunk.x, posInChunk.y, posInChunk.z, chunk.ID), new ForgeCustomData(5, ContainerBlockData.ContainerDisplay.Forge));
        }
        public override void BreakBlock((int x, int y, int z) posInChunk, Chunk chunk)
        {
            if (blockCustomData.TryGetValue(GetPos(posInChunk.x, posInChunk.y, posInChunk.z, chunk.ID), out object data))
            {
                SpitContents(ref ((ForgeCustomData)data).items, new Vector3(posInChunk.x + chunk.chunkPos.x * Chunk.Size,
                                                                            posInChunk.y + chunk.chunkPos.y * Chunk.Size,
                                                                            posInChunk.z + chunk.chunkPos.z * Chunk.Size));
            }
            blockCustomData.Remove(GetPos(posInChunk.x, posInChunk.y, posInChunk.z, chunk.ID));
        }
        public override bool UseBlock((int x, int y, int z) posInChunk, Chunk chunk, Entity from)
        {
            if (from is not Player) return false;

            Player player = from as Player;

            if (blockCustomData.TryGetValue(GetPos(posInChunk.x, posInChunk.y, posInChunk.z, chunk.ID), out object data))
            {
                player.OpenBlockContainer((ForgeCustomData)data);

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
                            droppedItem.position = (Vector3)position + Vector3.One * 0.5f;
                            droppedItem.velocity = new Vector3(Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle(), Random.Shared.NextSingle() * 2 - 1) * (3 + (float)Random.Shared.NextDouble());
                            droppedItem.gravity = droppedItem.velocity.Y;

                            EntityManager.SpawnEntity(droppedItem);
                        }
                    }
                }
            }
        }
    }
}
