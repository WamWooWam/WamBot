using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    [Owner]
    class ReloadCommand : DiscordCommand
    {
        public override string Name => "Reload";

        public override string Description => "Reloads all commands";

        public override string[] Aliases => new[] { "reload" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override async Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            await Program.LoadPluginsAsync();
            return CommandResult.Empty;
        }
    }
}
