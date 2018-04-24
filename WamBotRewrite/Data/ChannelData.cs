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

    public class ChannelFactory : IFactory<Channel>
    {
        public static ChannelFactory Instance { get; private set; } = new ChannelFactory();

        public Channel Create(object key)
        {
            var channel = Program.Client.GetChannel((ulong)key);
            return new Channel((IGuildChannel)channel);
        }

        public Task<Channel> CreateAsync(object key)
        {
            return Task.FromResult(Create(key));
        }
    }
}
