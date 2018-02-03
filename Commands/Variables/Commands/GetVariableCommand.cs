using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace Variables.Commands
{
    [RequiresGuild]
    class GetVariableCommand : ModernDiscordCommand
    {
        public override string Name => "Get Variable";

        public override string Description => "Gets a variable's value, if it exists.";

        public override string[] Aliases => new[] { "get" };

        public Task<CommandResult> RunCommand(string key)
        {
            if (Variables.GetVariable(Context.Guild.Id + key, out object obj))
            {
                return Task.FromResult<CommandResult>($"{key} = \"{obj}\"");
            }
            else
            {
                return Task.FromResult<CommandResult>("That variable doesn't exist!");
            }
        }
    }
}
