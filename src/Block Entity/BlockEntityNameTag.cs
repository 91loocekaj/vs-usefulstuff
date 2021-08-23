﻿using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using System.IO;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Config;
using System.Text;

namespace UsefulStuff
{
    public class BlockEntityNameTag : BlockEntity
    {
        public string text = "";
        int color;
        int tempColor;
        ItemStack tempStack;

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            color = tree.GetInt("color");
            if (color == 0) color = ColorUtil.BlackArgb;

            text = tree.GetString("text", "");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("color", color);
            tree.SetString("text", text);
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.SaveText)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    text = reader.ReadString();
                    if (text == null) text = "";
                }

                color = tempColor;

                /*((ICoreServerAPI)api).Network.BroadcastBlockEntityPacket(
                    pos.X, pos.Y, pos.Z,
                    (int)EnumSignPacketId.NowText,
                    data
                );*/

                MarkDirty(true);

                // Tell server to save this chunk to disk again
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();

                // 0% chance to get back the item
                if (text == "")
                {
                    player.InventoryManager.TryGiveItemstack(tempStack);
                }
            }

            if (packetid == (int)EnumSignPacketId.CancelEdit && tempStack != null)
            {
                player.InventoryManager.TryGiveItemstack(tempStack);
                tempStack = null;
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.OpenDialog)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);

                    string dialogClassName = reader.ReadString();
                    string dialogTitle = reader.ReadString();
                    text = reader.ReadString();
                    if (text == null) text = "";

                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                    GuiDialogBlockEntityTextInput dlg = new GuiDialogBlockEntityTextInput(dialogTitle, Pos, text, Api as ICoreClientAPI, 160, 1);

                    dlg.OnCloseCancel = () =>
                    {
                        (Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumSignPacketId.CancelEdit, null);
                    };
                    dlg.TryOpen();
                }
            }


            if (packetid == (int)EnumSignPacketId.NowText)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    text = reader.ReadString();
                    if (text == null) text = "";
                }
            }
        }

        public void OnRightClick(IPlayer byPlayer)
        {
            if (byPlayer?.Entity?.Controls?.Sneak == true)
            {
                ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                if (hotbarSlot?.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists == true)
                {
                    JsonObject jobj = hotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
                    int r = jobj["red"].AsInt();
                    int g = jobj["green"].AsInt();
                    int b = jobj["blue"].AsInt();

                    tempColor = ColorUtil.ToRgba(255, r, g, b);
                    tempStack = hotbarSlot.TakeOut(1);
                    hotbarSlot.MarkDirty();


                    if (Api.World is IServerWorldAccessor)
                    {
                        byte[] data;

                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryWriter writer = new BinaryWriter(ms);
                            writer.Write("BlockEntityTextInput");
                            writer.Write("Sign Text");
                            writer.Write(text);
                            data = ms.ToArray();
                        }

                        ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                            (IServerPlayer)byPlayer,
                            Pos.X, Pos.Y, Pos.Z,
                            (int)EnumSignPacketId.OpenDialog,
                            data
                        );
                    }
                }
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            text = byItemStack?.Attributes?.GetString("nametagStore") ?? "";
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            if (text != null && text != "")
            {
                dsc.AppendLine(Lang.Get("usefulstuff:nametag-name"));
                dsc.AppendLine(text);
            }
            else
            {
                dsc.AppendLine(Lang.Get("usefulstuff:nametag-erase"));
            }
        }

    }
}
