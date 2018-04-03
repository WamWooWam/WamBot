using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Data
{
    public class WamBotContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.
                Entity<Transaction>()
                .HasOne(t => t.To)
                .WithMany(u => u.TransactionsRecieved);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.From)
                .WithMany(u => u.TransactionsSent);

            modelBuilder.Entity<Channel>()
                .HasOne(t => t.Guild)
                .WithMany(g => g.Channels)
                .HasForeignKey(c => c.GuildId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseNpgsql(Program.Config?.ConnectionString ?? "Server=localhost;Database=WamBot")
                .UseLoggerFactory(new UILoggerFactory());
        }
    }
}
