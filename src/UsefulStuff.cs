using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API;
using HarmonyLib;
using Vintagestory.API.Common;
using System.Reflection;
using Vintagestory.ServerMods;

namespace UsefulStuff
{
    public class UsefulStuff : ModSystem
    {
        private Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("ItemShield", typeof(ItemShield));

            api.RegisterBlockClass("BlockRappelAnchor", typeof(BlockRappelAnchor));
            api.RegisterBlockClass("BlockClimbingRope", typeof(BlockClimbingRope));
            api.RegisterBlockClass("BlockLocustHorde", typeof(BlockLocustHorde));

            api.RegisterBlockEntityClass("BESluice", typeof(BESluice));

            api.RegisterEntityBehaviorClass("shield", typeof(EntityBehaviorShield));

            try
            {
                UsefulStuffConfig FromDisk;
                if ((FromDisk = api.LoadModConfig<UsefulStuffConfig>("UsefulStuffConfig.json")) == null)
                {
                    api.StoreModConfig<UsefulStuffConfig>(UsefulStuffConfig.Loaded, "UsefulStuffConfig.json");
                }
                else UsefulStuffConfig.Loaded = FromDisk;
            }
            catch
            {
                api.StoreModConfig<UsefulStuffConfig>(UsefulStuffConfig.Loaded, "UsefulStuffConfig.json");
            }

            harmony = new Harmony("com.jakecool19.usefulstuff.worldgen");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }
    }

    [HarmonyPatch(typeof(GenCaves), "CarveTunnel")]
    public class CrazyCaves
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return UsefulStuffConfig.Loaded.CrazyCaves;
        }

        [HarmonyPrefix]
        static void Prefix(ref float horizontalSize, ref float verticalSize, ref bool extraBranchy, ref bool largeNearLavaLayer)
        {
            horizontalSize *= 1.5f;
            verticalSize *= 1.5f;
            extraBranchy = UsefulStuffConfig.Loaded.CrazyCavesInsanityMode;
            largeNearLavaLayer = true;
        }
    }

    public class UsefulStuffConfig
    {
        public static UsefulStuffConfig Loaded { get; set; } = new UsefulStuffConfig();

        public float SluiceEfficiency { get; set; } = 0.4f;

        public float SluiceSiftTime { get; set; } = 0.25f;

        public bool CrazyCaves { get; set; } = false;

        public bool CrazyCavesInsanityMode { get; set; } = false;
    }
}
