using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using MusicCommands.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    class LeaveCommand : MusicCommand
    {
        public override string Name => "Leave";

        public override string Description => "Makes WamBot leave the current voice channel.";

        public override string[] Aliases => new[] { "leave", "disconnect" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x <= 1;

        public override bool Async => true;

        public override async Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            connection.Connected = false;
            connection.TokenSource.Cancel();

            await Task.Delay(100);

            connection.Connection.Disconnect();

            return "Disconnected!";
        }
    }
}

