using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    class UserInfoCommand : DiscordCommand
    {
        public override string Name => "User Info";

        public override string Description => "Gives information about a user";

        public override string[] Aliases => new[] { "user", "userinfo", "whodis" };

        public CommandResult RunCommand(DiscordUser user = null)
        {
            if (user == null)
            {
                user = Context.Message.Author;
            }

            if (user != null)
            {
                DiscordMember memb = user as DiscordMember;

                DiscordEmbedBuilder builder = Context.GetEmbedBuilder(user.Username);
                builder.WithThumbnailUrl(user.AvatarUrl);

                builder.AddField("Username", $"{user.Username}#{user.Discriminator}", memb != null);
                if (memb != null)
                {
                    builder.AddField("Display Name", memb.DisplayName, true);
                }

                builder.AddField("Id", $"{user.Id}", true);
                builder.AddField("Mention", $"\\{user.Mention}", true);

                builder.AddField("Joined Discord", user.CreationTimestamp.UtcDateTime.ToString(CultureInfo.CurrentCulture), memb != null);
                if (memb != null)
                {
                    builder.AddField("Joined Server", memb.JoinedAt.UtcDateTime.ToString(CultureInfo.CurrentCulture), true);
                }

                int guilds = Context.Client.Guilds.AsParallel().Where(g => g.Value.Members.Any(m => m.Id == user.Id)).Count();
                builder.AddField("Guilds with WamBot", guilds.ToString());

                builder.AddField("Is bot?", user.IsBot.ToString(), true);
                builder.AddField("Is current?", user.IsCurrent.ToString(), true);

                if (memb != null)
                {
                    builder.AddField("Is muted?", memb.IsMuted.ToString(), true);
                    builder.AddField("Is deafened?", memb.IsDeafened.ToString(), true);

                    builder.AddField("Roles", memb.Roles.Any() ? string.Join(", ", memb.Roles.Select(r => r.Mention)) : "None");
                }                

                return (builder.Build());
            }
            else
            {
                return ("I can't find that user dipshit!");
            }
        }
    }
}
