using BuffStuff;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace UsefulStuff
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class CardiacEffect : Buff
    {
        public float DamagePercent = 0.75f;
        public double Hours = 1;
        public static SimpleParticleProperties EffectParticles = new SimpleParticleProperties(1, 1, ColorUtil.ColorFromRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f(), 0.2f, 0.5f, 1, 1, EnumParticleModel.Cube);

        public void Init(double time = 1f, float damagePercent = 0.75f)
        {
            DamagePercent = damagePercent;
            Hours = time;
        }

        public override void OnStart()
        {
            SetExpiryInGameHours(Hours);
        }

        public override void OnExpire()
        {
            EntityBehaviorHealth bh = entity.GetBehavior<EntityBehaviorHealth>();
            if (bh != null)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal }, DamagePercent * bh.MaxHealth);
            }
        }

        public override void OnTick()
        {
            EffectParticles.MinPos = entity.SidedPos.XYZ;
            EffectParticles.AddPos = new Vec3d(0.5, 0.5, 0.5);
            entity.World.SpawnParticles(EffectParticles);
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class TranqEffect : Buff
    {
        public int StackAmount = 1;

        public override void OnStart()
        {
            Entity.Stats.Set("walkspeed", "tranquilizer", -0.1f, true);
            SetExpiryInGameHours(1);
        }
        public override void OnStack(Buff oldBuff)
        {
            StackAmount += ((TranqEffect)oldBuff).StackAmount;
            Entity.Stats.Set("walkspeed", "tranquilizer", -0.1f * StackAmount, true);
            SetExpiryInGameHours(1);
        }
        public override void OnExpire()
        {
            Entity.Stats.Remove("walkspeed", "tranquilizer");
        }
    }
}
