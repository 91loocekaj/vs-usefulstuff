using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace UsefulStuff
{
    public class BlockRappelAnchor : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer.Entity.Controls.Sneak != true) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            BlockPos endpoint = blockSel.Position.DownCopy();

            if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Code?.Path == "rope" && world.BlockAccessor.GetBlock(endpoint).Id == 0)
            {
                //Deploy rope
                int onHand = byPlayer.InventoryManager.ActiveHotbarSlot.StackSize;
                Block ropeBlock = world.BlockAccessor.GetBlock(CodeWithPart("section", 1));

                while (onHand > 0 && world.BlockAccessor.GetBlock(endpoint).Id == 0)
                {
                    world.BlockAccessor.SetBlock(ropeBlock.Id, endpoint);
                    endpoint.Down();
                    onHand--;
                }

                byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(byPlayer.InventoryManager.ActiveHotbarSlot.StackSize - onHand);

                return true;
            }
            else if (isRope(endpoint) && byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
            {
                //Retract rope
                int ropeTied = 0;


                while (isRope(endpoint))
                {
                    ropeTied++;

                    endpoint.Down();

                }

                if (ropeTied > 0)
                {
                    ItemStack retracted = new ItemStack(api.World.GetItem(new AssetLocation("game:rope")), ropeTied);
                    if (!byPlayer.InventoryManager.TryGiveItemstack(retracted))
                    {
                        world.SpawnItemEntity(retracted, byPlayer.Entity.SidedPos.XYZ);
                    }
                }

                while (endpoint.Y < blockSel.Position.Y)
                {
                    if (isRope(endpoint)) world.BlockAccessor.SetBlock(0, endpoint);
                    endpoint.Up();
                }

                return true;
            }

            return true;
        }

        public bool isRope(BlockPos rope)
        {
            if (api.World.BlockAccessor.GetBlock(rope).Code.Path.Contains("climbingrope-section")) return true;

            return false;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (!isRope(selection.Position.Down())) return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                        ActionLangCode = "usefulstuff:blockhelp-anchor-deploy",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = new ItemStack[] { new ItemStack(api.World.GetItem(new AssetLocation("game:rope"))) }
                },
            };

            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                        ActionLangCode = "usefulstuff:blockhelp-anchor-retract",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                }
            };
        }
    }

}
