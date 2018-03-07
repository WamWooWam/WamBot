using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

using WamWooWam.Core;

namespace BaseCommands
{
    class UptimeCommand : DiscordCommand
    {
        private static DateTime? _startupTime = null;

        public UptimeCommand()
        {
            if (_startupTime == null)
            {
                _startupTime = DateTime.Now;
            }
        }

        public override string Name => "Uptime";

        public override string Description => "Gets the bot's current uptime.";

        public override string[] Aliases => new[] { "up", "uptime" };

        public CommandResult RunCommand() => ($"WamBot has been running for {(DateTime.Now - _startupTime.Value).ToNaturalString()}");
    }
}
