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
            Name = guild.Name;
        }

        public string Name { get; set; }

        [Key]
        public long GuildId { get; set; }

        public long? AnnouncementChannelId { get; set; }

        public List<Channel> Channels { get; set; }
    }

    public class GuildFactory : IFactory<Guild>
    {
        public static GuildFactory Instance { get; private set; } = new GuildFactory();

        public Guild Create(object key)
        {
            var guild = Program.Client.GetGuild((ulong)key);
            return new Guild(guild);
        }

        public Task<Guild> CreateAsync(object key)
        {
            return Task.FromResult(Create(key));
        }
    }
}
