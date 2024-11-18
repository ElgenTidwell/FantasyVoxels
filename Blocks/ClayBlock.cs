using FantasyVoxels.ItemManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    public class ClayBlock : Block
    {
        public ClayBlock() { customDrops = true; }

        public override void Init()
        {
        }

        protected override bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk)
        {
            return false;
        }
        public override Item[] GetCustomDrops()
        {
            int count = Random.Shared.Next(1, 4);

            return count > 0 ? [new Item { itemID = ItemManager.GetItemID("clay"), stack = (byte)count }] : null;
        }
    }
}
