using FantasyVoxels.ItemManagement;
using FantasyVoxels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum ItemType
{
    Block,
    Misc,
}
public enum MaterialType
{
    Soil,
    Wood,
    Stone
}
public struct ToolPieceProperties
{
    public ToolPieceSlot slot;
    public MaterialType meantFor;
    public float diggingMultiplier;
    public int toolPieceLevel;
    public int durability;
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
    public string description;
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
    public int durability;
    public int maxDurability;
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

    public bool Use()
    {
        if(properties is ToolProperties prop)
        {
            prop.durability--;

            properties = prop;

            if(prop.durability <= 0)
            {
                itemID = prop.toolHandle;
                properties = null;
                stack = 1;
                return true;
            }
        }
        return false;
    }
}
public static class ItemManager
{
    private static List<ItemData> loadedItems = new List<ItemData>();
    private static Dictionary<string, int> itemNameToIndices = new Dictionary<string, int>();
    private static Dictionary<string, int> itemNameToItemTextureIndex = new Dictionary<string, int>();

    static ItemManager()
    {
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 1, name = "grass", displayName = "Grass" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 2, name = "dirt", displayName = "Dirt" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 4, name = "clayblock", displayName = "Clay Block" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 5, name = "wood", displayName = "Wood Log" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 8, name = "leaves", displayName = "Leaves" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 9, name = "stone", displayName = "Stone" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 10, name = "planks", displayName = "Wood Planks" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 11, name = "cobblestone", displayName = "Cobblestone" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 12, name = "daisy", displayName = "Daisy", alwaysRenderAsSprite = true });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 13, name = "lamp", displayName = "Lamp" });
        //RegisterItem(new ItemData { type = ItemType.Block, maxStackSize = 32, placement = 14, name = "torch", displayName = "Torch", alwaysRenderAsSprite = true });

        //RegisterItem(new ItemData { type = ItemType.Misc, toolPiece = true, maxStackSize = 32, name = "stick", displayName = "Stick", texture = 0 })
        //    .SetProperties(new ToolPieceProperties { meantFor = MaterialType.Wood, diggingMultiplier = 1, slot = ToolPieceSlot.Handle, toolPieceLevel = 1, durability = 100 });

        //RegisterItem(new ItemData { type = ItemType.Misc, maxStackSize = 32, name = "glowleaf", displayName = "Glow Leaf", texture = 1 });
        //RegisterItem(new ItemData { type = ItemType.Misc, maxStackSize = 32, name = "woodaxehead", displayName = "Wooden Axe Head", texture = 2 });
        //RegisterItem(new ItemData { type = ItemType.Misc, maxStackSize = 32, name = "clay", displayName = "Clay", texture = 3 });
        //RegisterItem(new ItemData { type = ItemType.Misc, toolPiece = true, maxStackSize = 32, name = "woodaxetoolhead", displayName = "Wooden Axe Tool-Head", texture = 4 })
        //    .SetProperties(new ToolPieceProperties { meantFor = MaterialType.Wood, diggingMultiplier = 1.5f, slot = ToolPieceSlot.Head, toolPieceLevel = 1, durability = 150 });

        //RegisterItem(new ItemData { type = ItemType.Misc, maxStackSize = 32, name = "plank", displayName = "Plank", texture = 5 });


        var jsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            MaxDepth = null,
            Formatting = Formatting.Indented
        };

        //File.WriteAllText($"{Environment.GetEnvironmentVariable("profilePath")}/items.json", JsonConvert.SerializeObject(loadedItems, jsonSerializerSettings));

        ItemData[] data = JsonConvert.DeserializeObject<ItemData[]>(File.ReadAllText($"{MGame.Instance.Content.RootDirectory}/Data/items.json"), jsonSerializerSettings);
        for (int i = 0; i < data.Length; i++)
        {
            RegisterItem(data[i]);
        }
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