using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WamBotRewrite.Api;
using WamBotRewrite.Data;

namespace WamBotRewrite.Commands
{
    class WamCashCommands : CommandCategory
    {
        public override string Name => "WamCash";

        public override string Description => "My virtual economy, what makes my world go round";

        static ConcurrentDictionary<ulong, decimal> _store = new ConcurrentDictionary<ulong, decimal>();

        public WamCashCommands()
        {
            var hourlyPayments = Tools.CreateTimer(TimeSpan.FromHours(1), HourlyPayments);
            Program.Client.MessageReceived += WamCash_MessageRecieve;
        }

        [Command("Transfer", "Send money to someone maybe special!", new[] { "trans", "give", "transfer" })]
        public async Task Transfer(CommandContext ctx, decimal amount, IUser user)
        {
            if (user.Id != ctx.Author.Id)
            {
                if (amount >= 0.01m)
                {
                    var totalAmount = amount + (amount * 0.02m);
                    var send = ctx.UserData;
                    var recieve = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, UserFactory.Instance);
                    var bot = ctx.DbContext.BotUser.Value;

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

        [RequiresGuild]
        [Command("Fine", "Fines a user", new[] { "fine" })]
        [Permissions(UserPermissions = GuildPermission.ManageMessages)]
        public async Task Fine(CommandContext ctx, IUser user, decimal amount)
        {
            if (amount > 0)
            {
                var recieve = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, UserFactory.Instance);
                var bot = ctx.DbContext.BotUser.Value;

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

        [Command("Statement", "Requests a statement from the Bank of Wam", new[] { "statement" })]
        public async Task Statement(CommandContext ctx)
        {
            IDMChannel channel = null;
            if (ctx.Channel is IDMChannel d)
                channel = d;
            else if (ctx.Author is IGuildUser u)
                channel = await u.GetOrCreateDMChannelAsync();

            if (channel != null)
            {
                var account = ctx.UserData;
                var builder = new EmbedBuilder()
                    .WithAuthor($"{ctx.Author.Username} - Bank of Wam", null, ctx.Author.GetAvatarUrl());

                builder.AddField("Balance", account.Balance.ToString("N2"));
                decimal balance = account.Balance;

                foreach (var transaction in ctx.DbContext.Transactions.Where(t => t.ToUserId == account.UserId || t.FromUserId == account.UserId).AsEnumerable().Reverse().Take(12))
                {
                    balance -= transaction.Amount;
                    builder.AddField(transaction.Reason, $"[{transaction.TimeStamp.ToString("u")}] W${transaction.Amount:N2}", true);
                    builder.AddField("Balance", $"W${balance:N2}", true);
                }

                await channel.SendMessageAsync(string.Empty, embed: builder.Build());
                if (ctx.Author is IGuildUser u)
                    await ctx.ReplyAsync("Your bank statement has been sent. Check your DMs!");
            }
            else
            {
                await ctx.ReplyAsync("Sorry! I can't find a DM channel for you!");
            }
        }

        [OwnerOnly]
        [Command("Account Details", "Gives details about a specific user's account.", new[] { "ac-details" })]
        public async Task AccountDetails(CommandContext ctx, IUser user)
        {
            var d = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, UserFactory.Instance);

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
            var bot = ctx.DbContext.BotUser.Value;
            var d = await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, UserFactory.Instance);

            Transaction t = new Transaction(bot, d, bal - d.Balance, "Hax");
            d.TransactionsRecieved.Add(t);
            bot.TransactionsSent.Add(t);

            d.Balance = bal;

            await ctx.DbContext.SaveChangesAsync();
        }

        private static Task WamCash_MessageRecieve(SocketMessage arg)
        {
            if (!arg.Author.IsCurrent() && !arg.Author.IsBot)
            {
                decimal add = 0.10m;
                if (arg.Attachments.Any())
                {
                    add += 0.05m;
                }

                if (arg.Embeds.Any())
                {
                    add += 0.05m;
                }

                if (_store.ContainsKey(arg.Author.Id))
                {
                    _store[arg.Author.Id] += add;
                }
                else
                {
                    _store.TryAdd(arg.Author.Id, add);
                }
            }

            return Task.CompletedTask;
        }

        private static void HourlyPayments(object sender, ElapsedEventArgs e)
        {
            using (WamBotContext ctx = new WamBotContext())
            {
                User bot = ctx.BotUser.Value;

                foreach (var p in _store)
                {
                    if (p.Value > 0)
                    {
                        var u = Program.Client.GetUser(p.Key);

                        if (u != null)
                        {
                            var d = ctx.Users.Find((long)p.Key);
                            if (d == null)
                            {
                                d = new User(u);
                                ctx.Users.Add(d);
                                ctx.SaveChanges();
                            }

                            ctx.Attach(d);

                            bot.Balance -= p.Value;
                            d.Balance += p.Value;

                            Transaction t = new Transaction(bot, d, p.Value, "Hourly Payment");
                            ctx.Transactions.Add(t);
                        }
                    }
                }

                _store.Clear();
                ctx.SaveChanges();
            }
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
    }
}
