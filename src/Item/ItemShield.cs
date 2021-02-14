using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace UsefulStuff
{
    public class ItemShield : Item
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get("Protection tier: {0}", ToolTier));
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                        ActionLangCode = "usefulstuff:itemhelp-shield",
                        MouseButton = EnumMouseButton.None,
                        HotKeyCode = "sneak"
                }
            };
        }
    }
}
