using ShackleGear.Controllers;
using ShackleGear.Datasource;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace ShackleGear.Commands
{
    public class SGXFree
    {

        PrisonController Prison;
        ShackleGearTracker Tracker;

        public SGXFree(PrisonController prison, ShackleGearTracker tracker)
        {
            Prison = prison;
            Tracker = tracker;
        }

        public void Handler(IServerPlayer player, int groupid, CmdArgs args)
        {
            int i = 0;
            try
            {
                string playername = args.PopWord();
                if (playername != null)
                {
                    i = 28;
                    IServerPlayer prisoner = null;
                    ItemSlot slot = player.InventoryManager?.ActiveHotbarSlot;

                    foreach (var val in player.Entity.World.AllPlayers)
                    {
                        i = 34;
                        if (val.PlayerName == playername)
                        {
                            i = 37;
                            prisoner = val as IServerPlayer;
                            break;
                        }
                    }
                    if (prisoner?.PlayerUID != null)
                    {
                        i = 47;
                        Prison.FreePlayer(prisoner.PlayerUID, Tracker.GetTrackData(prisoner.PlayerUID).Slot);
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "Player " + prisoner.PlayerName + " Freed.", EnumChatType.OwnMessage);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "Player \"" + playername + "\" does not exist.", EnumChatType.OwnMessage);
                    }
                }
                else
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Please provide a valid player name.", EnumChatType.OwnMessage);
                }
            }
            catch (Exception ex)
            {
                player.Entity.World.Logger.Debug("[ShackleGear] Exception thrown after: " + i);
                player.Entity.World.Logger.Debug("[ShackleGear] Ex: " + ex);
            }
        }
    }
}