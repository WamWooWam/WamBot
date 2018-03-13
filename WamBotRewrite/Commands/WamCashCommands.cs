using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;

namespace WamBotRewrite.Commands
{
    class WamCashCommands : CommandCategory
    {
        public override string Name => "WamCash";

        public override string Description => "My virtual economy, what makes my world go round";

        [RequiresGuild]
        [Command("Statement", "Requests a statement from the Bank of Wam", new[] { "statement" })]
        public async Task Statement(CommandContext ctx)
        {
            if (ctx.Author is SocketGuildUser member)
            {
                var account = ctx.UserData;
                var channel = await member.GetOrCreateDMChannelAsync();
                var builder = new EmbedBuilder()
                    .WithAuthor($"{member.Nickname ?? member.Username} - Bank of Wam", null, member.GetAvatarUrl());

                builder.AddField("Balance", account.Balance.ToString("N2"));
                decimal balance = account.Balance;

                foreach (var transaction in account.Transactions.AsEnumerable().Reverse().Take(12))
                {
                    balance -= transaction.Amount;
                    builder.AddField(transaction.Reason, $"[{transaction.TimeStamp.ToString("u")}] W${transaction.Amount:N2}", true);
                    builder.AddField("Balance", $"W${balance:N2}", true);
                }

                //context.Accounts.Update(account);
                await channel.SendMessageAsync(string.Empty, embed: builder.Build());
                await ctx.ReplyAsync("Your bank statement has been sent. Check your DMs!");
            }
            else
            {
                await ctx.ReplyAsync("Okay what the fuck??");
            }
        }
    }
}
