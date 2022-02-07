using Vintagestory.API.Common;

namespace UsefulStuff
{
    public class CollectibleBehaviorRemoveDecor : CollectibleBehavior
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);

            handHandling = EnumHandHandling.PreventDefault;
            if (blockSel != null && byEntity.World.BlockAccessor.GetDecors(blockSel.Position)[blockSel.Face.Index] != null)
            {
                byEntity.World.BlockAccessor.BreakDecor(blockSel.Position, blockSel.Face);
                byEntity.World.BlockAccessor.MarkChunkDecorsModified(blockSel.Position);
            }
        }

        public CollectibleBehaviorRemoveDecor(CollectibleObject collObj) : base(collObj)
        {
        }
    }
}
