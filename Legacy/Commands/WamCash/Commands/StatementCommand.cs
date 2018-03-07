using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamCash.Entities;

namespace WamCash.Commands
{
    [RequiresGuild]
    class StatementCommand : DiscordCommand
    {
        public override string Name => "Statement";

        public override string Description => "DMs you your latest bank statement.";

        public override string[] Aliases => new[] { "statement" };

        public async Task<CommandResult> Run()
        {
            using (AccountsContext context = new AccountsContext())
            {
                if (Context.Author is DiscordMember member)
                {
                    Account account = await context.Accounts.FindAsync(member.Id);
                    account = await Meta.EnsureAccountAsync(context, member, account);

                    DiscordDmChannel channel = await member.CreateDmChannelAsync();
                    DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                        .WithAuthor($"{member.DisplayName} - Bank of Wam", null, member.AvatarUrl);

                    builder.AddField("Balance", account.Balance.ToString("N2"));
                    decimal balance = account.Balance;

                    foreach (Transaction transaction in account.TransactionHistory.AsEnumerable().Reverse().Take(12))
                    {
                        balance -= transaction.Amount;
                        builder.AddField(transaction.Reason, $"[{transaction.Time.ToString("u")}] W${transaction.Amount:N2}", true);
                        builder.AddField("Balance", $"W${balance:N2}", true);
                    }

                    context.Accounts.Update(account);
                    await channel.SendMessageAsync(embed: builder);
                    return "Your bank statement has been sent. Check your DMs!";
                }
                else
                {
                    return "Okay what the fuck??";
                }
            }
        }
    }
}
