using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    class PingCommand : ModernDiscordCommand
    {
        public override string Name => "Ping";

        public override string Description => "Retrieves the current ping";

        public override string[] Aliases => new[] { "ping", "speedy" };

        public CommandResult RunCommand()
        {
            Context.Happiness += 2;
            return ($"Hola! My ping's currently {Context.Client.Ping}ms!");
        }
    }
}
