using System.Collections.Generic;

namespace DeviceBatchGenerics.Support.ExtendedTreeView
{
    public class DirectoryItem : Item
    {
        public List<Item> Items { get; set; }
        public DirectoryItem()
        {
            Items = new List<Item>();
        }
    }
}
