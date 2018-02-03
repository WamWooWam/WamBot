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
    class GetBalanceCommand : ModernDiscordCommand
    {
        public override string Name => "Balance";

        public override string Description => "Gets your current WamCash balance.";

        public override string[] Aliases => new[] { "bal", "ballance" };

        public async Task<CommandResult> Run()
        {
            DiscordUser user = Context.Invoker;
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithAuthor($"{(user is DiscordMember m ? m.DisplayName : user.Username)} - Bank of Wam", null, user.AvatarUrl);

            using (AccountsContext context = new AccountsContext())
            {
                Account account = await context.Accounts.FindAsync(user.Id);
                account = await Meta.EnsureAccountAsync(context, user, account);

                builder.AddField("Balance", account.Balance.ToString("N2"));
            }

            return builder.Build();
        }
    }
}
