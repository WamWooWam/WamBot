using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api.Converters
{
    class DiscordGuildParse : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(IGuild) };

        public async Task<object> Convert(string arg, Type to, CommandContext context)
        {
            IGuild guild = null;

            if (ulong.TryParse(arg, out ulong id))
            {
                guild = (IGuild)context.Client.GetGuild(id) ?? await Program.RestClient.GetGuildAsync(id);
            }

            return guild;
        }
    }
}
