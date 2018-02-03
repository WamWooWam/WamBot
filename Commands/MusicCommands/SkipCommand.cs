using MusicCommands.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    class SkipCommand : MusicCommand
    {
        public override string Name => "Skip";

        public override string Description => "Skips the current song in the queue";

        public override string[] Aliases => new[] { "skip", "next" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            Task<CommandResult> t = Task.FromResult<CommandResult>($"Skipped {connection.NowPlaying}");
            connection.Skip = true;

            return t;
        }
    }
}
