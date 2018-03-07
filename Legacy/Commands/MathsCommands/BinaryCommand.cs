using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MathsCommands
{
    class BinaryCommand : DiscordCommand
    {
        public override string Name => "Binary";

        public override string Description => "Returns the binary representation of a number";

        public override string[] Aliases => new[] { "bin", "binary" };

        public CommandResult Run(ulong l) => ($"```{Convert.ToString((long)l, 2)}```");
    }
}
