using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;
using WamBotRewrite.Data;

namespace WamBotRewrite
{
    internal static class WamCashSupport
    {
        public static async Task EnsureBallanceAsync(this CommandContext ctx, decimal cost, string reason)
        {
            if ((ctx.UserData.Balance - cost) > -50)
            {
                try
                {
                    if ((ctx.UserData.Balance - cost) < 0)
                    {
                        var msg = await ctx.ReplyAndAwaitResponseAsync($"This command will cost W${cost:N2} to run, which will put you into your overdraft! Do you want to continue? (y/n)", timeout: 15_000);
                        if (msg.Content.ToLowerInvariant() == "y")
                        {
                            ctx.UserData.Balance -= cost;
                            ctx.DbContext.BotUser.Value.Balance += cost;
                            ctx.DbContext.Transactions.Add(new Transaction(ctx.UserData, ctx.DbContext.BotUser.Value, cost, reason));
                        }
                        else
                        {
                            throw new CommandException("Command aborted.");
                        }
                    }
                    else
                    {
                        ctx.UserData.Balance -= cost;
                        ctx.DbContext.BotUser.Value.Balance += cost;
                        ctx.DbContext.Transactions.Add(new Transaction(ctx.UserData, ctx.DbContext.BotUser.Value, cost, reason));
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
        }
    }
}
