using MusicCommands.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    [Owner]
    class RecordCommand : MusicCommand
    {
        public override string Name => "Record";

        public override string Description => "Starts and stops recording audio from the current voice channel.";

        public override string[] Aliases => new[] { "rec" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            return Task.FromResult<CommandResult>("NYI!");
        }
    }
}
