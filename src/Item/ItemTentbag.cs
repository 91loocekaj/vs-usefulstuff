using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using System;

namespace UsefulStuff
{
    public class ItemTentbag : Item
    {
        internal  AssetLocation[] bannedBlocks
        {
            get { return Attributes["bannedBlocks"].AsArray<AssetLocation>(new AssetLocation[] { new AssetLocation("log-grown-*") }); }
        }

        internal AssetLocation[] allowedBlocks
        {
            get { return Attributes["allowedBlocks"].AsArray<AssetLocation>(new AssetLocation[] { new AssetLocation("cob-*") }); }
        }

        internal bool whitelist
        {
            get { return Attributes.IsTrue("whitelist"); }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (blockSel == null) return;
            handling = EnumHandHandling.PreventDefault;
            int size = UsefulStuffConfig.Loaded.TentRadius;
            int height = UsefulStuffConfig.Loaded.TentHeight;
            string debug = "instatent";

            if (slot.Itemstack.Attributes.GetString("tent") == null)
            {
                BlockSchematic bs = new BlockSchematic();
                BlockPos start = blockSel.Position.AddCopy(-size, 1, -size);
                BlockPos end = blockSel.Position.AddCopy(size, Math.Max(height, 3), size);
                bool canPack = true;

                byEntity.World.BlockAccessor.WalkBlocks(start, end, (block, posX, posY, posZ) => {
                    BlockPos pos = new BlockPos(posX, posY, posZ);
                    if (!canPack) return;
                    if (byPlayer != null && !byEntity.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.BuildOrBreak))
                    {
                        canPack = false;
                        return;
                    }
                    if ((!whitelist && FindMatchCode(block.Code, bannedBlocks)) || (whitelist && FindMatchCode(block.Code, allowedBlocks)))
                    {
                        canPack = false;
                        if (byEntity.World.Api is ICoreClientAPI capi)
                        {
                            capi.TriggerIngameError(this, "cannotpack", Lang.Get("usefulstuff:Cannot pack {0} in a bag!", block.GetPlacedBlockName(byEntity.World, pos)));

                        }
                        return;
                    }
                    BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
                    if (!UsefulStuffConfig.Loaded.TentKeepContents) (be as BlockEntityContainer)?.Inventory.DropAll(pos.ToVec3d());
                    if ((be is BlockEntityGroundStorage || be is BlockEntityItemPile) && !UsefulStuffConfig.Loaded.TentKeepContents) byEntity.World.BlockAccessor.BreakBlock(pos, null);
                });

                if (!canPack) return;
                end.Add(1, 1, 1);

                bs.AddArea(byEntity.World, start, end);
                bs.EntitiesUnpacked = new List<Entity>();
                bs.Pack(byEntity.World, start);
                ItemStack packed = new ItemStack(byEntity.World.GetItem(new AssetLocation(Attributes["packedBag"].AsString("usefulstuff:tentbag-packed"))), slot.StackSize);
                packed.Attributes.SetString("tent", bs.ToJson());
                if (slot.Itemstack.Attributes.GetString("nametagName") != null) packed.Attributes.SetString("nametagName", slot.Itemstack.Attributes.GetString("nametagName"));
                byEntity.World.SpawnItemEntity(packed, blockSel.Position.ToVec3d().Add(0, 1, 0));

                end.Add(-1, -1, -1);
                byEntity.World.BulkBlockAccessor.WalkBlocks(start, end, (block, posX, posY, posZ) => { if (block.BlockId != 0) byEntity.World.BulkBlockAccessor.SetBlock(0, new BlockPos(posX, posY, posZ)); });
                byEntity.World.BulkBlockAccessor.Commit();
                slot.TakeOutWhole();
                byEntity.ReceiveSaturation(-UsefulStuffConfig.Loaded.TentBuildEffort);
            }
            else
            {
                BlockPos start = blockSel.Position.AddCopy(-size, 0, -size);
                BlockPos end = blockSel.Position.AddCopy(size, Math.Max(height, 3), size);
                bool canPlace = true;

                byEntity.World.BlockAccessor.WalkBlocks(start, end, (block, posX, posY, posZ) => 
                {
                    BlockPos pos = new BlockPos(posX, posY, posZ);
                    if (!canPlace) return;
                    if (byPlayer != null && !byEntity.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.BuildOrBreak))
                    {
                        canPlace = false;
                        return;
                    }
                    if (pos.Y == start.Y && !block.SideSolid[BlockFacing.indexUP])
                    {
                        canPlace = false;
                        if (byEntity.World.Api is ICoreClientAPI capi)
                        {
                            capi.TriggerIngameError(this, "cannotpack", Lang.Get("usefulstuff:Cannot unpack here, need solid ground!"));

                        }
                        return;
                    }
                    if (pos.Y != start.Y && block.Replaceable < 9505)
                    {
                        canPlace = false;
                        if (byEntity.World.Api is ICoreClientAPI capi)
                        {
                            capi.TriggerIngameError(this, "cannotpack", Lang.Get("usefulstuff:Cannot unpack here, need a clear area!"));

                        }
                        return;
                    }
                });
                if (!canPlace) return;

                BlockSchematic bs = BlockSchematic.LoadFromString(slot.Itemstack.Attributes.GetString("tent"), ref debug);
                bs.ReplaceMode = EnumReplaceMode.ReplaceAll;
                start = bs.AdjustStartPos(start.Add(size, 1, size), EnumOrigin.BottomCenter);
                bs.Place(byEntity.World.BulkBlockAccessor, byEntity.World, start);
                byEntity.World.BulkBlockAccessor.Commit();
                bs.PlaceEntitiesAndBlockEntities(byEntity.World.BlockAccessor, byEntity.World, start);
                ItemStack empty = new ItemStack(byEntity.World.GetItem(new AssetLocation(Attributes["emptyBag"].AsString("usefulstuff:tentbag-empty"))), slot.StackSize);
                if (slot.Itemstack.Attributes.GetString("nametagName") != null) empty.Attributes.SetString("nametagName", slot.Itemstack.Attributes.GetString("nametagName"));
                byEntity.World.SpawnItemEntity(empty, blockSel.Position.ToVec3d().Add(0, 1, 0));
                slot.TakeOutWhole();
                byEntity.ReceiveSaturation(-UsefulStuffConfig.Loaded.TentBuildEffort);
            }

        }

        public bool FindMatchCode(AssetLocation needle, AssetLocation[] haystack)
        {
            if (needle == null) return false;

            foreach (AssetLocation hay in haystack)
            {
                if (hay.Equals(needle)) return true;

                if (hay.IsWildCard && WildcardUtil.GetWildcardValue(hay, needle) != null) return true;
            }

            return false;
        }
    }
}
