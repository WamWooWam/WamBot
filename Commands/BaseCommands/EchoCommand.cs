using System;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    public class EchoCommand : ModernDiscordCommand
    {
        public override string Name => "Echo";

        public override string Description => "Simple echo command.";

        public override string[] Aliases => new string[] { "echo", "say" };

        public CommandResult Run(params string[] args) =>  (string.Join(" ", args));
    }
}
