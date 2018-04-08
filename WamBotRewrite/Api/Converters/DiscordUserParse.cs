using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api.Converters
{
    class DiscordUserParse : IParamConverter
    {
        public string Name => "Discord User";

        public string Description => "Parses a Discord User";

        public Type[] AcceptedTypes => new[] { typeof(IUser) };

        public async Task<object> Convert(string arg, ParameterInfo to, CommandContext context)
        {
            IUser user = null;
            if (ulong.TryParse(arg, out ulong id))
            {
                user = (context.Guild != null ? (IUser)(await context.Guild.GetUserAsync(id)) : context.Client.GetUser(id));
            }
            else if(context.Message.MentionedUserIds.Count > 0)
            {
                if (context.Message is SocketMessage m)
                {
                    user = m.MentionedUsers
                        .ElementAtOrDefault(to.Position - 1);
                }
                else
                {
                    user = context.Message.MentionedUserIds
                        .Select(u => context.Client.GetUser(u))
                        .ElementAtOrDefault(to.Position - 1);
                }
            }
            else
            {
                user = (await context.Channel.GetUsersAsync().FlattenAsync()).FirstOrDefault(u => u.Username.ToLowerInvariant().Contains(arg.ToLowerInvariant()));
            }

            return user;
        }
    }
}
