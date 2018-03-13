using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Data
{
    public class WamBotContext : DbContext
    {
        private static bool init = false;

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        //public DbSet<ChannelData> Channels { get; set; }
        //public DbSet<GuildData> Guilds { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.
                Entity<Transaction>()
                .HasOne(t => t.To)
                .WithMany(u => u.TransactionsRecieved);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.From)
                .WithMany(u => u.TransactionsSent);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!init)
            {
                Batteries.Init();
                init = true;
            }

            optionsBuilder.UseSqlite("Data Source=WamBot.db");
        }
    }
}
