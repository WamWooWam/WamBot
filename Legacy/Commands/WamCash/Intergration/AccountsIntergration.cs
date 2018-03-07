using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamCash.Entities;

namespace WamCash.Intergration
{
    public class AccountsIntergration
    {
        public static async Task EnsureBallanceAsync(DiscordUser user, decimal cost)
        {
            using (AccountsContext context = new AccountsContext())
            {
                Account account = await context.Accounts.FindAsync(user.Id);
                if ((account.Balance - cost) > -300)
                {
                    account.Balance -= cost;
                }
                else
                {
                    throw new CommandException("You don't have enough money to run this command! Sorry!");
                }

                await context.SaveChangesAsync();
            }
        }

        public static async Task EnsureBallanceAsync(CommandContext ctx, decimal cost)
        {
            using (AccountsContext context = new AccountsContext())
            {
                Account account = await context.Accounts.FindAsync(ctx.Author.Id);
                if ((account.Balance - cost) > -300)
                {
                    try
                    {
                        var msg = await ctx.ReplyAndAwaitResponseAsync($"This command’s a little heavy, and will cost W${cost:N2} to run. Do you want to continue? (y/n)", 15_000);
                        if(msg.Content.ToLowerInvariant() == "y")
                        {
                            account.Balance -= cost;
                        }
                        else
                        {
                            throw new CommandException("Command aborted.");
                        }
                    }
                    catch
                    {
                        throw new CommandException("Command aborted.");
                    }
                }
                else
                {
                    throw new CommandException("You don't have enough money to run this command! Sorry!");
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
