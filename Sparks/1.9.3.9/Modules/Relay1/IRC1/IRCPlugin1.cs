﻿/*
    Copyright 2015 GoldenSparks
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using GoldenSparks.Commands;
using GoldenSparks.Events.ServerEvents;

namespace GoldenSparks.Modules.Relay1.IRC1 
{   
    public sealed class IRCPlugin1 : Plugin 
    {
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string GoldenSparks_Version { get { return Server.Version; } }
        public override string name { get { return "IRCRelay1"; } }

        public static IRCBot1 Bot = new IRCBot1();
        
        public override void Load(bool startup) {
            Bot.ReloadConfig();
            Bot.Connect();
            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);
        }
        
        public override void Unload(bool shutdown) {
            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);
            Bot.Disconnect("Disconnecting IRC1 bot");
        }
        
        void OnConfigUpdated() { Bot.ReloadConfig(); }
    }
    
    public sealed class CmdIRCBot : RelayBotCmd1 
    {
        public override string name { get { return "IRCBot1"; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("ResetBot1", "reset1"), new CommandAlias("ResetIRC1", "reset1") }; }
        }
        public override RelayBot1 Bot { get { return IRCPlugin1.Bot; } }
    }
    
    public sealed class CmdIrcControllers : BotControllersCmd1 
    {
        public override string name { get { return "IRCControllers1"; } }
        public override string shortcut { get { return "IRCCtrl1"; } }
        public override RelayBot1 Bot { get { return IRCPlugin1.Bot; } }
    }
}
