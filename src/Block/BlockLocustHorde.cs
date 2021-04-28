using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace UsefulStuff
{
    public class BlockLocustHorde : Block
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            double rnd = world.Rand.NextDouble();
            float level = pos.Y - 1 < world.SeaLevel ? 5 - (5 * (pos.Y + 1) / world.SeaLevel) : 0.5f;

            if (api.Side == EnumAppSide.Server)
            {
                long herdId = (api as ICoreServerAPI).WorldManager.GetNextUniqueId();
                EntityProperties enemy = null;
                if (rnd <= 0.01)
                {
                    enemy = world.GetEntityType(new AssetLocation("bell-normal"));
                    if (enemy != null) DoSpawn(enemy, pos.ToVec3d().Add(0.5, 0.5, 0.5), herdId);
                }
                else if (rnd <= 0.05)
                {
                    enemy = world.GetEntityType(new AssetLocation("locust-corrupt-sawblade"));
                    if (enemy != null) DoSpawn(enemy, pos.ToVec3d().Add(0.5, 0.5, 0.5), herdId);
                }
                else if (rnd < 0.50)
                {
                    for (int i = 0; i < (int)Math.Ceiling(level); i++)
                    {
                        enemy = world.GetEntityType(new AssetLocation("locust-corrupt"));
                        if (enemy != null) DoSpawn(enemy, pos.ToVec3d().Add(0.5, 0.5, 0.5), herdId);
                    }
                }
                else
                {
                    for (int i = 0; i < (int)Math.Ceiling(level); i++)
                    {
                        enemy = world.GetEntityType(new AssetLocation("locust-bronze"));
                        if (enemy != null) DoSpawn(enemy, pos.ToVec3d().Add(0.5, 0.5, 0.5), herdId);
                    }
                }
            }

                base.OnBlockBroken(world, pos, byPlayer, (float)Math.Ceiling(level));
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            LootList locusthordeitems;

            if (pos.Y <= world.SeaLevel * 0.5)
            {
                locusthordeitems = LootList.Create(dropQuantityMultiplier,
            LootItem.Item(0.5f, 10, 20, "ore-lignite", "ore-bituminouscoal"),
                LootItem.Item(0.7f, 10, 20, "nugget-nativecopper", "ore-quartz", "nugget-galena"),
                LootItem.Item(1f, 10, 20, "nugget-galena", "nugget-cassiterite", "nugget-sphalerite", "nugget-bismuthinite"),
                LootItem.Item(1f, 10, 20, "nugget-limonite", "nugget-nativegold", "nugget-ilmenite", "nugget-nativesilver", "nugget-magnetite"),
                LootItem.Item(0.6f, 1, 7, "gear-rusty"),
                LootItem.Item(0.5f, 1, 3, "gem-diamond-rough", "gem-emerald-rough", "gem-olivine_peridot-rough"),
                LootItem.Item(0.01f, 1, 1, "gear-temporal")
            );
            }
            else if (pos.Y <= world.SeaLevel)
            {
                locusthordeitems = LootList.Create(dropQuantityMultiplier,
            LootItem.Item(1, 10, 20, "ore-lignite", "ore-bituminouscoal"),
                LootItem.Item(1, 10, 20, "nugget-nativecopper", "ore-quartz", "nugget-galena"),
                LootItem.Item(0.5f, 10, 20, "nugget-cassiterite", "nugget-sphalerite", "nugget-bismuthinite"),
                LootItem.Item(0.5f, 10, 20, "nugget-limonite", "nugget-nativegold", "nugget-ilmenite", "nugget-nativesilver", "nugget-magnetite"),
                LootItem.Item(0.6f, 1, 4, "gear-rusty")
                );
            }
            else
            {
                locusthordeitems = LootList.Create(dropQuantityMultiplier,
            LootItem.Item(1, 10, 20, "ore-lignite", "ore-bituminouscoal"),
                LootItem.Item(1, 10, 20, "nugget-nativecopper", "ore-quartz", "nugget-galena")
                );
            }

            return locusthordeitems.GenerateLoot(world, byPlayer);
        }

        protected void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdid)
        {
            Entity entity = api.World.ClassRegistry.CreateEntity(entityType);

            EntityAgent agent = entity as EntityAgent;
            if (agent != null) agent.HerdId = herdid;

            entity.ServerPos.SetPos(spawnPosition);
            entity.ServerPos.SetYaw((float)api.World.Rand.NextDouble() * GameMath.TWOPI);
            entity.Pos.SetFrom(entity.ServerPos);
            entity.Attributes.SetString("origin", "locusthorde");
            api.World.SpawnEntity(entity);
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            BlockPos downPos = pos.DownCopy();
            if (!blockAccessor.GetBlock(downPos).CanAttachBlockAt(blockAccessor, this, pos, BlockFacing.UP))
            {
                for (int i = 0; i < 15; i++)
                {
                    downPos.Y--;
                    Block stone = blockAccessor.GetBlock(downPos);
                    if (stone.CanAttachBlockAt(blockAccessor, this, pos, BlockFacing.UP) && stone.BlockMaterial == EnumBlockMaterial.Stone && downPos.Y < api.World.SeaLevel)
                    {
                        string rocktype;
                        if (stone.Variant.TryGetValue("rock", out rocktype))
                        {
                            pos.Y = downPos.Y + 1;
                            blockAccessor.SetBlock(blockAccessor.GetBlock(CodeWithPart(rocktype, 1)).BlockId, pos);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
