using Vintagestory.API.Common;
using System;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;

namespace UsefulStuff
{
    public class ItemGlider : Item
    {
        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            base.OnHeldIdle(slot, byEntity);

            if (!byEntity.OnGround && !byEntity.FeetInLiquid && !byEntity.Controls.IsFlying && !byEntity.Controls.IsClimbing && byEntity.ApplyGravity && byEntity.SidedPos.Motion.Y < 0)
            {
                //Handles duration loss
                int dur = slot.Itemstack.Attributes.GetInt("glidingDur");
                if (dur > UsefulStuffConfig.Loaded.GliderDamageRate)
                {
                    DamageItem(byEntity.World, byEntity, slot);
                    dur = 0;
                }
                slot.Itemstack.Attributes.SetInt("glidingDur", dur + 1);

                //Air brake to prevent hold jump exploit
                if (byEntity.Controls.Jump) return;

                //Handler to stop animation
                long handler = slot.Itemstack.TempAttributes.GetLong("handler");
                if (handler != 0) api.World.UnregisterCallback(handler);
                slot.Itemstack.TempAttributes.SetLong("handler", api.World.RegisterCallback((dt) => CancelGlide(byEntity), 100));

                //This animation only partly works because it gets blended with falling animation. But stopping falling animation causes head to spin VERTICALLY for some reason
                byEntity.StartAnimation("glide");

                double yVector = byEntity.SidedPos.GetViewVector().Y; //Returns a value between -1 (looking down) and 1 (looking up)
                double prevFall = byEntity.SidedPos.Motion.Y; //Save our previous elevation for horizontal speed gain calculations

                if (yVector <= UsefulStuffConfig.Loaded.GliderMaxStall && yVector >= UsefulStuffConfig.Loaded.GliderMinStall)
                {
                    //Slows down our fall
                    byEntity.SidedPos.Motion.Y *= Math.Max(0.10, UsefulStuffConfig.Loaded.GliderDescentRModifier + byEntity.SidedPos.Motion.Y);
                    byEntity.SidedPos.Motion.Y += byEntity.SidedPos.Motion.Y * (yVector < 0 ? -yVector : yVector);

                    //Gives us a horizontal push forward or backwards
                    Vec3d newVec = byEntity.SidedPos.HorizontalAheadCopy((UsefulStuffConfig.Loaded.GliderBackwardsAt - yVector) * UsefulStuffConfig.Loaded.GliderThrustModifier).XYZ;
                    byEntity.SidedPos.Motion.Z -= byEntity.SidedPos.Z - newVec.Z;
                    byEntity.SidedPos.Motion.X -= byEntity.SidedPos.X - newVec.X;
                }

                //Wind push. Note the wind does not seem fully implemented, only blows postively on X axis
                Vec3d windSpeed = byEntity.World.BlockAccessor.GetWindSpeedAt(byEntity.SidedPos.AsBlockPos);
                byEntity.SidedPos.Motion.X += windSpeed.X * UsefulStuffConfig.Loaded.GliderWindPushModifier;
                byEntity.SidedPos.Motion.Y += windSpeed.Y * UsefulStuffConfig.Loaded.GliderWindPushModifier;
                byEntity.SidedPos.Motion.Z += windSpeed.Z * UsefulStuffConfig.Loaded.GliderWindPushModifier;
            }
        }

        public void CancelGlide(EntityAgent byEntity)
        {
            byEntity.StopAnimation("glide");
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    HotKeyCode = "jump",
                    ActionLangCode = "usefulstuff:itemhelp-glider-airbrake",
                    MouseButton = EnumMouseButton.None
                }
            };
        }
    }
}
