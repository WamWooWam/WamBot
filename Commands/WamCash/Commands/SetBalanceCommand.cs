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
    [Owner]
    [RequiresGuild]
    class SetBalanceCommand : DiscordCommand
    {
        public override string Name => "Set Balance";

        public override string Description => "Sets a user's balance";

        public override string[] Aliases => new[] { "setbal" };

        public async Task<CommandResult> Run(ulong id, decimal bal)
        {
            List<Account> accounts = this.GetData<List<Account>>("accounts");

            using (AccountsContext context = new AccountsContext())
            {
                Account account = await context.Accounts.FindAsync(id);
                account = await Meta.EnsureAccountAsync(context, await Context.Client.GetUserAsync(id), account);

                account.TransactionHistory.Add(new Transaction(0, bal - account.Balance, "hax"));
                account.Balance = bal;

                context.Accounts.Update(account);
                await context.SaveChangesAsync();
            }

            return "Done!";
        }
    }
}
