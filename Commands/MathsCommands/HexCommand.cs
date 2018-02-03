using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MathsCommands
{
    class HexCommand : ModernDiscordCommand
    {
        public override string Name => "Hex";

        public override string Description => "Returns the hex representation of a number";

        public override string[] Aliases => new[] { "hex" };

        public CommandResult Run(long l) => ($"0x{l:X2}");
    }
}
