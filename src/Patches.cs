using BuffStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace UsefulStuff
{
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

            if (entity.Properties.Attributes?.IsTrue("isMechanical") == false && __instance.ProjectileStack?.Attributes?.GetString("tip") == "tranq" && UsefulStuffConfig.Loaded.TranqArrowEnabled)
            {
                var poison = new TranqEffect();
                poison.Apply(entity);
                if (__instance.Alive) __instance.ProjectileStack?.Attributes?.RemoveAttribute("tip");
            }

            if (__instance.ProjectileStack?.Attributes?.GetString("tip") == "cardiac" && UsefulStuffConfig.Loaded.CardiacArrowEnabled)
            {
                if (entity.Properties.Attributes?.IsTrue("isMechanical") == false && BuffManager.GetActiveBuff(entity, "CardiacEffect") == null)
                {
                    var poison = new CardiacEffect();
                    poison.Init(1f, 0.75f);
                    poison.Apply(entity);
                }
                if (__instance.Alive) __instance.ProjectileStack?.Attributes?.RemoveAttribute("tip");
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

    [HarmonyPatch(typeof(CollectibleObject))]
    public class QuenchPatches
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return UsefulStuffConfig.Loaded.QuenchEnabled;
        }

        [HarmonyPatch("OnCreatedByCrafting")]
        [HarmonyPostfix]
        static void QuenchBonus(ItemSlot[] allInputslots, ItemSlot outputSlot)
        {
            if (outputSlot.Itemstack?.Collectible.Tool == null || !UsefulStuffConfig.Loaded.QuenchBonusMats.Contains(outputSlot.Itemstack.Collectible.FirstCodePart(1))) return;
            bool quench = false;
            foreach (ItemSlot slot in allInputslots)
            {
                if (slot.Itemstack?.Attributes.GetBool("quenched") == true)
                {
                    quench = true;
                    break;
                }
            }
            if (quench) outputSlot.Itemstack.Attributes.SetInt("durability", outputSlot.Itemstack.Collectible.Durability + (int)(outputSlot.Itemstack.Collectible.Durability * UsefulStuffConfig.Loaded.QuenchBonusMult));
        }
    }

    [HarmonyPatch(typeof(BlockEntityClayForm))]
    public class Clayback
    {
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPatch("OnBlockRemoved")]
        [HarmonyPrefix]
        static void GiveClay(ItemStack ___baseMaterial, BlockEntityClayForm __instance)
        {
            if (__instance.Api.Side == EnumAppSide.Client) return;

            int vox = 0;

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (__instance.Voxels[x, y, z]) vox++;
                    }
                }
            }

            int amount = (__instance.AvailableVoxels + vox - 89) / 25;
            if (amount <= 0) return;

            ItemStack clayBack = ___baseMaterial.Clone();
            clayBack.StackSize = amount;
            System.Diagnostics.Debug.WriteLine(__instance.Api.Side);


            __instance.Api.World.SpawnItemEntity(clayBack, __instance.Pos.ToVec3d().Add(0.5,0.5,0.5));

            __instance.AvailableVoxels = 0;
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        __instance.Voxels[x,y,z] = false;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CollectibleObject))]
    public class DamageBits
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false; 
                }
            }

            return true;
        }

        [HarmonyPatch("DamageItem")]
        [HarmonyPrefix]
        static void ChangeToScraps(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, CollectibleObject __instance, int amount = 1)
        {
            if (world.Side != EnumAppSide.Server || byEntity == null) return;
            IItemStack itemstack = itemslot.Itemstack;

            int leftDurability = itemstack.Attributes.GetInt("durability", __instance.GetDurability(itemstack));
            leftDurability -= amount;

            if (leftDurability <= 0 && __instance.Attributes?["brokenReturn"] != null)
            {
                JsonItemStack[] stacks = __instance.Attributes["brokenReturn"].AsObject<JsonItemStack[]>(new JsonItemStack[0]);
                if (stacks.Length <= 0) return;
                int randomItem = world.Rand.Next(stacks.Length);
                stacks[randomItem].Resolve(world, "brokentool", false);
                if (stacks[randomItem].ResolvedItemstack != null)
                {
                    world.SpawnItemEntity(stacks[randomItem].ResolvedItemstack.Clone(), byEntity.ServerPos.XYZ);
                }
            }
        }
    }

    [HarmonyPatch(typeof(BlockGroundStorage))]
    public class KilnCheck
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return true;
        }

        [HarmonyPatch("OnBlockInteractStart")]
        [HarmonyPrefix]
        static bool CheckForKiln(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref bool __result, BlockGroundStorage __instance)
        {
            BlockPos pos = blockSel.Position.Copy();

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    for (int y = -3; y < -1; y++)
                    {
                        pos.Set(blockSel.Position);
                        pos.Add(x, y, z);
                        BlockEntityFireBox fb = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityFireBox;
                        if (fb != null && fb.Lit)
                        {
                            if (world.Side == EnumAppSide.Client)
                            {
                                (world.Api as ICoreClientAPI).TriggerIngameError(__instance, "locked", Lang.Get("usefulstuff:kiln-lock"));
                            }

                            __result = false;
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BlockEntityGroundStorage))]
    public class KilnSet
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return true;
        }

        [HarmonyPatch("OnBlockBroken")]
        [HarmonyPrefix]
        static void CheckForKiln(BlockEntityGroundStorage __instance)
        {
            BlockPos pos = __instance.Pos.Copy();

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    for (int y = -3; y < -1; y++)
                    {
                        pos.Set(__instance.Pos);
                        pos.Add(x, y, z);
                        BlockEntityFireBox fb = __instance.Api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFireBox;
                        if (fb != null && fb.Lit)
                        {
                            fb.RemoveFromFiring(__instance.Pos);
                            return;
                        }
                    }
                }
            }
        }
    }
}
