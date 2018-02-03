using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    [Owner]
    class UnloadCommand : DiscordCommand
    {
        public override string Name => "Unload";

        public override string Description => "Unloads a command assembly.";

        public override string[] Aliases => new[] { "unload" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x == 1;
        

        public override async Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            if (File.Exists(Path.Combine("Plugins", args[0])))
            {
                File.Move(Path.Combine("Plugins", args[0]), Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(args[0]) + Path.GetFileName(Path.GetTempFileName())));
                await Program.LoadPluginsAsync();
            }

            return CommandResult.Empty;
        }
    }
}
