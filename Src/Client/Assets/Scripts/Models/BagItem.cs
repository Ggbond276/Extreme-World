using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public struct BagItem
    {
        public ushort ItemId;
        public ushort Count;

        public BagItem(int itemId, int count)
        {
            this.ItemId =(ushort)itemId;
            this.Count = (ushort)count;
        }
    }
}
