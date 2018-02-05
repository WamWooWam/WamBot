using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using WamBot.Api;
using WamCash.Commands;
using WamCash.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using WamWooWam.Core;

namespace WamCash
{
    public class Meta : ICommandsAssembly, IBotStartup
    {
        public string Name => "WamCash";

        public string Description => "CASH, WONGA, DOLLA DOLLA! Or, well, money.";

        private DiscordCommand _anyCommand = new SetBalanceCommand();
        private Dictionary<ulong, decimal> _store = new Dictionary<ulong, decimal>();
        private Timer _timer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        private DiscordClient _client;

        public async Task Startup(DiscordClient client)
        {
            _client = client;
            client.MessageCreated += Client_MessageCreated;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            _timer.AutoReset = true;
            _timer.Elapsed += (o, e) => SaveAsync();
            _timer.Start();
            await MigrateDatabase(client);
        }

        private static async Task MigrateDatabase(DiscordClient client)
        {
            var cunt = new FineCommand().GetData<List<Account>>("accounts");
            if (cunt != null)
            {
                client.DebugLogger.LogMessage(LogLevel.Info, "WamCash", "Starting WamCash migration...", DateTime.Now);
                using (AccountsContext context = new AccountsContext())
                {
                    foreach (Account account in cunt)
                    {
                        if (!context.Accounts.Any(c => c.Id == account.Id))
                        {
                            await context.Accounts.AddAsync(account);
                        }
                        else
                        {
                            Account toMerge = await context.Accounts.FindAsync(account.Id);
                            toMerge.Balance += account.Balance;
                            toMerge.TransactionHistory?.AddRange(account.TransactionHistory.Select(c =>
                            {
                                c.Id = Strings.RandomString(24);
                                return c;
                            }));

                        }
                    }

                    await context.SaveChangesAsync();
                    client.DebugLogger.LogMessage(LogLevel.Info, "WamCash", "WamCash database migrated.", DateTime.Now);
                }

                new FineCommand().SetData<object>("accounts", null);
            }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            SaveAsync();
        }

        internal static async Task<Account> EnsureAccountAsync(AccountsContext context, DiscordUser user, Account account)
        {
            if (account == null)
            {
                account = new Account(user);
                await context.Accounts.AddAsync(account);
            }

            account.TransactionHistory = account.TransactionHistory ?? new List<Transaction>();
            return account;
        }

        internal static Account EnsureAccount(AccountsContext context, DiscordUser user, Account account)
        {
            if (account == null)
            {
                account = new Account(user);
                context.Accounts.Add(account);
            }

            account.TransactionHistory = account.TransactionHistory ?? new List<Transaction>();
            return account;
        }

        private async void SaveAsync()
        {
            using (AccountsContext context = new AccountsContext())
            {
                foreach (var thing in _store.Keys)
                {
                    Account account = await context.Accounts.FindAsync(thing);
                    account = EnsureAccount(context, await _client.GetUserAsync(thing), account);

                    decimal ammount = _store[thing];
                    account.TransactionHistory.Add(new Transaction(0, ammount, "Hourly payment."));
                    account.Balance += ammount;
                }

                await context.SaveChangesAsync();
            }

            _store.Clear();
        }

        private Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Author.Id != e.Client.CurrentUser.Id && !e.Author.IsBot)
            {
                decimal add = 0.10m;
                if (e.Message.Attachments.Any())
                {
                    add += 0.05m;
                }

                if (e.Message.Embeds.Any())
                {
                    add += 0.05m;
                }

                if (_store.ContainsKey(e.Author.Id))
                {
                    _store[e.Author.Id] += add;
                }
                else
                {
                    _store.Add(e.Author.Id, add);
                }
            }
            //else if (e.Channel.IsPrivate && (e.Author.Id == 401524353528889344 || e.Author.Id == 404765717221605376))
            //{
            //    try
            //    {
            //        JObject jObject = JObject.Parse(e.Message.Content);
            //    }
            //    catch { }
            //}
            return Task.CompletedTask;
        }
    }
}
