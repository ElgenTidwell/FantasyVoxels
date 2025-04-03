using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    public class BushBlock : BasicFlowerBlock
    {
        public BushBlock() : base() { customDrops = true; }

        public override Item[] GetCustomDrops()
        {
            if (Random.Shared.Next(0, 4) == 1) return null;

            int stickCount = Random.Shared.Next(1, 5);

            List<Item> items = new List<Item>();

            if (stickCount > 0) items.Add(new Item { itemID = ItemManager.GetItemID("stick"), stack = (byte)stickCount });

            return items.Count > 0 ? items.ToArray() : null;
        }
    }
}
