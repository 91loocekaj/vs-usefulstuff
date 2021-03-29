using Vintagestory.API.Common;

namespace UsefulStuff
{
    public class ItemSlotSluice : ItemSlotSurvival
    {
        public ItemSlotSluice(InventoryGeneric inventory) : base(inventory)
        {
            //MaxSlotStackSize = 1;
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return base.CanTakeFrom(sourceSlot, priority) && sourceSlot.Itemstack?.Block?.Attributes?.IsTrue("pannable") == true;
        }

        public override bool CanHold(ItemSlot itemstackFromSourceSlot)
        {
            return base.CanHold(itemstackFromSourceSlot) && itemstackFromSourceSlot.Itemstack?.Block?.Attributes?.IsTrue("pannable") == true;
        }
    }
}
