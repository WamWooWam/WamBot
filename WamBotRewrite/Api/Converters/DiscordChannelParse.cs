using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;

namespace WamBotRewrite.Api.Converters
{
    class DiscordChannelParse : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(IChannel) };

        public async Task<object> Convert(string arg, ParameterInfo to, CommandContext context)
        {
            IChannel channel = null;
            if (ulong.TryParse(arg, out ulong id))
            {
                channel = context.Client.GetChannel(id);
            }
            else if (context.Message.MentionedChannelIds.Any() && context.Message.MentionedChannelIds.Count() >= to.Position - 1)
            {
                channel = context.Message.MentionedChannelIds
                    .Select(c => context.Client.GetChannel(c))
                    .ElementAtOrDefault(to.Position - 1);
            }
            else if (context.Guild != null)
            {
                channel = (await context.Guild.GetChannelsAsync()).FirstOrDefault(c => c.Name.ToLowerInvariant().Contains(arg.ToLowerInvariant()));
            }

            return channel;
        }
    }
}
