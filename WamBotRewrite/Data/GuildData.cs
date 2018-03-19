using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Data
{
    public class Guild
    {
        public Guild()
        {
            Channels = new List<Channel>();
        }

        public Guild(IGuild guild) : this()
        {
            GuildId = (long)guild.Id;
        }

        [Key]
        public long GuildId { get; set; }

        public long? AnnouncementChannelId { get; set; }

        public List<Channel> Channels { get; set; }
    }
}
