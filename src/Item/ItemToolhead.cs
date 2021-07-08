using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using System.Text;

namespace UsefulStuff
{
    public class ItemToolhead : Item
    {
        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);

            if (UsefulStuffConfig.Loaded.QuenchEnabled && UsefulStuffConfig.Loaded.QuenchBonusMats.Contains(FirstCodePart(1)) && entityItem.Itemstack?.Attributes.GetBool("quenched") != true && entityItem.Itemstack?.Collectible.GetTemperature(entityItem.World, entityItem.Itemstack) > 600 && entityItem.Swimming)
            {
                entityItem.Itemstack.Attributes.SetBool("quenched", true);
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (UsefulStuffConfig.Loaded.QuenchEnabled && UsefulStuffConfig.Loaded.QuenchBonusMats.Contains(FirstCodePart(1)) && inSlot.Itemstack.Attributes.GetBool("quenched")) dsc.AppendLine(Lang.Get("usefulstuff:Quenched"));
        }

        public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
        {
            bool result = base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
            if (!UsefulStuffConfig.Loaded.QuenchEnabled || !UsefulStuffConfig.Loaded.QuenchBonusMats.Contains(FirstCodePart(1))) return result;

            result &= !inputStack.Attributes.GetBool("quenched") || inputStack.Attributes.GetTreeAttribute("temperature")?.GetFloat("temperature") < 200;

            return result;
        }
    }
}
