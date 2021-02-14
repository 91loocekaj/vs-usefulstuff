using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace UsefulStuff
{
    public class BESluice : BlockEntityContainer, ITexPositionSource
    {
        MeshData currentMesh;
        ITexPositionSource blockTexPosSource;
        bool activeCurrent = false;

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                return blockTexPosSource[textureCode];
            }
        }

        public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;

        public override InventoryBase Inventory => inv;

        InventoryGeneric inv;

        public override string InventoryClassName => "sluice";

        public Dictionary<string, PanningDrop[]> sluiceBlocks;

        public BESluice()
        {
            inv = new InventoryGeneric(1, null, null, (id, inv) => new ItemSlotSluice(inv));
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            sluiceBlocks = api.World.BlockAccessor.GetBlock(new AssetLocation("game:pan-wooden")).Attributes["panningDrops"].AsObject<Dictionary<string, PanningDrop[]>>();

            RegisterGameTickListener(updateStep, 3000);

            if (Api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)api;
                if (currentMesh == null)
                {
                    string water = Api.World.BlockAccessor.GetBlock(posForward(1, 1, 0)).Code.Path;
                    activeCurrent = water.Contains("water") && water.Contains("1");
                    if (activeCurrent) currentMesh = GenMesh();
                }
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            if (Api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)Api;
                if (currentMesh == null)
                {
                    string water = Api.World.BlockAccessor.GetBlock(posForward(1, 1, 0)).Code.Path;
                    activeCurrent = water.Contains("water") && water.Contains("1");
                    if (activeCurrent) currentMesh = GenMesh();
                }
            }
        }

        public void updateStep(float dt)
        {
            checkForWater();

            if (activeCurrent && inv[0].Itemstack?.Block?.Attributes?.IsTrue("pannable") == true && Api.Side == EnumAppSide.Server)
            {
                string fromBlockCode = inv[0].Itemstack.Block.Code.ToShortString();
                PanningDrop[] drops = null;
                foreach (var val in sluiceBlocks.Keys)
                {
                    if (WildcardUtil.Match(val, fromBlockCode))
                    {
                        drops = sluiceBlocks[val];
                    }
                }

                if (drops == null)
                {
                    inv.DropAll(posForward(-1,0,0).ToVec3d());
                }

                string rocktype = Api.World.GetBlock(new AssetLocation(fromBlockCode))?.Variant["rock"];

                for (int l = 0; l < 8; l++)
                {
                    drops.Shuffle(Api.World.Rand);

                    for (int i = 0; i < drops.Length; i++)
                    {
                        PanningDrop drop = drops[i];

                        double rnd = Api.World.Rand.NextDouble();

                        float extraMul = 1f;
                        if (drop.DropModbyStat != null)
                        {
                            // Sluicing has low efficiency
                            extraMul = 0.5f;
                        }

                        float val = drop.Chance.nextFloat() * extraMul;

                        
                        ItemStack stack;

                        if (drops[i].Code.Path.Contains("{rocktype}"))
                        {
                            stack = Resolve(drops[i].Type, drops[i].Code.Path.Replace("{rocktype}", rocktype));
                        }
                        else
                        {
                            drop.Resolve(Api.World, "sluicing");
                            stack = drop.ResolvedItemstack;
                        }

                        if (rnd < val && stack != null)
                        {
                            stack = stack.Clone();
                            Api.World.SpawnItemEntity(stack, posForward(-1, 0, 0).ToVec3d().Add(0.5,0.1,0.5));
                            break;
                        }
                    }
                }

                inv[0].TakeOutWhole();
            }
        }

        MeshData GenMesh()
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;

            MeshData meshbase;
            Vec3f rotation = new Vec3f(Block.Shape.rotateX, Block.Shape.rotateY, Block.Shape.rotateZ);
            blockTexPosSource = capi.Tesselator.GetTexSource(Block);

            capi.Tesselator.TesselateShape("besluice", Api.Assets.TryGet("usefulstuff:shapes/block/wood/sluicecurrent.json").ToObject<Shape>(), out meshbase, this, rotation);

            return meshbase;
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(currentMesh);
            return currentMesh != null;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            //base.GetBlockInfo(forPlayer, dsc);
        }

        public BlockPos posForward(int offset, int height, int otheraxis)
        {
            switch (Block.Shape.rotateY)
            {
                case 0:
                    return Pos.AddCopy(otheraxis, height, -offset);
                case 180:
                    return Pos.AddCopy(otheraxis, height, offset);
                case 90:
                    return Pos.AddCopy(-offset, height, otheraxis);
                case 270:
                    return Pos.AddCopy(offset, height, otheraxis);
            }

            return Pos;
        }

        public bool checkForWater()
        {
            string water = Api.World.BlockAccessor.GetBlock(posForward(1, 1, 0)).Code.Path;
            activeCurrent = water.Contains("water") && water.Contains("1");

            if (activeCurrent && Api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)Api;
                if (currentMesh == null)
                {
                    currentMesh = GenMesh();
                    MarkDirty(true);
                }
            }
            else if (Api.Side == EnumAppSide.Client)
            {
                currentMesh = null;
                MarkDirty(true);
            }
            return activeCurrent;
        }

        public string GetBlockMaterialCode(ItemStack stack)
        {
            return stack?.Attributes?.GetString("materialBlockCode", null);
        }

        private ItemStack Resolve(EnumItemClass type, string code)
        {
            if (type == EnumItemClass.Block)
            {
                Block block = Api.World.GetBlock(new AssetLocation(code));
                if (block == null)
                {
                    Api.World.Logger.Error("Failed resolving panning block drop with code {0}. Will skip.", code);
                    return null;
                }
                return new ItemStack(block);

            }
            else
            {
                Item item = Api.World.GetItem(new AssetLocation(code));
                if (item == null)
                {
                    Api.World.Logger.Error("Failed resolving panning item drop with code {0}. Will skip.", code);
                    return null;
                }
                return new ItemStack(item);
            }
        }
    }
}
