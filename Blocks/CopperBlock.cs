using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    public class CopperBlock : Block
    {
        public CopperBlock() { customDrops = true; }

        public override Item[] GetCustomDrops()
        {
            return new Item[] { new Item { itemID = ItemManager.GetItemID("copperchunk"), stack = (byte)Random.Shared.Next(1, 2) } };
        }

        public override void Init()
        {
        }

        protected override bool BlockUpdate((int x, int y, int z) posInChunk, Chunk chunk)
        {
            return false;
        }
    }
}
