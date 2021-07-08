using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using System.Linq;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using System.Text;
using System.Collections.Generic;

namespace UsefulStuff
{
    public class BlockEntityFireBox : BlockEntityContainer
    {
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "firebox";

        ILoadedSound ambientSound;
        InventoryGeneric inv;
        MultiblockStructure ms;
        ICoreClientAPI capi;
        public bool Lit;
        public double BurningUntilTotalHours;
        Dictionary<string, int> fuels;
        static SimpleParticleProperties smoke;
        bool[] firing = new bool[18];
        MeshData fullmesh;

        public BlockEntityFireBox()
        {
            inv = new InventoryGeneric(1, null, null);

            smoke = new SimpleParticleProperties(
                5, 15, ColorUtil.ToRgba(150, 80, 80, 80), new Vec3d(), new Vec3d(),
                new Vec3f(-0.2f, 1f, -0.2f), new Vec3f(0.2f, 2f, 0.2f), 6, 0, 0.5f, 1f, EnumParticleModel.Quad
            );
            smoke.SelfPropelled = true;
            smoke.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);
            smoke.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (ambientSound == null && api.Side == EnumAppSide.Client)
            {
                ambientSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game:sounds/environment/fire.ogg"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.3f,
                    Range = 8
                });
                if (Lit) ambientSound.Start();
            }

            capi = api as ICoreClientAPI;
            ms = Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
            ms.InitForUse(0);
            fuels = Block.Attributes["fuelTypes"].AsObject<Dictionary<string, int>>();
            RegisterGameTickListener(OnGameTick, 500);
        }

        public void OnGameTick(float dt)
        {
            if (!Lit) return;

            if (GetIncomplete() > 0)
            {
                Lit = false;
                firing = new bool[18];
                return;
            }

            if (Api.Side == EnumAppSide.Client)
            {
                smoke.MinPos.Set(Pos.X + 0.5 - 2 / 16.0, Pos.Y + 1 + 10 / 16f, Pos.Z + 0.5 - 2 / 16.0);
                smoke.AddPos.Set(4 / 16.0, 0, 4 / 16.0);
                Api.World.SpawnParticles(smoke, null);
            }

            if (Api.World.Calendar.TotalHours >= BurningUntilTotalHours)
            {
                OnFired();
            }
        }

        public void OnFired()
        {
            Lit = false;
            inv[0].Itemstack = null;
            int shouldFire = 0;

            BlockPos tmpPos = Pos.Copy();

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    for (int y = 2; y < 4; y++)
                    {
                        if (firing[shouldFire])
                        {
                            tmpPos.Set(Pos);
                            tmpPos.Add(x, y, z);
                            BlockEntityGroundStorage gs = Api.World.BlockAccessor.GetBlockEntity(tmpPos) as BlockEntityGroundStorage;
                            if (gs != null)
                            {
                                foreach (ItemSlot slot in gs.Inventory)
                                {
                                    if (slot.Itemstack?.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null && slot.Itemstack.Collectible.CombustibleProps.SmeltingType == EnumSmeltType.Fire)
                                    {
                                        int orgSize = slot.Itemstack.StackSize;
                                        slot.Itemstack = slot.Itemstack.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack.Clone();
                                        slot.Itemstack.StackSize = orgSize;
                                        slot.MarkDirty();
                                    }
                                }

                                gs.MarkDirty(true);
                            }
                        }
                        shouldFire++;
                    }
                }
            }

            firing = new bool[18];
            MarkDirty(true);
        }


        public void OnInteract(IPlayer player)
        {
            int incompleteCount = GetIncomplete();

            if (incompleteCount > 0)
            {
                if (capi != null)
                {
                    capi.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", incompleteCount));
                    ms.HighlightIncompleteParts(Api.World, player, Pos);
                }
            }
            else if (Api.Side == EnumAppSide.Client)
            {
                ms?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
            }
            else if (inv[0].Empty)
            {
                ItemSlot hand = player.InventoryManager.ActiveHotbarSlot;
                int amount = 0;
                if (hand.Itemstack != null && (amount = GetFuelAmount(hand.Itemstack.Collectible.Code.Path)) > 0 && hand.StackSize >= amount)
                {
                    inv[0].Itemstack = hand.TakeOut(amount);
                    inv[0].MarkDirty();
                    MarkDirty(true);
                }
            }
        }

        public bool CanIgnite(IPlayer player)
        {
            int incompleteCount = GetIncomplete();

            if (incompleteCount > 0)
            {
                if (capi != null)
                {
                    if (player != null) capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", incompleteCount));
                    ms.HighlightIncompleteParts(Api.World, player, Pos);
                }
                return false;
            }
            else if (Api.Side == EnumAppSide.Client)
            {
                ms?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
            }

            if (inv[0].Empty)
            {
                if (player != null) capi?.TriggerIngameError(this, "nofuel", Lang.Get("No fuel inserted!"));
                return false;
            }

            return !Lit;
        }

        public void TryIgnite(IPlayer byPlayer)
        {
            if (!CanIgnite(byPlayer)) return;
            BurningUntilTotalHours = Api.World.Calendar.TotalHours + UsefulStuffConfig.Loaded.PotKilnBurnHours;
            Lit = true;
            ambientSound?.Start();
            BlockPos tmpPos = Pos.Copy();
            int i = 0;

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    for (int y = 2; y < 4; y++)
                    {
                        tmpPos.Set(Pos);
                        tmpPos.Add(x, y, z);
                        BlockEntityGroundStorage gs = Api.World.BlockAccessor.GetBlockEntity(tmpPos) as BlockEntityGroundStorage;
                        if (gs != null)
                        {
                            firing[i] = true;
                        }
                        i++;
                    }
                }
            }
        }

        public int GetFuelAmount(string path)
        {
            int amount = 0;

            foreach (var fuel in fuels)
            {
                if (path == fuel.Key)
                {
                    return fuel.Value;
                }
            }

            return amount;
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (Api.Side == EnumAppSide.Client)
            {
                ms?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            if (Api.Side == EnumAppSide.Client)
            {
                ms?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
            }
        }

        public void RemoveFromFiring(BlockPos pos)
        {
            BlockPos tmpPos = Pos.Copy();
            int remove = 0;
            
            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    for (int y = 2; y < 4; y++)
                    {
                        tmpPos.Set(Pos);
                        tmpPos.Add(x, y, z);
                        if (tmpPos.X == pos.X && tmpPos.Y == pos.Y && tmpPos.Z == pos.Z) { firing[remove] = false; System.Diagnostics.Debug.WriteLine("Yes"); return; }
                        remove++;
                    }
                }
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (inv[0].Empty) return false;

            if (fullmesh == null)
            {
                capi.Tesselator.TesselateShape(Block, Api.Assets.TryGet("usefulstuff:shapes/block/firebox/full.json").ToObject<Shape>(), out fullmesh);
            }

            mesher.AddMeshData(fullmesh);
            return true;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("Lit", Lit);
            tree.SetDouble("BurningUntilTotalHours", BurningUntilTotalHours);
            tree["firing"] = new BoolArrayAttribute(firing);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Lit = tree.GetBool("Lit");
            BurningUntilTotalHours = tree.GetDouble("BurningUntilTotalHours");
            firing = (tree["firing"] as BoolArrayAttribute)?.value;
            if (firing == null) firing = new bool[18];
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            //base.GetBlockInfo(forPlayer, dsc);
            if (Lit)
            {
                dsc.AppendLine(Lang.Get("usefulstuff:firebox-burning"));
            }
            else if (!inv[0].Empty)
            {
                dsc.AppendLine(Lang.Get("usefulstuff:firebox-full"));
            }
            else dsc.AppendLine(Lang.Get("Empty"));
        }

        public int GetIncomplete()
        {
            int ignoregas = 0;
            int inc = ms.InCompleteBlockCount(Api.World, Pos, (block, wrong) => { if (wrong.Path.Contains("air") && block.Replaceable > 9500) ignoregas++; });

            return inc - ignoregas;
        }
    }
}
