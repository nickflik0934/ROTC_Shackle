using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using System.Linq;
using ShackleGear.Controllers;
using ShackleGear.Datasource;
using System.Collections.Generic;

namespace ShackleGear.Items
{
    public class ItemShackleGear : Item
    {
        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;
        private double fuelMult;
        private double maxSeconds;
        public PrisonController Prsn { get => api.ModLoader.GetModSystem<ModSystemShackleGear>().Prison; }
        public ShackleGearTracker Tracker { get => api.ModLoader.GetModSystem<ShackleGearTracker>(); }
        public List<TrackData> Tracked { get => Tracker.Tracked; }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            sapi = api as ICoreServerAPI;
            capi = api as ICoreClientAPI;

            fuelMult = Attributes["fuelmult"].AsDouble(1);
            maxSeconds = Attributes["maxseconds"].AsDouble(1210000);
        }

        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
#if DEBUG
            if (world is IServerWorldAccessor)
            {
                //world.Logger.Debug("[SHACKLE-GEAR] Shackle Item Modified");
            }
#endif
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);
            //ITreeAttribute attribs = entityItem.Slot?.Itemstack?.Attributes;
            //if (attribs?.GetString("pearled_uid") != null)
            //{
            //    Prsn.FreePlayer(attribs.GetString("pearled_uid"), entityItem.Slot);
            //    entityItem.Die();
            //}
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            handling = EnumHandHandling.PreventDefault;
            ITreeAttribute attribs = slot?.Itemstack?.Attributes;

            if (sapi != null && attribs != null)
            {
                if (byEntity.Controls.Sneak && attribs.GetString("pearled_uid") != null)
                {
                    Prsn.FreePlayer(attribs.GetString("pearled_uid"), slot);
                }
                else
                {
                    slot.Inventory.Any(s =>
                    {
                        if (s?.Itemstack?.Collectible?.CombustibleProps != null)
                        {
                            if (s.Itemstack.Collectible.CombustibleProps.BurnTemperature < 1000) return false;
                            double currentfuel = attribs.GetDouble("pearled_fuel");
                            if (currentfuel > maxSeconds) return true;

                            attribs.SetDouble("pearled_fuel", currentfuel + (s.Itemstack.Collectible.CombustibleProps.BurnDuration * fuelMult));
                            s.TakeOut(1);
                            s.MarkDirty();
                            slot.MarkDirty();
                            if (attribs.GetString("pearled_uid") != null) Tracker.GetTrackData(attribs.GetString("pearled_uid")).trackData.LastFuelerUID = (byEntity as EntityPlayer).PlayerUID;
                            return true;
                        }
                        return false;
                    });
                }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            UpdateFuelState(world, inSlot);

            ITreeAttribute attribs = inSlot?.Itemstack?.Attributes;
            if (attribs?.GetDouble("pearled_fuel", 0) == 0)
            {
                dsc.AppendLine("Empty!");
            }
            else
            {
                long exile = attribs.GetLong("pearled_timestamp");
                double fuel = Math.Round(attribs.GetDouble("pearled_fuel"), 3);

                DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(exile);
                TimeSpan fuelTime = TimeSpan.FromSeconds(fuel);

                IServerPlayer prisoner = Tracker.GetTrackData(attribs.GetString("pearled_uid")).Prisoner;

                string imprisonedName = attribs?.GetString("pearled_name") ?? "-";
                string imprisonedUID = attribs?.GetString("pearled_uid") ?? "-";
                string imprisonedKiller = attribs?.GetString("pearled_killer") ?? "-";

                var healthTree = prisoner.Entity.WatchedAttributes.GetTreeAttribute("health");
                var health = healthTree.GetFloat("currenthealth");
                var maxhealth = healthTree.GetFloat("maxhealth");

                string imprisonedHealth = (health / maxhealth).ToString() ?? "0";

                dsc.AppendLine("Player: " + imprisonedName).AppendLine("Health: " + imprisonedHealth + "%").AppendLine("Exiled on: " + timestamp.ToString("MM-dd-yyyy")).AppendLine("Killer: " + imprisonedKiller).AppendLine("Remaining Time: " + fuelTime.ToString(@"dd\:hh\:mm\:ss"));
            }

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public void UpdateFuelState(IWorldAccessor world, ItemSlot inSlot)
        {
            if (inSlot == null)
            {
#if DEBUG
                world.Logger.Debug("[SHACKLE-GEAR] Slot was null during update fuel state.");
#endif
                return;
            }

            if (world.Side.IsServer() && !(inSlot is ItemSlotCreative))
            {
                ITreeAttribute attribs = inSlot?.Itemstack?.Attributes;
                if (attribs?.GetString("pearled_uid") != null)
                {
                    if (attribs.GetDouble("pearled_timestamp", -1.0) != -1.0)
                    {
                        long ms = DateTime.UtcNow.Ticks;
                        long dt = ms - attribs.GetLong("pearled_timestamp");
                        double fuel = attribs.GetDouble("pearled_fuel", 0.0f);
#if DEBUG
                        world.Logger.Debug(string.Format("[SHACKLE-GEAR] Fuel Left On This Tick: {0} Units", Math.Round(fuel, 3)));
                        world.Logger.Debug(string.Format("[SHACKLE-GEAR] TimeStamp: {0}", Math.Round(attribs.GetFloat("pearled_timestamp"), 3)));
                        world.Logger.Debug(string.Format("[SHACKLE-GEAR] MS: {0}", ms));
#endif
                        if (fuel < 0f)
                        {
                            Prsn.FreePlayer(attribs.GetString("pearled_uid"), inSlot);
                            attribs.SetLong("pearled_timestamp", -1);
                        }
                        else
                        {
                            attribs.SetDouble("pearled_fuel", fuel - (double)(dt / 10000000.0));
                        }
                    }
                }
                inSlot.MarkDirty();
            }
        }
    }
}
