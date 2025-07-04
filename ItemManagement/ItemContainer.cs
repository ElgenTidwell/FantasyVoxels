﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyVoxels.ItemManagement
{
    [System.Serializable]
    public class ItemContainer
    {
        public Item[] items;

        public ItemContainer(int itemCount)
        {
            items = new Item[itemCount];
            Array.Fill(items, new Item { itemID = -1 });
        }

        public void SetItem(Item item, int slot)
        {
            items[slot] = item;
        }
        public bool AddItem(Item item, int slot, out int remainder)
        {
            remainder = 0;

            if (items[slot].itemID == -1)
            {
                SetItem(item, slot);
                return true;
            }
            if (items[slot].itemID == -2 || items[slot].stack > ItemManager.GetItemFromID(items[slot].itemID).maxStackSize) return false;

            if (items[slot].itemID != item.itemID) return false;

            items[slot].stack += item.stack;

            if (items[slot].stack > ItemManager.GetItemFromID(items[slot].itemID).maxStackSize)
            {
                remainder = items[slot].stack - ItemManager.GetItemFromID(items[slot].itemID).maxStackSize;
                items[slot].stack = ItemManager.GetItemFromID(items[slot].itemID).maxStackSize;
            }

            return true;
        }
        public bool TestAddItem(Item item, int slot)
        {
            if (items[slot].itemID == -1)
            {
                return true;
            }
            if (items[slot].itemID == -2 || items[slot].stack >= ItemManager.GetItemFromID(items[slot].itemID).maxStackSize) return false;

            return (items[slot].itemID == item.itemID);
        }
        public bool AddItem(Item item, out int remainder)
        {
            remainder = 0;
            (int slot, bool empty) bestSlot = (-1,true);
            for(int i = 0; i < items.Length; i++)
            {
                if (TestAddItem(item, i))
                {
                    if (bestSlot.slot == -1) bestSlot.slot = i;
                    else if(bestSlot.empty && item.itemID == items[i].itemID)
                    {
                        bestSlot = (i, false);
                    }
                }
            }

            if(bestSlot.slot >= 0)
            {
                AddItem(item, bestSlot.slot, out remainder);

                return true;
            }

            return false;
        }
        public Item TakeItemStack(int slot)
        {
            var send = items[slot];
            items[slot] = new Item { itemID = -1, stack = 0 };
            return send;
        }
        public Item? TakeItem(int slot, byte quantity)
        {
            if (items[slot].stack == 0 || items[slot].itemID == -1)
            {
                items[slot].itemID = -1;
                return null;
            }

            if (quantity >= items[slot].stack) return TakeItemStack(slot);

            items[slot].stack = (byte)(items[slot].stack-quantity);
            
            return new Item { itemID = items[slot].itemID, stack = quantity };
        }
        public Item PeekItem(int slot) => items[slot];

        public Item[] GetAllItems() => items;
        public void SetAllItems(Item[] items) => this.items = items;

        internal bool UseItem(int slot)
        {
            return items[slot].Use();
        }
    }
}
