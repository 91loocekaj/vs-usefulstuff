using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System.Collections.Generic;

namespace UsefulStuff
{
    public class BlockFireBox : Block
    {
        WorldInteraction[][] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            List<ItemStack> fuel = new List<ItemStack>();

            Dictionary<string, int> fuelCodes = Attributes["fuelTypes"].AsObject<Dictionary<string, int>>();

            if (fuelCodes != null && fuelCodes.Count > 0)
            {
                foreach (var val in fuelCodes)
                {
                    CollectibleObject fuelItem = api.World.GetItem(new AssetLocation(val.Key)) ?? api.World.GetBlock(new AssetLocation(val.Key)) as CollectibleObject;
                    if (fuelItem != null) fuel.Add(new ItemStack(fuelItem, val.Value));
                }
            }

            interactions = new WorldInteraction[][]
        {
            new WorldInteraction[] { new WorldInteraction()
            {
                MouseButton = EnumMouseButton.Right,
                Itemstacks = fuel.ToArray(),
                ActionLangCode = "blockhelp-bloomery-fuel"
            }},
            new WorldInteraction[] { new WorldInteraction()
            {
                MouseButton = EnumMouseButton.Right,
                Itemstacks = new ItemStack[] { new ItemStack(api.World.GetBlock(new AssetLocation("torch-up"))) },
                HotKeyCode = "sneak",
                ActionLangCode = "blockhelp-bloomery-ignite"
            }}
        };
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityFireBox befb = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFireBox;
            if (befb != null)
            {
                befb.OnInteract(byPlayer);
                (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return true;
            }



            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            BlockEntityFireBox beb = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFireBox;
            if (!beb.CanIgnite((byEntity as EntityPlayer)?.Player)) return EnumIgniteState.NotIgnitablePreventDefault;



            return secondsIgniting > 4 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;

            BlockEntityFireBox beb = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFireBox;
            beb?.TryIgnite((byEntity as EntityPlayer).Player);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BlockEntityFireBox fb = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityFireBox;
            if (fb == null) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            return fb.Inventory[0].Empty ? interactions[0] : interactions[1];
        }

    }
}
