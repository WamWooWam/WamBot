using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    class HappinessCommand : ModernDiscordCommand
    {
        public override string Name => "Happiness";

        public override string Description => "Gets info on the bot's happiness.";

        public override string[] Aliases => new[] { "happiness", "happy" };

        public async Task<CommandResult> RunCommand(DiscordUser user = null)
        {
            if (user != null)
            {
                if (Context.Message.Author.Id == user.Id || Context.Message.Author.Id == Context.Client.CurrentApplication.Owner.Id)
                {
                    return UserHappiness(user);
                }
                else
                {
                    return "You don't have permission to check the happiness of another member. Fuck off!";
                }
            }
            else if (Context.Message.Author.Id == Context.Client.CurrentApplication.Owner.Id)
            {
                DiscordEmbedBuilder builder = Context.GetEmbedBuilder("Happiness Info");
                foreach (var kv in Program.HappinessData)
                {
                    DiscordUser u = await Context.Client.GetUserAsync(kv.Key);
                    builder.AddField(u is DiscordMember m ? m.DisplayName : u.Username, $"{kv.Value} ({Tools.GetHappinessLevel(kv.Value)})", true);
                }

                return builder.Build();
            }
            else
            {
                return UserHappiness(Context.Guild?.Members.FirstOrDefault(m => m.Id == Context.Message.Author.Id) ?? Context.Message.Author);
            }
        }

        private CommandResult UserHappiness(DiscordUser user)
        {
            DiscordEmbedBuilder builder = Context.GetEmbedBuilder(user is DiscordMember m ? m.DisplayName : user.Username);
            if (Program.HappinessData.TryGetValue(user.Id, out sbyte h))
            {
                builder.AddField("Raw Happiness", h.ToString(), true);
                builder.AddField("Evaluated Happiness", Tools.GetHappinessLevel(h).ToString(), true);
            }
            else
            {
                builder.AddField("Happiness Unavailable", "This user hasn't got happiness data available. Sorry!");
            }

            return builder.Build();
        }
    }
}
