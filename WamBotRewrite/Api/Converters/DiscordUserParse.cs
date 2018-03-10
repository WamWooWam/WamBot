using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api.Converters
{
    class DiscordUserParse : IParamConverter
    {
        public string Name => "Discord User";

        public string Description => "Parses a Discord User";

        public Type[] AcceptedTypes => new[] { typeof(IUser) };

        public async Task<object> Convert(string arg, Type to, CommandContext context)
        {
            IUser user = null;
            if (ulong.TryParse(arg, out ulong id))
            {
                user = (context.Guild != null ? (IUser)(await context.Guild.GetUserAsync(id)) : context.Client.GetUser(id));
            }
            else
            {
                user = context.Message.MentionedUserIds
                    .Select(u => context.Client.GetUser(u))
                    .FirstOrDefault();
            }

            return user;
        }
    }
}
