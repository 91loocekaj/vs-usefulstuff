using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace UsefulStuff
{
    public class EntityBehaviorShield : EntityBehavior
    {
        EntityAgent guard { get => entity as EntityAgent; }

        public float handleDefense(float dmg, DamageSource source)
        {
            if (guard != null && guard.Controls.Sneak && guard.LeftHandItemSlot?.Itemstack?.Collectible is ItemShield && source?.SourceEntity != null)
            {
                if (source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.AheadCopy(1).XYZ) >= source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.BehindCopy(1).XYZ)) return dmg;
                float mult = dmg + (dmg * (source.DamageTier - guard.LeftHandItemSlot.Itemstack.Collectible.ToolTier) * 0.25f);
                guard.World.PlaySoundAt(new AssetLocation("game:sounds/tool/breakreinforced.ogg"), guard);
                guard.LeftHandItemSlot.Itemstack.Collectible.DamageItem(entity.World, entity, guard.LeftHandItemSlot, Math.Max((int)mult, 1));
                return 0f;
            }
            else if (guard != null && guard.Controls.Sneak && guard.RightHandItemSlot?.Itemstack?.Collectible is ItemShield && source?.SourceEntity != null)
            {
                if (source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.AheadCopy(1).XYZ) >= source.SourceEntity.SidedPos.XYZ.SquareDistanceTo(entity.SidedPos.BehindCopy(1).XYZ)) return dmg;
                float mult = dmg + (dmg * (source.DamageTier - guard.RightHandItemSlot.Itemstack.Collectible.ToolTier) * 0.25f);
                guard.World.PlaySoundAt(new AssetLocation("game:sounds/tool/breakreinforced.ogg"), guard);
                guard.RightHandItemSlot.Itemstack.Collectible.DamageItem(entity.World, entity, guard.RightHandItemSlot, Math.Max((int)mult, 1));
                return 0f;
            }

            return dmg;
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
