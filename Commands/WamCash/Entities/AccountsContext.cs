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
    }
}
