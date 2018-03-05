using WamBotVoiceProcess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    class VolumeCommand : MusicCommand
    {
        public override string Name => "Volume Command";

        public override string Description => "Adjusts the volume of WamBot's output.";

        public override string[] Aliases => new[] { "vol" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x <= 1;

        public override Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            string vol = args.FirstOrDefault();
            if (vol != null)
            {
                if (float.TryParse(vol, out float v))
                {
                    if (v > 2)
                    {
                        v = v / 100;
                    }

                    if (v > 2 && context.Author.Id != context.Client.CurrentApplication.Owner.Id)
                    {
                        throw new CommandException("Let's not kill people's ears eh?");
                    }

                    if (v < 0.01 && context.Author.Id != context.Client.CurrentApplication.Owner.Id)
                    {
                        throw new CommandException("Little quiet don't you think?");
                    }

                    connection.Volume = v;
                    return Task.FromResult<CommandResult>($"Volume set to {v * 100}%.");
                }
                else
                {
                    return Task.FromResult<CommandResult>("floats plz");
                }
            }
            else
            {
                return Task.FromResult<CommandResult>($"Current volume is {connection.Volume * 100}%");
            }
        }
    }
}
