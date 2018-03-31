using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Data
{
    public class Channel
    {
        public Channel()
        {
            MarkovEnabled = true;
        }

        public Channel(IGuildChannel channel) : this()
        {
            ChannelId = (long)channel.Id;
            GuildId = (long)channel.Guild.Id;
            Name = channel.Name;
            Type = channel is ITextChannel ? ChannelType.Text : ChannelType.Voice;
        }

        public string Name { get; set; }
        public ChannelType Type { get; set; }

        [Key]
        public long ChannelId { get; set; }
        
        public long GuildId { get; set; }
        public Guild Guild { get; set; }

        public bool MarkovEnabled { get; set; }

        public int MessagesSent { get; set; }
        public int Connections { get; set; }
    }
}
