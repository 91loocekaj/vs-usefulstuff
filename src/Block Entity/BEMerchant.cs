using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace UsefulStuff
{
    public class BEWelcomeMat : BlockEntity
    {
        RoomRegistry inn;
        long eid;
        bool occupied;
        EntityPartitioning entityUtil;
        bool noroom;
        bool nobed;
        bool nofood;
        double timer;
        ItemSlot foodsource;
        int maxTravelDays
        { get { return Block?.Attributes?["maxTravelDays"].AsInt(4) ?? 4; } }
        int minTravelDays
        { get { return Block?.Attributes?["minTravelDays"].AsInt(3) ?? 3; } }
        AssetLocation[] merchants
        {
            get { return AssetLocation.toLocations(Block.Attributes["merchants"].AsArray<string>(new string[0])); }
        }
        ItemStack[] gifts
        {
            get
            {
                List<ItemStack> stacks = new List<ItemStack>();
                foreach (JsonItemStack jstack in Block.Attributes["gifts"].AsObject<JsonItemStack[]>())
                {
                    jstack.Resolve(Api.World, "Tenant gift");
                    if (jstack.ResolvedItemstack != null) stacks.Add(jstack.ResolvedItemstack);
                }

                return stacks.ToArray();
            }
        }
        double nextTenantIn;
        double tenantLeavingIn;
        AssetLocation[] badFood
        {
            get { return AssetLocation.toLocations(Block.Attributes["badFood"].AsArray<string>(new string[0])); }
        }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inn = api.ModLoader.GetModSystem<RoomRegistry>();
            entityUtil = api.ModLoader.GetModSystem<EntityPartitioning>();
            if (timer == 0) timer = api.World.Calendar.TotalHours;

            RegisterGameTickListener(checkConditions, 10);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            Room place = inn.GetRoomForPosition(Pos);
            if (place == null) { noroom = true; return; }

            Api.World.BlockAccessor.WalkBlocks(new BlockPos(place.Location.MinX, place.Location.MinY, place.Location.MinZ), new BlockPos(place.Location.MaxX, place.Location.MaxY, place.Location.MaxZ),
                (block, posX, posY, posZ) =>
                {
                    if (block.Code.Path == Block.Code.Path && new BlockPos(posX, posY, posZ) != Pos)
                    {
                        Api.World.BlockAccessor.BreakBlock(Pos, null);
                    }
                });

            nextTenantIn = 24 * Api.World.Rand.Next(minTravelDays, maxTravelDays + 1);
            occupied = false;
        }

        public void checkConditions(float dt)
        {
            if (Api.World.BlockAccessor.GetBlock(Pos.UpCopy(1)).Id != 0) { Api.World.BlockAccessor.BreakBlock(Pos, null); return; }
            Room place = inn.GetRoomForPosition(Pos);
            //System.Diagnostics.Debug.WriteLine(place.ExitCount);
            Entity tenant = haveTenant();

            if (place == null) noroom = true; else noroom = false;
            if (hasBed(place.Location)) nobed = false; else nobed = true;
            if (hasFood(place.Location)) nofood = false; else nofood = true;

            if ((nobed || noroom) && tenant != null) { evictTenant(); nextTenantIn = Api.World.Rand.Next(minTravelDays, maxTravelDays + 1) * 24; }

            if (Api.World.Calendar.TotalHours - timer >= 1)
            {
                //System.Diagnostics.Debug.WriteLine(String.Format("Coming: {0}, Going {1}, Occupied {2}", nextTenantIn, tenantLeavingIn, occupied));
                timer += 1;
                if (occupied)
                {
                    tenantLeavingIn -= 1;
                    if (tenantLeavingIn <= 0)
                    {
                        occupied = false;
                        evictTenant(true);
                        nextTenantIn = Api.World.Rand.Next(minTravelDays, maxTravelDays + 1) * 24;
                    }
                }
                else
                {
                    nextTenantIn -= 1;
                    if (nextTenantIn <= 0 && !nobed && !nofood && !noroom)
                    {
                        if (Api.Side == EnumAppSide.Server && foodsource.TakeOut(1) != null) inviteGuest();
                        tenantLeavingIn = Block.Attributes["stayDays"].AsInt(1) * 24;
                        occupied = true;
                    }
                    else if (nextTenantIn <= 0) nextTenantIn = 24;
                }
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (noroom)
            {
                dsc.AppendLine(Lang.Get("Not built right!"));
                return;
            }
            if (nobed)
            {
                dsc.AppendLine(Lang.Get("No place to rest!"));
                return;
            }
            if (nofood)
            {
                dsc.AppendLine(Lang.Get("No food for next guest!"));
            }


            if (!occupied && nextTenantIn >= 24)
            {
                dsc.AppendLine(Lang.Get("Another merchant arrives in {0} days", Math.Floor(nextTenantIn/24)));
                return;
            }
            else if (!occupied && nextTenantIn < 24)
            {
                dsc.AppendLine(Lang.Get("Another merchant arrives in less than a day"));
                return;
            }
            else if (occupied && tenantLeavingIn >= 24)
            {
                dsc.AppendLine(Lang.Get("Merchant leaves in {0} days", Math.Floor(tenantLeavingIn/24)));
                return;
            }
            else
            {
                dsc.AppendLine(Lang.Get("Merchant leaves in less than a day"));
                return;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            occupied = tree.GetBool("occupied");
            eid = tree.GetLong("eid", 0);
            timer = tree.GetDouble("timer");
            nextTenantIn = tree.GetDouble("nextTenantIn");
            tenantLeavingIn = tree.GetDouble("tenantLeavingIn");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetLong("eid", eid);
            tree.SetBool("occupied", occupied);
            tree.SetDouble("timer", timer);
            tree.SetDouble("nextTenantIn", nextTenantIn);
            tree.SetDouble("tenantLeavingIn", tenantLeavingIn);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            evictTenant();
        }

        public Entity haveTenant()
        {
            Entity result = null;

            entityUtil.WalkEntities(Pos.ToVec3d(), 48, (search) =>
            {
                if (eid == search.EntityId && search.Alive)
                {
                    result = search;
                    return false;
                }

                return true;
            });





            return result;
        }

        public void evictTenant(bool enjoyedStay = false)
        {
            entityUtil.WalkEntities(Pos.ToVec3d(), 48, (search) =>
            {
                if (eid == search.EntityId)
                {
                    EntityTrader trader;
                    if (enjoyedStay && Api.Side == EnumAppSide.Server && (trader = search as EntityTrader) != null && search.Alive)
                    { 
                        DummySlot giftslot = new DummySlot(gifts[Api.World.Rand.Next(gifts.Length)]);

                        if (foodsource?.Inventory != null)
                        {
                            foreach (ItemSlot slot in foodsource.Inventory)
                            {
                                giftslot.TryPutInto(Api.World, slot, giftslot.StackSize);
                                if (giftslot.Empty) break;
                            }

                            if (!giftslot.Empty) Api.World.SpawnItemEntity(giftslot.Itemstack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                        }
                        else Api.World.SpawnItemEntity(giftslot.Itemstack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                    }

                    search.Die(EnumDespawnReason.Removed);
                    return false;
                }

                return true;
            });

            occupied = false;
        }

        public bool hasBed(Cuboidi area)
        {
            bool result = false;

            Api.World.BlockAccessor.WalkBlocks(new BlockPos(area.MinX, area.MinY, area.MinZ), new BlockPos(area.MaxX, area.MaxY, area.MaxZ),
                (block, posX, posY, posZ) =>
                {
                    if (block.Code.BeginsWith("game","bed"))
                    {
                        result = true;
                        return;
                    }
                });


            return result;
        }

        public bool hasFood(Cuboidi area)
        {
            bool result = false;

            Api.World.BlockAccessor.WalkBlocks(new BlockPos(area.MinX, area.MinY, area.MinZ), new BlockPos(area.MaxX, area.MaxY, area.MaxZ),
                (block, posX, posY, posZ) =>
                {
                    BlockEntityGenericContainer bec;

                    if ((bec = Api.World.BlockAccessor.GetBlockEntity(new BlockPos(posX, posY, posZ)) as BlockEntityGenericContainer) == null) return;

                    foreach (ItemSlot slot in bec.Inventory)
                    {
                        if (slot.Itemstack?.Collectible?.NutritionProps != null && !FindMatchCode(slot.Itemstack.Collectible.Code))
                        {
                            result = true;
                            foodsource = slot;
                            return;
                        }
                    }
                    
                });


            return result;
        }

        public void inviteGuest()
        {
            EntityProperties type = Api.World.GetEntityType(merchants[Api.World.Rand.Next(merchants.Length)]);
            Entity guest = Api.ClassRegistry.CreateEntity(type);
            guest.ServerPos.SetPos(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            guest.Pos.SetFrom(guest.ServerPos);
            Api.World.SpawnEntity(guest);
            eid = guest.EntityId;
        }

        public bool FindMatchCode(AssetLocation needle)
        {
            if (needle == null) return false;

            foreach (AssetLocation hay in badFood)
            {
                if (hay.Equals(needle)) return true;

                if (hay.IsWildCard && WildcardUtil.GetWildcardValue(hay, needle) != null) return true;
            }

            return false;
        }
    }
}
