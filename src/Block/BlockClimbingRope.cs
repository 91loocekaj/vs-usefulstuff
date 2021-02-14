using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace UsefulStuff
{
    public class BlockClimbingRope : Block
    {
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            Block attach = world.BlockAccessor.GetBlock(pos.UpCopy());
            if (!attach.Code.Path.Contains("climbingrope"))
            {
                world.BlockAccessor.BreakBlock(pos, null);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                return;
            }
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[] { new ItemStack(world.GetItem(new AssetLocation("game:rope"))) };
        }
    }

}
