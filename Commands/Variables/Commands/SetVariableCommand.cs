using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace Variables.Commands
{
    [RequiresGuild]
    class SetVariableCommand : ModernDiscordCommand
    {
        public override string Name => "Set Variable";

        public override string Description => "Sets a variable value.";

        public override string[] Aliases => new[] { "set", "var" };

        public Task<CommandResult> RunCommand(string key, string value)
        {
            Variables.SetVariable(Context.Guild.Id + key, value);
            return Task.FromResult<CommandResult>($"Set variable {key} to \"{value}\"");
        }
    }
}
