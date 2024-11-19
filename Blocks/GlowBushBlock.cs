using FantasyVoxels.ItemManagement;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    public class GlowBushBlock : BasicFlowerBlock
    {
        public GlowBushBlock() :base() { customDrops = true; }

        public override Item[] GetCustomDrops()
        {
            if (Random.Shared.Next(0, 4) == 1) return null;

            int stickCount = Random.Shared.Next(0, 3);
            int leafCount = int.Max(Random.Shared.Next(-2, 3),0);

            List<Item> items = new List<Item>();

            if (stickCount > 0) items.Add(new Item { itemID = ItemManager.GetItemID("stick"), stack = (byte)stickCount });
            if (leafCount > 0) items.Add(new Item { itemID = ItemManager.GetItemID("glowleaf"), stack = (byte)leafCount });

            return items.Count>0?items.ToArray():null;
        }
    }
}
