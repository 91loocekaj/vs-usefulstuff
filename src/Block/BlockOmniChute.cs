using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System.Linq;

namespace UsefulStuff
{
    public class BlockOmniChute : Block, IBlockItemFlow
    {
        ICoreClientAPI capi;
        public string[] PullFaces => Attributes["pullFaces"].AsArray<string>(new string[0]);
        public string[] PushFaces => Attributes["pushFaces"].AsArray<string>(new string[0]);
        public string[] AcceptFaces => Attributes["acceptFromFaces"].AsArray<string>(new string[0]);

        public bool HasItemFlowConnectorAt(BlockFacing facing)
        {
            return PullFaces.Contains(facing.Code) || PushFaces.Contains(facing.Code) || AcceptFaces.Contains(facing.Code);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            capi = api as ICoreClientAPI;
        }

        public string GetOrientations(IWorldAccessor world, BlockPos pos)
        {
            string orientations =
                GetOmniChuteCode(world, pos, BlockFacing.NORTH) +
                GetOmniChuteCode(world, pos, BlockFacing.SOUTH) +
                GetOmniChuteCode(world, pos, BlockFacing.EAST) +
                GetOmniChuteCode(world, pos, BlockFacing.WEST) +
                GetOmniChuteCode(world, pos, BlockFacing.UP) +
                GetOmniChuteCode(world, pos, BlockFacing.DOWN)
            ;
            if (orientations == "n" || orientations == "s") orientations = "ns";
            if (orientations == "w" || orientations == "e") orientations = "ew";
            if (orientations == "u" || orientations == "d") orientations = "ud";
            if (orientations.Length == 0) orientations = "ns";
            return orientations;
        }

        private string GetOmniChuteCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
        {
            if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];
            return "";
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            string orientations = GetOrientations(world, blockSel.Position);
            Block block = world.BlockAccessor.GetBlock(CodeWithVariant("type", orientations));

            if (block == null) block = this;

            if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                return true;
            }

            return false;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            string orientations = GetOrientations(world, pos);

            AssetLocation newBlockCode = CodeWithVariant("type", orientations);

            if (!Code.Equals(newBlockCode))
            {
                Block block = world.BlockAccessor.GetBlock(newBlockCode);
                if (block == null) return;

                BlockEntityItemFlow bef = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityItemFlow;
                if (bef != null) bef.Inventory.DropAll(pos.ToVec3d().Add(0.5));

                world.BlockAccessor.SetBlock(block.BlockId, pos);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            }
            else
            {
                base.OnNeighbourBlockChange(world, pos, neibpos);
            }
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type" }, new string[] { "ns" }));
            return new ItemStack[] { new ItemStack(block) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type" }, new string[] { "ns" }));
            return new ItemStack(block);
        }



        public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
        {
            BlockPos onside = ownPos.AddCopy(side);
            Block block = world.BlockAccessor.GetBlock(onside);
            BlockEntity ent = world.BlockAccessor.GetBlockEntity(onside);

            return block is BlockOmniChute || (block as IBlockItemFlow)?.HasItemFlowConnectorAt(side.Opposite) == true || (ent is BlockEntityContainer && !(ent is BlockEntityItemFlow));
        }
    }
}
