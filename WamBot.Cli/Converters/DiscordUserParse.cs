using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.Converters
{
    class DiscordUserParse : IParamConverter
    {
        public string Name => "Discord User";

        public string Description => "Parses a Discord User";

        public Type[] AcceptedTypes => new[] { typeof(DiscordUser) };

        public async Task<object> Convert(string arg, Type to, CommandContext context)
        {
            DiscordUser user = null;
            if (ulong.TryParse(arg, out ulong id))
            {
                user = await context.Client.GetUserAsync(id);
            }
            else
            {
                user = context.Message.MentionedUsers.FirstOrDefault();
            }

            return user;
        }
    }
}
