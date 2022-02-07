using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace UsefulStuff
{
    public class BlockEntityPotteryHolder : BlockEntityContainer
    {
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "finishedpottery";

        InventoryGeneric inv;

        public BlockEntityPotteryHolder()
        {
            inv = new InventoryGeneric(64, null, null);
        }
    }
}
