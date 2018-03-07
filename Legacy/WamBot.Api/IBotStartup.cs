using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Api
{
    public interface IBotStartup
    {
        Task Startup(DiscordClient client);
    }
}
