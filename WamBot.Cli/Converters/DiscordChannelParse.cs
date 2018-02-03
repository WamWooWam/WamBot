using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.Converters
{
    class DiscordChannelParse : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(DiscordChannel) };

        public async Task<object> Convert(string arg, Type to, CommandContext context)
        {
            DiscordChannel channel = null;
            if (ulong.TryParse(arg, out ulong id))
            {
                channel = await context.Client.GetChannelAsync(id);
            }
            else
            {
                channel = context.Message.MentionedChannels.FirstOrDefault();
            }

            return channel;
        }
    }
}
