using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API;
using HarmonyLib;
using Vintagestory.API.Common;
using System.Reflection;
using Vintagestory.ServerMods;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using Vintagestory.API.Util;
using System.Collections.Generic;
using Vintagestory.API.Config;
using System;

namespace UsefulStuff
{
    public class UsefulStuff : ModSystem
    {
        private Harmony harmony;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

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

            api.World.Config.SetBool("locustHordeEnabled", UsefulStuffConfig.Loaded.LocustHordeEnabled);
            api.World.Config.SetBool("tentbagEnabled", UsefulStuffConfig.Loaded.TentbagEnabled);
            api.World.Config.SetBool("shieldsEnabled", UsefulStuffConfig.Loaded.ShieldsEnabled);
            api.World.Config.SetBool("climbingRopeEnabled", UsefulStuffConfig.Loaded.ClimbingRopeEnabled);
            api.World.Config.SetBool("climbingPickEnabled", UsefulStuffConfig.Loaded.ClimbingPickEnabled);
            api.World.Config.SetBool("sluiceEnabled", UsefulStuffConfig.Loaded.SluiceEnabled);
            api.World.Config.SetBool("gliderEnabled", UsefulStuffConfig.Loaded.GliderEnabled);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("ItemShield", typeof(ItemShield));
            api.RegisterItemClass("Tentbag", typeof(ItemTentbag));
            api.RegisterItemClass("ItemGlider", typeof(ItemGlider));

            api.RegisterBlockClass("BlockRappelAnchor", typeof(BlockRappelAnchor));
            api.RegisterBlockClass("BlockClimbingRope", typeof(BlockClimbingRope));
            api.RegisterBlockClass("BlockLocustHorde", typeof(BlockLocustHorde));

            api.RegisterBlockEntityClass("BESluice", typeof(BESluice));

            api.RegisterEntityBehaviorClass("shield", typeof(EntityBehaviorShield));
            api.RegisterItemClass("ItemClimbingPick", typeof(ItemClimbingPick));

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

    [HarmonyPatch(typeof(EntityPlayer))]
    [HarmonyPatch("LightHsv", MethodType.Getter)]
    public class LanternClip
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return UsefulStuffConfig.Loaded.LanternClipOnEnabled;
        }

        [HarmonyPostfix]
        static void Postfix(EntityPlayer __instance, ref byte[] __result)
        {
            IInventory backpack = __instance.Player?.InventoryManager.GetInventory(GlobalConstants.backpackInvClassName + "-" + __instance.PlayerUID);
            if (backpack == null || backpack.Count < 5 || backpack[0].Itemstack?.Collectible.Code.Path.Contains("backpack") != true || backpack[4].Itemstack?.Collectible.Code.Path.Contains("lantern") != true) return;

            byte[] clipon = backpack[4].Itemstack?.Block?.LightHsv;
            if (clipon == null) return;

            if (__result == null)
            {
                __result = clipon;
                return;
            }

            float totalval = __result[2] + clipon[2];
            float t = clipon[2] / totalval;

            __result = new byte[]
            {
                    (byte)(clipon[0] * t + __result[0] * (1-t)),
                    (byte)(clipon[1] * t + __result[1] * (1-t)),
                    Math.Max(clipon[2], __result[2])
            };
        }
    }

    [HarmonyPatch(typeof(TreeGen))]
    [HarmonyPatch("GrowTree")]
    public class TreeGrowth
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPostfix]
        static void Prefix(ref float sizeModifier, ref float vineGrowthChance, ref float otherBlockChance)
        {
            sizeModifier *= UsefulStuffConfig.Loaded.TreeSizeMult;
            vineGrowthChance *= UsefulStuffConfig.Loaded.TreeVineMult;
            otherBlockChance *= UsefulStuffConfig.Loaded.TreeSpecialLogMult;
        }
    }

    public class UsefulStuffConfig
    {
        public static UsefulStuffConfig Loaded { get; set; } = new UsefulStuffConfig();

        public float SluiceEfficiency { get; set; } = 0.4f;

        public float SluiceSiftTime { get; set; } = 0.25f;

        public bool CrazyCaves { get; set; } = false;

        public bool CrazyCavesInsanityMode { get; set; } = false;

        public int ClimbingPickDamageRate { get; set; } = 100;

        public int GliderDamageRate { get; set; } = 100;

        public double GliderDescentRModifier { get; set; } = 0.75;

        public double GliderMaxStall { get; set; } = 0.6;

        public double GliderMinStall { get; set; } = -0.7;

        public double GliderThrustModifier { get; set; } = 0.02;

        public double GliderWindPushModifier { get; set; } = 0.005;

        public double GliderBackwardsAt { get; set; } = 0.2;

        public float TreeSizeMult { get; set; } = 1;

        public float TreeVineMult { get; set; } = 1;

        public float TreeSpecialLogMult { get; set; } = 1;


        #region Control Content

        public bool LocustHordeEnabled { get; set; } = true;

        public bool ShieldsEnabled { get; set; } = true;

        public bool ClimbingRopeEnabled { get; set; } = true;

        public bool ClimbingPickEnabled { get; set; } = true;

        public bool SluiceEnabled { get; set; } = true;

        public bool TentbagEnabled { get; set; } = true;

        public bool GliderEnabled { get; set; } = true;

        public bool LanternClipOnEnabled { get; set; } = true;
        #endregion
    }
}
