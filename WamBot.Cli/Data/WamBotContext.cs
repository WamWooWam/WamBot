using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WamBot.Api.Data;

namespace WamBot.Data
{
    class WamBotContext : DbContext
    {
        public DbSet<UserData> Users { get; set; }
        public DbSet<ChannelData> Channels { get; set; }
        public DbSet<GuildData> Guilds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLoggerFactory(new LoggerFactory())
                .UseSqlite("Data Source=WamBot.db");
        }
    }
}
