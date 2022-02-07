using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace UsefulStuff
{
    public class BlockChiselBench : Block
    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client || !UsefulStuffConfig.Loaded.ChiselBenchEnabled) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            List<ItemStack> chisels = new List<ItemStack>();
            List<ItemStack> blocks = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj.Tool == EnumTool.Chisel)
                {
                    chisels.Add(new ItemStack(obj));
                }
                else if (obj is BlockMicroBlock)
                {
                    blocks.Add(new ItemStack(obj));
                }
            }

            interactions = new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        Itemstacks = chisels.ToArray(),
                        ActionLangCode = "usefulstuff:blockhelp-breakdown",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right
                    },
                    new WorldInteraction()
                    {
                        Itemstacks = blocks.ToArray(),
                        ActionLangCode = "usefulstuff:blockhelp-addmat",
                        MouseButton = EnumMouseButton.Right
                    }
                };

        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!UsefulStuffConfig.Loaded.ChiselBenchEnabled) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            BlockEntityChiselBench cb = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChiselBench;

            if (byPlayer != null)
            {
                return cb.OnInteract(byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer.Entity);
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (UsefulStuffConfig.Loaded.ChiselBenchEnabled) return interactions;
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }
    }
}
