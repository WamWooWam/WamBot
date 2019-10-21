using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Data
{
    internal class WamBotDatabase : DbContext
    {
        public WamBotDatabase(DbContextOptions<WamBotDatabase> options) : base(options)
        {
        }
    }
}
