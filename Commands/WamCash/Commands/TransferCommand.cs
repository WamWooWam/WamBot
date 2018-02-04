using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamCash.Entities;

namespace WamCash.Commands
{
    class TransferCommand : ModernDiscordCommand
    {
        public override string Name => "Transfer";

        public override string Description => "Transfer money from your account to someone else's.";

        public override string[] Aliases => new[] { "transfer", "trans", "give", "send" };

        public async Task<CommandResult> RunCommand(DiscordUser recipient, decimal amount)
        {
            if (recipient.Id != Context.Author.Id)
            {
                if (amount >= 0.01m)
                {
                    using (AccountsContext context = new AccountsContext())
                    {
                        try
                        {
                            Account send = await context.Accounts.FindAsync(Context.Author.Id);
                            send = await Meta.EnsureAccountAsync(context, Context.Author, send);

                            Account recieve = await context.Accounts.FindAsync(recipient.Id);
                            recieve = await Meta.EnsureAccountAsync(context, recipient, recieve);

                            if ((send.Balance - amount) >= 0)
                            {
                                try
                                {
                                    RunTransaction(recipient, amount, send, recieve);
                                    return $"Sent W${amount:N2} to {recieve.Name}";
                                }
                                finally
                                {
                                    context.Accounts.Update(send);
                                    context.Accounts.Update(recieve);
                                }
                            }
                            else if ((send.Balance - amount) >= -300)
                            {
                                try
                                {
                                    DiscordMessage m = await Context.ReplyAndAwaitResponseAsync($"This transaction will {(send.Balance >= 0 ? "put you into" : "increase your")} overdraft. Are you sure you want to continue? Y/N");
                                    if (m.Content.ToLowerInvariant() == "y")
                                    {
                                        try
                                        {
                                            RunTransaction(recipient, amount, send, recieve);
                                            return $"Sent W${amount:N2} to {recieve.Name}";
                                        }
                                        finally
                                        {
                                            context.Accounts.Update(send);
                                            context.Accounts.Update(recieve);
                                        }
                                    }
                                    else
                                    {
                                        return "Transaction aborted.";
                                    }
                                }
                                catch
                                {
                                    return "Transaction aborted.";
                                }
                            }
                            else
                            {
                                return "This transaction would put you over your overdraft limit and has been canceled.";
                            }
                        }
                        finally
                        {
                            await context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    return "This transaction is under the W$1 minimum transaction threshold.";
                }
            }
            else
            {
                return "Thats not how that works, sunshine.";
            }
        }

        private void RunTransaction(DiscordUser recipient, decimal amount, Account send, Account recieve)
        {
            send.Balance -= amount;
            recieve.Balance += amount;
            send.TransactionHistory.Add(new Transaction(recieve.Id, -amount, $"Sent to {recipient.Username}"));
            recieve.TransactionHistory.Add(new Transaction(send.Id, +amount, $"Received from {Context.Author.Username}"));
        }
    }
}
