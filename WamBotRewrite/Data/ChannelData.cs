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
        }

        [Key]
        public long ChannelId { get; set; }
        
        public long GuildId { get; set; }
        public Guild Guild { get; set; }

        public bool MarkovEnabled { get; set; }
    }
}
