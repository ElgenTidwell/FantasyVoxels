using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.ItemManagement
{
    public enum ItemType
    {
        Block,
        Tool,
        Misc,
    }
    public struct ToolPieceProperties
    {
        public ToolPieceSlot slot;
        public Voxel.MaterialType meantFor;
        public float diggingMultiplier;
        public int toolPieceLevel;
    }
    public enum ToolPieceSlot
    {
        Handle,
        Head
    }
    public class ItemData
    {
        public ItemType type;
        public int placement;
        public byte maxStackSize;
        public string name;
        public string displayName;
        public int texture;
        public bool alwaysRenderAsSprite;
        public bool toolPiece;
        public object properties;

        public void SetProperties(object properties) => this.properties = properties;
    }
    public struct ToolProperties
    {
        public int toolHandle;
        public int toolHead;
    }
    public struct Item
    {
        public int itemID;
        public byte stack;
        public object properties;
        public Item(string name, byte stack)
        {
            itemID = ItemManager.GetItemID(name);
            this.stack = stack;
        }
    }
    public static class ItemManager
    {
        private static List<ItemData> loadedItems = new List<ItemData>();
        private static Dictionary<string, int> itemNameToIndices = new Dictionary<string, int>();
        private static Dictionary<string, int> itemNameToItemTextureIndex = new Dictionary<string, int>();

        static ItemManager()
        {
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 1, name = "grass", displayName = "Grass" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 2, name = "dirt", displayName = "Dirt" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 4, name = "clayblock", displayName = "Clay Block" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 5, name = "wood", displayName = "Wood Log" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 8, name = "leaves", displayName = "Leaves" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 9, name = "stone", displayName = "Stone" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 10, name = "planks", displayName = "Planks" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 11, name = "cobblestone", displayName = "Cobblestone" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 12, name = "daisy", displayName = "Daisy", alwaysRenderAsSprite = true });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 13, name = "lamp", displayName = "Lamp" });
            RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 50, placement = 14, name = "torch", displayName = "Torch", alwaysRenderAsSprite = true });

            RegisterItem(new ItemData { type = ItemType.Misc, toolPiece = true, maxStackSize = 50, name = "stick", displayName = "Stick", texture = 0 })
                .SetProperties(new ToolPieceProperties { meantFor = Voxel.MaterialType.Wood, diggingMultiplier = 1, slot = ToolPieceSlot.Handle, toolPieceLevel = 1 });
            RegisterItem(new ItemData { type = ItemType.Misc, maxStackSize = 50, name = "glowleaf", displayName = "Glow Leaf", texture = 1 });
            RegisterItem(new ItemData { type = ItemType.Misc, maxStackSize = 50, name = "woodaxehead", displayName = "Wooden Axe Head", texture = 2 });
            RegisterItem(new ItemData { type = ItemType.Misc, maxStackSize = 50, name = "clay", displayName = "Clay", texture = 3 });
            RegisterItem(new ItemData { type = ItemType.Misc, toolPiece = true, maxStackSize = 50, name = "woodaxetoolhead", displayName = "Wooden Axe Tool-Head", texture = 4 })
                .SetProperties(new ToolPieceProperties { meantFor = Voxel.MaterialType.Wood, diggingMultiplier = 1.5f, slot = ToolPieceSlot.Head, toolPieceLevel = 1 });
        }
        public static ItemData RegisterItem(ItemData data)
        {
            int id = loadedItems.Count;
            loadedItems.Add(data);
            itemNameToIndices.Add(data.name,id);
            return data;
        }


        public static ItemData GetItem(string name) => GetItemFromID(itemNameToIndices[name]);
        public static int GetItemID(string name) => (itemNameToIndices[name]);
        public static ItemData GetItemFromID(int id)
        {
            return loadedItems[id];
        }
    }
}
