using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.ItemManagement
{
    public struct Recipe
    {
        public Item[] itemInput;
        public Item itemOutput;
    }
    public class CraftingManager
    {
        private Recipe[] recipes;
    }
}
