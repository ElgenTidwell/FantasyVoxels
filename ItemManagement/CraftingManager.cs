using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.ItemManagement
{
    public struct Recipe
    {
        public int[] itemInput;
        public Item itemOutput;
    }
    public static class CraftingManager
    {
        private static List<Recipe> recipes = new List<Recipe>();

        public static void Register(Recipe r)
        {
            recipes.Add(r);
        }

        static int Get(string name) => ItemManager.GetItemID(name);

        public static void Setup()
        {
            Register(new Recipe
            {
                itemInput = [
                     -1,Get("glowleaf"),
                     -1,Get("stick"),
                ],
                itemOutput = new Item { itemID = Get("torch"), stack = 1 }
            });
            Register(new Recipe
            {
                itemInput = [
                     Get("glowleaf"),-1,
                     Get("stick"),   -1
                ],
                itemOutput = new Item { itemID = Get("torch"), stack = 1 }
            });
            Register(new Recipe
            {
                itemInput = [
                    Get("stick"),
                    Get("stick"),
                    Get("stick"),
                    -1
                ],
                itemOutput = new Item { itemID = Get("woodaxehead"), stack = 1 }
            });
            Register(new Recipe
            {
                itemInput = [
                    Get("woodaxehead"),
                    Get("clay"),
                    -1,
                    -1
                ],
                itemOutput = new Item { itemID = Get("woodaxetoolhead"), stack = 1 }
            });
        }
        public static Item TryCraft(ItemContainer input)
        {
            if (input.GetAllItems().Length == 5)
            {
                int[] itemIDs = new int[4];

                for(int i = 0; i < 4; i++)
                {
                    var item = input.PeekItem(i);
                    itemIDs[i] = item.itemID;
                }
                var recipe = recipes.Find(e=>e.itemInput[0] == itemIDs[0] && e.itemInput[1] == itemIDs[1] && e.itemInput[2] == itemIDs[2] && e.itemInput[3] == itemIDs[3]);

                if (recipe.itemInput != null && recipe.itemOutput.itemID >= 0)
                {
                    return new Item { itemID = recipe.itemOutput.itemID, stack = recipe.itemOutput.stack };
                }
                else
                {
                    int headID = -1;
                    int handleID = -1;

                    for (int i = 0; i < 4; i++)
                    {
                        var item = input.PeekItem(i);
                        if(item.itemID != -1 && ItemManager.GetItemFromID(item.itemID).toolPiece)
                        {
                            if (((ToolPieceProperties)ItemManager.GetItemFromID(item.itemID).properties).slot == ToolPieceSlot.Head) headID = item.itemID;
                            if (((ToolPieceProperties)ItemManager.GetItemFromID(item.itemID).properties).slot == ToolPieceSlot.Handle) handleID = item.itemID;
                        }
                    }

                    if(headID >= 0 && handleID >= 0)
                    {
                        return new Item { itemID = -2, stack = 1, properties = new ToolProperties { toolHead = headID,toolHandle = handleID } };
                    }
                }
            }
            return new Item { itemID = -1, stack = 0 };
        }
    }
}
