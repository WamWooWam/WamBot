using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace CryptoCommands
{
    class GuidCommand : ModernDiscordCommand
    {
        public override string Name => "GUID";

        public override string Description => "Generates a new GUID";

        public override string[] Aliases => new[] { "guid" };

        public CommandResult Run()
        {
            return (Guid.NewGuid().ToString());
        }
    }
}
