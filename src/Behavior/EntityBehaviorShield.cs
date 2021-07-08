using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace UsefulStuff
{
    public class EntityBehaviorShield : EntityBehavior
    {
        public float ShieldAttackDown
        {
            get { return shieldDown; }
            set { shieldDown = Math.Max(0, value); }
        }
        public float ShieldFatigueTime
        {
            get { return shieldFatigue; }
            set { shieldFatigue = Math.Max(0, value); }
        }
        public bool LeftGuardUp
        {
            get { return guard != null && guard.Controls.Sneak && guard.LeftHandItemSlot?.Itemstack?.Collectible is ItemShield; }
        }
        public bool RightGuardUp
        {
            get { return guard != null && guard.Controls.Sneak && guard.RightHandItemSlot?.Itemstack?.Collectible is ItemShield; }
        }

        public float MaxShieldTime
        {
            get { return (guard.LeftHandItemSlot?.Itemstack?.Collectible as ItemShield)?.ShieldTime ?? (guard.RightHandItemSlot.Itemstack.Collectible as ItemShield).ShieldTime; }
        }

        float shieldFatigue = 0;
        float shieldDown = 0;
        EntityAgent guard { get => entity as EntityAgent; }

        public float handleDefense(float dmg, DamageSource source)
        {
            if (source?.SourceEntity != null && LeftGuardUp && shieldDown <= 0)
            {
                if (entity.World.Rand.NextDouble() <= (shieldFatigue / MaxShieldTime) * UsefulStuffConfig.Loaded.ShieldFatigueMultiplier) return dmg;
                if (source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.AheadCopy(1).XYZ) > source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.BehindCopy(1).XYZ)) return dmg;
                float mult = dmg + (dmg * (source.DamageTier - guard.LeftHandItemSlot.Itemstack.Collectible.ToolTier) * 0.25f * entity.Stats.GetBlended("armorDurabilityLoss"));
                guard.World.PlaySoundAt(new AssetLocation("game:sounds/tool/breakreinforced.ogg"), guard);
                guard.LeftHandItemSlot.Itemstack.Collectible.DamageItem(entity.World, entity, guard.LeftHandItemSlot, Math.Max((int)mult, 1));
                return 0f;
            }
            else if (source?.SourceEntity != null && RightGuardUp && shieldDown <= 0)
            {
                if (entity.World.Rand.NextDouble() <= (shieldFatigue / MaxShieldTime) * UsefulStuffConfig.Loaded.ShieldFatigueMultiplier) return dmg;
                if (source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.AheadCopy(1).XYZ) > source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.BehindCopy(1).XYZ)) return dmg;
                float mult = dmg + (dmg * (source.DamageTier - guard.RightHandItemSlot.Itemstack.Collectible.ToolTier) * 0.25f * entity.Stats.GetBlended("armorDurabilityLoss"));
                guard.World.PlaySoundAt(new AssetLocation("game:sounds/tool/breakreinforced.ogg"), guard);
                guard.RightHandItemSlot.Itemstack.Collectible.DamageItem(entity.World, entity, guard.RightHandItemSlot, Math.Max((int)mult, 1));
                return 0f;
            }

            return dmg;
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

            ShieldAttackDown -= deltaTime;
            if ((guard.Controls.LeftMouseDown || guard.Controls.RightMouseDown) && (RightGuardUp || LeftGuardUp)) ShieldAttackDown = UsefulStuffConfig.Loaded.ShieldDownAfterAttack;

            if (LeftGuardUp || RightGuardUp) ShieldFatigueTime += deltaTime;
            else ShieldFatigueTime -= deltaTime * UsefulStuffConfig.Loaded.ShieldRecoveryRate;
        }

        public override void OnEntityLoaded()
        {
            EntityBehaviorHealth bh = entity.GetBehavior<EntityBehaviorHealth>();
            if (bh != null)
            {
                bh.onDamaged += (dmg, source) => handleDefense(dmg, source);
            }
        }

        public override void OnEntitySpawn()
        {
            EntityBehaviorHealth bh = entity.GetBehavior<EntityBehaviorHealth>();
            if (bh != null)
            {
                bh.onDamaged += (dmg, source) => handleDefense(dmg, source);
            }
        }
        public EntityBehaviorShield(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "shield";
        }
    }
}
