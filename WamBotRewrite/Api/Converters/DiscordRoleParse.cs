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
    class DiscordRoleParse : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(IRole) };

        public Task<object> Convert(string arg, ParameterInfo to, CommandContext context)
        {
            IRole role = null;
            if (ulong.TryParse(arg, out ulong id))
            {
                role = context.Client.Guilds
                    .SelectMany(g => g.Roles)
                    .FirstOrDefault(g => g.Id == id);
            }
            else
            {
                role = context.Message.MentionedRoleIds
                    .Select(c => context.Client.Guilds
                                .SelectMany(g => g.Roles)
                                .FirstOrDefault(g => g.Id == c))
                    .ElementAtOrDefault(to.Position - 1);
            }

            return Task.FromResult<object>(role);
        }
    }
}
