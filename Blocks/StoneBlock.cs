using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.Blocks
{
    internal class StoneBlock : Block
    {
        public StoneBlock() { customDrops = true; }

        public override Item[] GetCustomDrops()
        {
            return new Item[] { new Item { itemID = ItemManager.GetItemID("stonechunk"), stack = (byte)Random.Shared.Next(2,4)} };
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
