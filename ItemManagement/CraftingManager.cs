using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.ItemManagement
{
    public struct Recipe
    {
        public int[] itemInput;
        public Item itemOutput;

        public bool noOrder;
    }
    public struct TemplateRecipe
    {
        public string[] itemInput;
        public string itemOutput;
        public byte outputStack;

        public bool noOrder;
    }
    public static class CraftingManager
    {
        private static List<Recipe> recipes = new List<Recipe>();

        public static void Register(Recipe r)
        {
            recipes.Add(r);
        }
        public static void Register(TemplateRecipe r)
        {
            Recipe recipe = new Recipe();

            recipe.noOrder = r.noOrder;
            recipe.itemOutput = new Item { itemID= Get(r.itemOutput), stack = r.outputStack};
            recipe.itemInput = new int[r.itemInput.Length];
            for (int i = 0; i < r.itemInput.Length; i++)
            {
                if (string.IsNullOrEmpty(r.itemInput[i])) { recipe.itemInput[i] = -1; continue; }

                recipe.itemInput[i] = Get(r.itemInput[i]);
            }

            recipes.Add(recipe);
        }

        static int Get(string name) => ItemManager.GetItemID(name);

        public static void Setup()
        {
            //Register(new Recipe
            //{
            //    itemInput = [
            //        -1, -1, Get("glowleaf"),
            //        -1, -1, Get("stick"),
            //    ],
            //    itemOutput = new Item { itemID = Get("torch"), stack = 1 }
            //});
            //Register(new Recipe
            //{
            //    itemInput = [
            //        Get("stick"),Get("stick"), -1,
            //        Get("stick"), -1, -1
            //    ],
            //    itemOutput = new Item { itemID = Get("woodaxehead"), stack = 1 }
            //});
            //Register(new Recipe
            //{
            //    itemInput = [
            //        Get("stick"), Get("stick"), -1,
            //        Get("stick"), Get("clay"), -1
            //    ],
            //    itemOutput = new Item { itemID = Get("woodaxetoolhead"), stack = 1 }
            //});
            //Register(new Recipe
            //{
            //    itemInput = [
            //        Get("woodaxehead"), Get("clay"),-1,
            //        -1,-1,-1
            //    ],
            //    itemOutput = new Item { itemID = Get("woodaxetoolhead"), stack = 1 },
            //    noOrder = true
            //});
            //Register(new Recipe
            //{
            //    itemInput = [
            //        Get("wood"),
            //        -1,
            //        -1,
            //        -1,
            //        -1,
            //        -1
            //    ],
            //    itemOutput = new Item { itemID = Get("plank"), stack = 12 },
            //    noOrder = true
            //});
            //Register(new Recipe
            //{
            //    itemInput = [
            //        Get("plank"),
            //        Get("plank"),
            //        Get("plank"),
            //        Get("plank"),
            //        Get("plank"),
            //        Get("plank")
            //    ],
            //    itemOutput = new Item { itemID = Get("planks"), stack = 1 },
            //    noOrder = false
            //});

            TemplateRecipe[] templateRecipes = JsonConvert.DeserializeObject<TemplateRecipe[]>(File.ReadAllText($"{MGame.Instance.Content.RootDirectory}/Data/recipes.json"));
            for(int i = 0; i < templateRecipes.Length; i++)
            {
                Register(templateRecipes[i]);
            }
        }
        public static Item TryCraft(ItemContainer input)
        {
            if (input.GetAllItems().Length == 10)
            {
                int[] itemIDs = new int[9];
                bool any = false;
                for(int i = 0; i < 9; i++)
                {
                    var item = input.PeekItem(i);
                    itemIDs[i] = item.itemID;
                    if(item.itemID != -1) any = true;
                }

                if (!any) return new Item { itemID = -1, stack = 0 };

                Recipe recipe = new Recipe();
                for(int i = 0; i < recipes.Count; i++)
                {
                    if (!recipes[i].noOrder)
                    {
                        if (itemIDs.SequenceEqual(recipes[i].itemInput))
                        {
                            recipe = recipes[i];
                            break;
                        }
                    }
                    else
                    {
                        if(recipes[i].itemInput.OrderBy(x=>x).SequenceEqual(itemIDs.OrderBy(x=>x)))
                        {
                            recipe = recipes[i];
                            break;
                        }
                    }
                }

                if (recipe.itemInput != null && recipe.itemOutput.itemID >= 0)
                {
                    return new Item { itemID = recipe.itemOutput.itemID, stack = recipe.itemOutput.stack };
                }
                else
                {
                    int headID = -1;
                    int handleID = -1;

                    for (int i = 0; i < 9; i++)
                    {
                        var item = input.PeekItem(i);
                        if(item.itemID >= 0 && ItemManager.GetItemFromID(item.itemID).toolPiece)
                        {
                            if (item.itemID >= 0 && ItemManager.GetItemFromID(item.itemID).properties is ToolPieceProperties properties && properties.slot == ToolPieceSlot.Head) headID = item.itemID;
                            if (item.itemID >= 0 && ItemManager.GetItemFromID(item.itemID).properties is ToolPieceProperties properties1 && properties1.slot == ToolPieceSlot.Handle) handleID = item.itemID;
                        }
                    }

                    if(headID >= 0 && handleID >= 0)
                    {
                        int durability = (int)((((ToolPieceProperties)ItemManager.GetItemFromID(headID).properties).durability + ((ToolPieceProperties)ItemManager.GetItemFromID(handleID).properties).durability) / 2f);

                        return new Item { itemID = -2, stack = 1, properties = new ToolProperties { toolHead = headID,toolHandle = handleID,durability = durability,maxDurability = durability } };
                    }
                }
            }
            return new Item { itemID = -1, stack = 0 };
        }
    }
}
