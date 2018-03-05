using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModerationCommands.Data
{
    class GuildData
    {
        public GuildData()
        {
            Hackbans = new List<Hackban>();
            Webhook = null;
            LastPinnedMessage = 0;
        }

        public List<Hackban> Hackbans { get; set; }     
        
        public DiscordWebhook Webhook { get; set; }

        public ulong LastPinnedMessage { get; set; }

        public DateTimeOffset LastPinTimestamp { get; set; }

        public bool HasUpdatedPins { get; set; }
    }
}
