using Vintagestory.API.Common;
using HarmonyLib;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;

namespace UsefulStuff
{
    public class BlockEntityChiselBench : BlockEntity
    {
        public bool OnInteract(ItemSlot cSlot, EntityAgent ent)
        {
            if (cSlot.Empty) return false;

            BlockPos workspace = Pos.UpCopy();
            BlockEntityMicroBlock mb = Api.World.BlockAccessor.GetBlockEntity(workspace) as BlockEntityMicroBlock;

            if (mb == null) return false;

            if (cSlot.Itemstack.Collectible.Tool == EnumTool.Chisel && mb.VolumeRel >= 1 && ent.Controls.Sneak)
            {
                cSlot.Itemstack.Collectible.DamageItem(Api.World, ent, cSlot, UsefulStuffConfig.Loaded.ChiselBenchRestoreCost);

                foreach (int id in mb.MaterialIds)
                {
                    Api.World.SpawnItemEntity(new ItemStack(Api.World.GetBlock(id), 1), workspace.ToVec3d().Add(0.5));
                }

                Api.World.BlockAccessor.SetBlock(0, workspace);

                return true;
            }

            if (cSlot.Itemstack.Collectible is BlockMicroBlock)
            {
                int[] addTo = (cSlot.Itemstack.Attributes?["materials"] as IntArrayAttribute).value;

                if (addTo == null || addTo.Length < 1) return false;

                mb.MaterialIds = mb.MaterialIds.AddRangeToArray(addTo);
                mb.MarkDirty(true);

                cSlot.TakeOut(1);
                cSlot.MarkDirty();

                return true;
            }

            return false;
        }
    }
}
