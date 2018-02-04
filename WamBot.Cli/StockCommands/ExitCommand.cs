using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{ 
    [Owner]
    class ExitCommand : DiscordCommand
    {
        public override string Name => "Exit";

        public override string Description => "Gracefully exits WamBot";

        public override string[] Aliases => new[] { "exit", "close", "kys" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;
        
        public override async Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            try
            {
                DiscordMessage message = await context.ReplyAndAwaitResponseAsync("Are you sure you want to exit WamBot? Y/N");
                if(message.Content.ToLowerInvariant() == "y")
                {
                    await context.ReplyAsync("Turrah!");
                    await context.Client.DisconnectAsync();
                    Environment.Exit(0);
                }
            }
            catch { }

            return "Exit aborted.";
        }
    }
}
