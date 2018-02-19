using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamBot.Core;

namespace WamBot.Cli.StockCommands
{
    class HappinessCommand : ModernDiscordCommand
    {
        public override string Name => "Happiness";

        public override string Description => "Gets info on the bot's happiness.";

        public override string[] Aliases => new[] { "happiness", "happy" };

        public CommandResult RunCommand(DiscordUser user = null)
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
            else
            {
                return UserHappiness(Context.Guild?.Members.FirstOrDefault(m => m.Id == Context.Message.Author.Id) ?? Context.Message.Author);
            }
        }

        private DiscordEmbed UserHappiness(DiscordUser user)
        {
            DiscordEmbedBuilder builder = Context.GetEmbedBuilder(user is DiscordMember m ? m.DisplayName : user.Username);
            if (((BotContext)Context.AdditionalData["botContext"]).Config.Happiness.TryGetValue(user.Id, out sbyte h))
            {
                builder.AddField("Raw Happiness", h.ToString(), true);
                builder.AddField("Evaluated Happiness", Api.Tools.GetHappinessLevel(h).ToString(), true);
            }
            else
            {
                builder.AddField("Happiness Unavailable", "This user hasn't got happiness data available. Sorry!");
            }

            return builder.Build();
        }
    }
}
