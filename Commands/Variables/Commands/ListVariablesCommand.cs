using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace Variables.Commands
{
    [RequiresGuild]
    class ListVariablesCommand : ModernDiscordCommand
    {
        public override string Name => "List Variables";

        public override string Description => "Lists all available variables";

        public override string[] Aliases => new[] { "list", "vars" };

        public Task<CommandResult> RunCommand()
        {
            DiscordEmbedBuilder builder = Context.GetEmbedBuilder("Variables");
            foreach (var variable in Variables.GetVariables().Where(v => v.Key.StartsWith(Context.Guild.Id.ToString())))
            {
                builder.AddField(variable.Key.Length > 256 ? variable.Key.Substring(Context.Guild.Id.ToString().Length).Substring(0, 252) + "..." : variable.Key.Substring(Context.Guild.Id.ToString().Length), variable.Value.ToString(), true);
            }

            return Task.FromResult<CommandResult>(builder.Build());
        }
    }
}
