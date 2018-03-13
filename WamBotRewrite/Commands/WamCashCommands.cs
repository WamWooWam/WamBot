﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;
using WamBotRewrite.Data;

namespace WamBotRewrite.Commands
{
    class WamCashCommands : CommandCategory
    {
        public override string Name => "WamCash";

        public override string Description => "My virtual economy, what makes my world go round";

        [Command("Transfer", "Send money to someone maybe special!", new[] { "trans", "give", "transfer" })]
        public async Task Transfer(CommandContext ctx, IUser user, decimal amount)
        {
            if (user.Id != ctx.Author.Id)
            {
                if (amount >= 0.01m)
                {
                    var totalAmount = amount + (amount * 0.02m);
                    var send = ctx.UserData;
                    var recieve = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, () => new User(user));
                    var bot = await GetBotUserAsync(ctx);

                    if ((send.Balance - totalAmount) >= 0)
                    {
                        await RunTransaction(ctx, user, amount, send, recieve, bot);
                    }
                    else if ((send.Balance - totalAmount) >= -300)
                    {
                        try
                        {
                            var m = await ctx.ReplyAndAwaitResponseAsync($"This transaction will {(send.Balance >= 0 ? "put you into" : "increase your")} overdraft. Are you sure you want to continue? Y/N");
                            if (m.Content.ToLowerInvariant() == "y")
                            {
                                await RunTransaction(ctx, user, amount, send, recieve, bot);
                            }
                            else
                            {
                                await ctx.ReplyAsync("Transaction aborted.");
                            }
                        }
                        catch
                        {
                            await ctx.ReplyAsync("Transaction aborted.");
                        }
                    }
                    else
                    {
                        await ctx.ReplyAsync("This transaction would put you over your overdraft limit and has been canceled.");
                    }
                }
                else
                {
                    await ctx.ReplyAsync("This transaction is under the W$0.01 minimum transaction threshold and has been canceled.");
                }
            }
            else
            {
                await ctx.ReplyAsync("Thats not how that works, sunshine.");
            }
        }

        [Command("Fine", "Fines a user", new[] { "fine" })]
        [Permissions(UserPermissions = GuildPermission.ManageMessages)]
        public async Task Fine(CommandContext ctx, IUser user, decimal amount)
        {
            if (amount > 0)
            {
                var recieve = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, () => new User(user));
                var bot = await GetBotUserAsync(ctx);

                recieve.Balance -= amount;
                bot.Balance += amount;

                await ctx.DbContext.Transactions.AddAsync(new Transaction(bot, recieve, -amount, $"Fine by {ctx.Author.Id}"));
                ctx.DbContext.Users.Update(recieve);

                await ctx.ReplyAsync($"Fined {user.Username} W${amount:N2}");
            }
            else
            {
                await ctx.ReplyAsync("Last time I looked, that's not how fines work.");
            }
        }

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

                foreach (var transaction in ctx.DbContext.Transactions.Where(t => t.ToUserId == account.UserId || t.FromUserId == account.UserId).AsEnumerable().Reverse().Take(12))
                {
                    balance -= transaction.Amount;
                    builder.AddField(transaction.Reason, $"[{transaction.TimeStamp.ToString("u")}] W${transaction.Amount:N2}", true);
                    builder.AddField("Balance", $"W${balance:N2}", true);
                }

                await channel.SendMessageAsync(string.Empty, embed: builder.Build());
                await ctx.ReplyAsync("Your bank statement has been sent. Check your DMs!");
            }
            else
            {
                await ctx.ReplyAsync("Okay what the fuck??");
            }
        }

        [OwnerOnly]
        [Command("Account Details", "Gives details about a specific user's account.", new[] { "ac-details" })]
        public async Task AccountDetails(CommandContext ctx, IUser user)
        {
            var d = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, () => new User(user));

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("```");
            builder.AppendLine($" --- Account Data for {user.Username} --- ");
            builder.AppendLine($"Balance: W${d.Balance}");
            builder.AppendLine(" -- Transactions -- ");

            decimal balance = d.Balance;
            foreach (var t in ctx.DbContext.Transactions.Where(t => t.ToUserId == d.UserId || t.FromUserId == d.UserId).AsEnumerable().Reverse())
            {
                IUser f = ctx.Client.GetUser((ulong)t.FromUserId);
                builder.AppendLine($"{f.Username}#{f.Discriminator} - {t.TimeStamp} - {t.Reason} - W${balance} - W${balance -= t.Amount}");
            }

            builder.AppendLine("```");
            await ctx.ReplyAsync(builder.ToString());
        }

        [OwnerOnly]
        [Command("Set Balance", "Sets a user's current balance.", new[] { "setbal" })]
        public async Task SetBalance(CommandContext ctx, IUser user, decimal bal)
        {
            var bot = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)ctx.Client.CurrentUser.Id, () => new User(ctx.Client.CurrentUser));
            var d = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, () => new User(user));

            Transaction t = new Transaction(bot, d, bal - d.Balance, "Hax");
            d.TransactionsRecieved.Add(t);
            bot.TransactionsSent.Add(t);

            d.Balance = bal;

            await ctx.DbContext.SaveChangesAsync();
        }

        private static async Task RunTransaction(CommandContext ctx, IUser user, decimal amount, User send, User recieve, User bot)
        {
            try
            {
                send.Balance -= amount + (amount * 0.02m);
                bot.Balance += amount * 0.02m;
                recieve.Balance += amount;

                string reason = $"Transfer from {ctx.Author.Username} to {user.Username} (W${amount * 0.02m:N2} fee)";

                await ctx.DbContext.Transactions.AddAsync(new Transaction(send, recieve, amount, reason));
                await ctx.DbContext.Transactions.AddAsync(new Transaction(send, bot, amount * 0.02m, reason));
                await ctx.ReplyAsync($"Sent W${amount:N2} to {user.Username}");
            }
            finally
            {
                ctx.DbContext.Users.Update(send);
                ctx.DbContext.Users.Update(recieve);
            }
        }

        private static async Task<User> GetBotUserAsync(CommandContext ctx)
        {
            return await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)ctx.Client.CurrentUser.Id, () => new User(ctx.Client.CurrentUser) { Balance = uint.MaxValue });
        }
    }
}