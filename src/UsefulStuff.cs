using System;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace UsefulStuff
{
    public class UsefulStuff : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("ItemShield", typeof(ItemShield));

            api.RegisterBlockClass("BlockRappelAnchor", typeof(BlockRappelAnchor));
            api.RegisterBlockClass("BlockClimbingRope", typeof(BlockClimbingRope));

            api.RegisterBlockEntityClass("BESluice", typeof(BESluice));

            api.RegisterEntityBehaviorClass("shield", typeof(EntityBehaviorShield));
        }
    }
}
