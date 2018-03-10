using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;

namespace WamBotRewrite.Api.Converters
{
    class DiscordChannelParse : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(IChannel) };

        public Task<object> Convert(string arg, Type to, CommandContext context)
        {
            IChannel channel = null;
            if (ulong.TryParse(arg, out ulong id))
            {
                channel = context.Client.GetChannel(id);
            }
            else
            {
                channel = context.Message.MentionedChannelIds
                    .Select(c => context.Client.GetChannel(c))
                    .FirstOrDefault();
            }

            return Task.FromResult<object>(channel);
        }
    }
}
