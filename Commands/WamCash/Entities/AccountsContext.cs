using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WamBot.Api;
using WamCash.Commands;

namespace WamCash.Entities
{
    class AccountsContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source=Data\\wamcash.db");
        }

        public DbSet<Account> Accounts { get; set; }

        //private List<Account> _accounts;
        //private List<Account> _modifiedAccounts = new List<Account>();
        //private DiscordMember _u;
        //private DiscordCommand _c;
        //private CommandContext _context;

        //internal AccountsContext(DiscordCommand c, CommandContext context)
        //{
        //    _c = c;
        //    _accounts = c.GetData<List<Account>>(context.Guild?.Id.ToString() ?? "global");
        //    _context = context;
        //    if (_accounts == null)
        //        _accounts = new List<Account>();
        //}

        //internal AccountsContext(DiscordCommand c, DiscordUser u)
        //{
        //    _c = c ?? new FineCommand();
        //    _accounts = c.GetData<List<Account>>((u as DiscordMember)?.Guild.Id.ToString() ?? "global");
        //    _u = u as DiscordMember;
        //    if (_accounts == null)
        //        _accounts = new List<Account>();
        //}

        //public Account GetAccount(DiscordUser u)
        //{
        //    Account account = _accounts.FirstOrDefault(a => a.Id == u.Id);
        //    if (account == null)
        //        account = new Account(u);

        //    _modifiedAccounts.Add(account);

        //    return account;
        //}

        //public void Dispose()
        //{
        //    foreach (Account a in _modifiedAccounts)
        //    {
        //        _accounts.RemoveAll(na => na.Id == a.Id);
        //        _accounts.Add(a);
        //    }

        //    (_c ?? new FineCommand()).SetData((_context?.Guild?.Id.ToString() ?? _u?.Guild.Id.ToString()) ?? "global", _accounts);
        //}
    }
}
