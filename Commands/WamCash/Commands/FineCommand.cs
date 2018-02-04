using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using WamBot.Api;
using WamCash.Entities;

namespace WamCash.Commands
{
    [RequiresGuild]
    class FineCommand : ModernDiscordCommand
    {
        public override string Name => "Fine";

        public override string Description => "Fines a user an amount of money ignoring overdraft.";

        public override string[] Aliases => new[] { "fine" };

        public override Permissions? UserPermissions => RequiredPermissions | Permissions.ManageMessages;

        public async Task<CommandResult> Run(DiscordUser user, decimal amount)
        {
            if (user is DiscordMember)
            {
                using (AccountsContext context = new AccountsContext())
                {
                    if(amount > 0)
                    {

                        Account account = await context.Accounts.FindAsync(user.Id);
                        account = await Meta.EnsureAccountAsync(context, user, account);

                        account.Balance -= amount;
                        account.TransactionHistory.Add(new Transaction(Context.Author.Id, -amount, $"Fine by {Context.Author.Id}"));
                        context.Accounts.Update(account);
                        await context.SaveChangesAsync();

                        return $"Fined {user.Username} W${amount:N2}";
                    }
                    else
                    {
                        return "Last time I looked, that's not how fines work.";
                    }
                }
            }
            else
            {
                return "The fuck!?";
            }
        }
    }
}
