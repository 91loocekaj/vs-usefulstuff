using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace BuffStuff
{
    //An amazing status effect system made by Goxmeor

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    internal class SerializedBuff
    {
        public string id;
        public double timeRemainingInDays;
        public int expireTick;
        public int tickCounter;
        public byte[] data;
    }

    public class BuffManager
    {
        public static readonly int TICK_MILLISECONDS = 250;
        private static bool isInitialized = false;
        private static Dictionary<string, Type> buffTypes = new Dictionary<string, Type>();
        private static Dictionary<Type, string> buffTypeToIds = new Dictionary<Type, string>();
        private static Dictionary<Entity, Dictionary<string, Buff>> activeBuffsByEntityAndBuffId = new Dictionary<Entity, Dictionary<string, Buff>>();
        private static Dictionary<string, List<SerializedBuff>> inactiveBuffsByPlayerUid = new Dictionary<string, List<SerializedBuff>>();
        public static void RegisterBuffType(string buffTypeId, Type buffType)
        {
            if (!isInitialized) { throw new System.Exception("BuffManager.RegisterBuff: must call BuffManager.Initialize() first!"); }
            if (buffTypes.ContainsKey(buffTypeId)) { throw new System.Exception($"BuffManager.RegisterBuff: buffId already registered: {buffTypeId}"); }
            buffTypes[buffTypeId] = buffType;
            buffTypeToIds[buffType] = buffTypeId;
        }
        private static SerializedBuff serializeBuff(Buff buff)
        {
            MemoryStream memoryStream = new MemoryStream();
            RuntimeTypeModel.Default.Serialize(memoryStream, buff);
            var serializedBuffObjectBytes = memoryStream.ToArray();
            return new SerializedBuff()
            {
                id = buffTypeToIds[buff.GetType()],
                timeRemainingInDays = buff.ExpireTimestampInDays - api.World.Calendar.TotalDays,
                expireTick = buff.ExpireTick,
                tickCounter = buff.TickCounter,
                data = serializedBuffObjectBytes // SerializerUtil.Serialize<object>(buff)
            };
        }
        private static Buff deserializeBuff(SerializedBuff serializedBuff, Entity entity)
        {
            MemoryStream serializedBuffMemoryStream = new MemoryStream(serializedBuff.data);
            Buff buff;
            try
            {
                buff = (Buff)RuntimeTypeModel.Default.Deserialize(serializedBuffMemoryStream, null, buffTypes[serializedBuff.id]);
            }
            catch (Exception e)
            {
                api.Logger.Error("BuffStuff.BuffManager failed to deserialize a buff with ID {0}: {1}", serializedBuff.id, e.ToString());
                return null;
            }
            finally
            {
                serializedBuffMemoryStream.Dispose();
            }
            buff.entity = entity;
            buff.ExpireTimestampInDays = api.World.Calendar.TotalDays + serializedBuff.timeRemainingInDays;
            buff.TickCounter = serializedBuff.tickCounter;
            buff.ExpireTick = serializedBuff.expireTick;
            return buff;
        }
        private static ICoreAPI api;
        internal static double Now { get { return api.World.Calendar.TotalDays; } }
        public static void Initialize(ICoreAPI api_, ModSystem mod)
        {
            api = api_;
            if (api.Side == EnumAppSide.Server)
            {
                buffTypes.Clear();
                activeBuffsByEntityAndBuffId.Clear();
                inactiveBuffsByPlayerUid.Clear();

                var sapi = api as ICoreServerAPI;
                sapi.Event.SaveGameLoaded += () => {
                    var data = sapi.WorldManager.SaveGame.GetData($"{mod.Mod.Info.ModID}:BuffStuff");
                    if (data != null)
                    {
                        inactiveBuffsByPlayerUid = SerializerUtil.Deserialize<Dictionary<string, List<SerializedBuff>>>(data);
                    }
                };
                sapi.Event.GameWorldSave += () => {
                    var activeAndInactiveBuffsByPlayerUid = new Dictionary<string, List<SerializedBuff>>(inactiveBuffsByPlayerUid); // shallow clone dictionary
                    foreach (var entityActiveBuffsByBuffIdPair in activeBuffsByEntityAndBuffId)
                    { // add active buffs too!
                        var playerEntity = entityActiveBuffsByBuffIdPair.Key as EntityPlayer;
                        if (playerEntity != null)
                        {
                            activeAndInactiveBuffsByPlayerUid[playerEntity.PlayerUID] = entityActiveBuffsByBuffIdPair.Value.Select(pair => serializeBuff(pair.Value)).ToList();
                        }
                    }
                    sapi.WorldManager.SaveGame.StoreData($"{mod.Mod.Info.ModID}:BuffStuff", SerializerUtil.Serialize<Dictionary<string, List<SerializedBuff>>>(activeAndInactiveBuffsByPlayerUid));
                };

                //Expire entity effects on server shutdown
                sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, () => {
                    foreach (var entity in activeBuffsByEntityAndBuffId.Keys.ToArray())
                    {
                        var activeBuffsByBuffId = activeBuffsByEntityAndBuffId[entity];
                        foreach (var buff in activeBuffsByBuffId.Values.ToArray())
                        {
                            buff.OnExpire();
                            RemoveBuff(entity, buff);
                            continue;
                        }
                    }
                });

                sapi.Event.PlayerNowPlaying += (serverPlayer) => {
                    var playerUid = (serverPlayer.Entity as EntityPlayer).PlayerUID;
                    if (inactiveBuffsByPlayerUid.TryGetValue(playerUid, out var inactiveBuffs))
                    {
                        var now = Now;
                        activeBuffsByEntityAndBuffId[serverPlayer.Entity] = inactiveBuffs.Select(serializedBuff => deserializeBuff(serializedBuff, serverPlayer.Entity)).Where((buff) => buff != null).ToDictionary(buff => buffTypeToIds[buff.GetType()], buff => buff);
                        inactiveBuffsByPlayerUid.Remove(playerUid);
                        foreach (var buffIdAndBuffPair in activeBuffsByEntityAndBuffId[serverPlayer.Entity])
                        {
                            buffIdAndBuffPair.Value.OnJoin();
                        }
                    }
                };
                sapi.Event.PlayerLeave += (serverPlayer) => {
                    var playerUid = (serverPlayer.Entity as EntityPlayer).PlayerUID;
                    if (activeBuffsByEntityAndBuffId.TryGetValue(serverPlayer.Entity, out var activeBuffsByBuffId))
                    {
                        foreach (var activeBuffsByBuffIdPair in activeBuffsByBuffId)
                        {
                            activeBuffsByBuffIdPair.Value.OnLeave();
                        }
                        inactiveBuffsByPlayerUid[playerUid] = activeBuffsByBuffId.Select(pair => serializeBuff(pair.Value)).ToList();
                        // activeBuffs.ToDictionary(pair => pair.Key, pair => pair.Value - now); // convert remaining time to future timestamp
                        activeBuffsByEntityAndBuffId.Remove(serverPlayer.Entity);
                    }
                };
                sapi.Event.OnEntityDespawn += (Entity entity, EntityDespawnReason reason) => {
                    activeBuffsByEntityAndBuffId.Remove(entity);
                };
                sapi.Event.PlayerDeath += (serverPlayer, damageSource) => {
                    if (activeBuffsByEntityAndBuffId.TryGetValue(serverPlayer.Entity, out var activeBuffsByBuffId))
                    {
                        foreach (var activeBuffsByBuffIdPair in activeBuffsByBuffId)
                        {
                            activeBuffsByBuffIdPair.Value.OnDeath();
                        }
                    }
                    activeBuffsByEntityAndBuffId.Remove(serverPlayer.Entity);
                };
                sapi.World.RegisterGameTickListener((float dt) => {
                    var now = Now;
                    foreach (var entity in activeBuffsByEntityAndBuffId.Keys.ToArray())
                    {
                        var activeBuffsByBuffId = activeBuffsByEntityAndBuffId[entity];
                        foreach (var buff in activeBuffsByBuffId.Values.ToArray())
                        {
                            if (buff.ExpireTimestampInDays < now || buff.TickCounter >= buff.ExpireTick)
                            {
                                buff.OnExpire();
                                RemoveBuff(entity, buff);
                                continue;
                            }
                            buff.TickCounter += 1;
                            buff.OnTick();
                            if (buff.ExpireTimestampInDays < now || buff.TickCounter >= buff.ExpireTick)
                            {
                                buff.OnExpire();
                                RemoveBuff(entity, buff);
                            }
                        }
                    }
                }, TICK_MILLISECONDS);
            }
            isInitialized = true;
        }
        public static Buff GetActiveBuff(Entity entity, string buffId)
        {
            if (activeBuffsByEntityAndBuffId.TryGetValue(entity, out var activeBuffs))
            {
                if (activeBuffs.TryGetValue(buffId, out var buff))
                {
                    return buff;
                }
            }
            return null;
        }
        public static bool IsBuffActive(Entity entity, string buffId)
        {
            return GetActiveBuff(entity, buffId) != null;
        }
        /// <summary>
        /// This is intended to be called by `Buff.Apply`, you probably shouldn't call it directly.
        /// </summary>
        internal static void ApplyBuff(Entity entity, Buff buff)
        {
            if (!isInitialized) { throw new System.Exception("BuffManager.RegisterBuff: must call BuffManager.Initialize() first"); }
            if (!buffTypeToIds.ContainsKey(buff.GetType())) { throw new System.Exception($"BuffManager.RegisterBuff: must call BuffManager.RegisterBuffType() first for buff type {buff.GetType().FullName}"); }
            buff.entity = entity; // set entity! this is otherwise unsettable, because it's internal
            Dictionary<string, Buff> activeBuffs;
            if (!activeBuffsByEntityAndBuffId.TryGetValue(entity, out activeBuffs))
            { // if entity doesn't have a dict pair, create one
                activeBuffs = new Dictionary<string, Buff>();
                activeBuffsByEntityAndBuffId[entity] = activeBuffs;
            }
            var buffId = buffTypeToIds[buff.GetType()];
            if (activeBuffsByEntityAndBuffId[entity].TryGetValue(buffId, out var oldBuff))
            {
                buff.OnStack(oldBuff);
            }
            else
            {
                buff.OnStart();
            }
            activeBuffs[buffId] = buff;
        }
        /// <summary>
        /// This is intended to be called by `Buff.Remove`, you probably shouldn't call it directly.
        /// </summary>
        internal static bool RemoveBuff(Entity entity, Buff buff)
        { // n.b. does not call any Buff event callbacks!
            if (activeBuffsByEntityAndBuffId.TryGetValue(entity, out var activeBuffs))
            {
                var buffId = buffTypeToIds[buff.GetType()];
                if (activeBuffs.Remove(buffId))
                {
                    if (activeBuffs.Count == 0)
                    {
                        activeBuffsByEntityAndBuffId.Remove(entity); // cleanup dictionary pair with now-empty list!
                    }
                    return true;
                }
            }
            return false;
        }
    }

    public abstract class Buff
    {
        // public abstract string ID { get; }
        public int TickCounter = 0;
        public double ExpireTimestampInDays = 0;
        public int ExpireTick = 0;
        internal Entity entity;
        public Entity Entity { get { return entity; } }
        /// <summary>Called when a buff is first applied. If a buff is applied while the buff is still active, OnStart is not called: OnStack is called instead.</summary>
        public virtual void OnStart() { }
        /// <summary>Called when a buff is applied while a buff with an identical ID is still active. OnStack is called on the second buff, which will replace the first buff.</summary>
        public virtual void OnStack(Buff oldBuff) { }
        /// <summary>Called when a buff expires due to time passing or SetExpiryImmediately() having been called. Note that changing the expiry of the buff will have no effect: it has already expired.</summary>
        public virtual void OnExpire() { }
        /// <summary>Called when the Entity dies.</summary>
        public virtual void OnDeath() { }
        /// <summary>Called every 250 ms (4 times per second) while the buff is active.</summary>
        public virtual void OnTick() { }
        /// <summary>Called when the player with this active buff leaves the server.</summary>
        public virtual void OnLeave() { }
        /// <summary>Called when the player with this active buff re-joins the server.</summary>
        public virtual void OnJoin() { }
        /// <summary>Sets expiry of this buff to the provided number of in-game days from now. Disables real-time (tick) based expiry.</summary>
        protected void SetExpiryInGameDays(double deltaDays)
        {
            ExpireTimestampInDays = BuffManager.Now + deltaDays;
            ExpireTick = Int32.MaxValue;
        }
        /// <summary>Sets expiry of this buff to the provided number of in-game hours from now. Disables real-time (tick) based expiry.</summary>
        protected void SetExpiryInGameHours(double deltaHours)
        {
            ExpireTimestampInDays = BuffManager.Now + deltaHours / 24.0;
            ExpireTick = Int32.MaxValue;
        }
        /// <summary>Sets expiry of this buff to the provided number of in-game minutes from now. Disables real-time (tick) based expiry.</summary>
        protected void SetExpiryInGameMinutes(double deltaMinutes)
        {
            ExpireTimestampInDays = BuffManager.Now + deltaMinutes / 24.0 / 60.0;
            ExpireTick = Int32.MaxValue;
        }
        /// <summary>Sets expiry of this buff to the provided number of buff ticks from now. If you supply `0`, the buff will be removed immediately after its buff tick. Disables in-game (calendar) based expiry.</summary>
        protected void SetExpiryInTicks(int deltaTicks)
        {
            ExpireTick = TickCounter + deltaTicks;
            ExpireTimestampInDays = double.PositiveInfinity;
        }
        /// <summary>Sets expiry of this buff to the provided number of seconds from now, using buff ticks to count time. Disables in-game (calendar) based expiry.</summary>
        protected void SetExpiryInRealSeconds(int deltaSeconds)
        {
            SetExpiryInTicks((int)Math.Ceiling(deltaSeconds * 1000.0 / BuffManager.TICK_MILLISECONDS));
        }
        /// <summary>Sets expiry of this buff to the provided number of minutes from now, using buff ticks to count time. Disables in-game (calendar) based expiry.</summary>
        protected void SetExpiryInRealMinutes(int deltaMinutes)
        {
            SetExpiryInRealSeconds(deltaMinutes * 60);
        }
        /// <summary>Disables auto-expiry of this buff.</summary>
        protected void SetExpiryNever()
        {
            ExpireTimestampInDays = double.PositiveInfinity;
            ExpireTick = Int32.MaxValue;
        }
        /// <summary>Forces the buff to expire immediately after OnTick has been called. If this is called from `OnTick`, the buff will expire when `OnTick` returns.</summary>
        protected void SetExpiryImmediately()
        {
            ExpireTimestampInDays = 0;
        }
        /// <summary>Apply this buff to an entity, which starts everything in motion. Either `OnStart` or `OnStack` will be called, depending on whether the entity already has an active buff with the same buff ID.</summary>
        public void Apply(Entity entity)
        {
            if (this.entity != null) { throw new Exception("BuffStuff.Buff: this buff has already been applied to an entity, use a new buff instead of re-applying an existing buff"); }
            BuffManager.ApplyBuff(entity, this);
        }
        /// <summary>Forcibly remove this buff from its entity, without calling `OnExpire` or any other methods. You may want to use the auto-expiry system instead of calling this method.</summary>
        public void Remove()
        {
            BuffManager.RemoveBuff(entity, this);
        }
    }
}
