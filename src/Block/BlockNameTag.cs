using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using System.Collections.Generic;
using Vintagestory.API.Config;
using System.Text;
using Vintagestory.GameContent;

namespace UsefulStuff
{
    public class BlockNameTag : Block
    {
        WorldInteraction[] interactions;

        WorldInteraction[] heldInteractions;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client) return;

            interactions = ObjectCacheUtil.GetOrCreate(api, "signBlockInteractions", () =>
            {
                List<ItemStack> stacksList = new List<ItemStack>();

                foreach (CollectibleObject collectible in api.World.Collectibles)
                {
                    if (collectible.Attributes?["pigment"].Exists == true)
                    {
                        stacksList.Add(new ItemStack(collectible));
                    }
                }

                return new WorldInteraction[] { new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-sign-write",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacksList.ToArray()
                    }
                };
            });

            heldInteractions = ObjectCacheUtil.GetOrCreate(api, "tabletInteractions", () =>
            {

                return new WorldInteraction[] { new WorldInteraction()
                    {
                        ActionLangCode = "usefulstuff:heldhelp-nameentity",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "usefulstuff:heldhelp-nameitem",
                        HotKeyCodes = new string[] { "sprint", "sneak"},
                        MouseButton = EnumMouseButton.Right,
                    }
                };
            });
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntity entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);

            if (byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack != null && entity is BlockEntityNameTag)
            {
                BlockEntityNameTag besigh = (BlockEntityNameTag)entity;
                besigh.OnRightClick(byPlayer);
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            string name = slot.Itemstack.Attributes.GetString("nametagStore");
            if (entitySel != null && byEntity.Controls.Sneak)
            {
                if (entitySel.Entity is EntityPlayer || entitySel.Entity is EntityTrader) return;
                handling = EnumHandHandling.PreventDefault;
                if (name != null)
                {
                    entitySel.Entity.WatchedAttributes.SetString("nametagName", name);
                    slot.Itemstack.Attributes.RemoveAttribute("nametagStore");
                }
                else
                {
                    entitySel.Entity.WatchedAttributes.RemoveAttribute("nametagName");
                }
                entitySel.Entity.WatchedAttributes.MarkPathDirty("nametagName");
                return;
            }

            if (byEntity.Controls.Sprint && byEntity.Controls.Sneak && blockSel == null && entitySel == null)
            {
                handling = EnumHandHandling.PreventDefault;
                if (slot.Itemstack.Attributes.GetBool("imprint"))
                {
                    slot.Itemstack.Attributes.SetBool("imprint", false);
                    ((byEntity as EntityPlayer)?.Player as IServerPlayer)?.SendMessage(GlobalConstants.InfoLogChatGroup, name != null ? Lang.Get("usefulstuff:imprintoff") : Lang.Get("usefulstuff:eraseoff"), EnumChatType.Notification);
                }
                else
                {
                    slot.Itemstack.Attributes.SetBool("imprint", true);
                    ((byEntity as EntityPlayer)?.Player as IServerPlayer)?.SendMessage(GlobalConstants.InfoLogChatGroup, name != null ? Lang.Get("usefulstuff:imprinton") : Lang.Get("usefulstuff:eraseon"), EnumChatType.Notification);
                }
            }
        }

        public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
        {
            if (priority == EnumMergePriority.DirectMerge && sinkStack.Attributes.GetBool("imprint") && !(sourceStack.Collectible is BlockNameTag))
            {
                string name = sinkStack.Attributes.GetString("nametagStore");
                if (name != null)
                {
                    sourceStack.Attributes.SetString("nametagName", name);
                    sinkStack.Attributes.RemoveAttribute("nametagStore");
                }
                else
                {
                    sourceStack.Attributes.RemoveAttribute("nametagName");
                }

                sinkStack.Attributes.SetBool("imprint", false);
            }

            return base.GetMergableQuantity(sinkStack, sourceStack, priority);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return heldInteractions;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack result =  base.OnPickBlock(world, pos);

            BlockEntityNameTag tag = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityNameTag;

            if (tag?.text != null && tag?.text != "")
            {
                result.Attributes.SetString("nametagStore", tag.text);
            }

            return result;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string name = inSlot.Itemstack.Attributes.GetString("nametagStore");

            if (name != null)
            {
                dsc.AppendLine(Lang.Get("usefulstuff:nametag-name"));
                dsc.AppendLine(name);
            }
            else
            {
                dsc.AppendLine(Lang.Get("usefulstuff:nametag-erase"));
            }
        }
    }
}
