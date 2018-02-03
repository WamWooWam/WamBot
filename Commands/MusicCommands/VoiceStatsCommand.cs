using DSharpPlus.Entities;
using MusicCommands.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    class VoiceStatsCommand : MusicCommand
    {
        public override string Name => "Voice Stats";

        public override string Description => "Gives voice statistics and diagnostic data";

        public override string[] Aliases => new[] { "stats" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            DiscordEmbedBuilder builder = context.GetEmbedBuilder("Voice Stats");
            builder.AddField("Region", connection.Connection.Channel.Guild.VoiceRegion.Id, true);
            builder.AddField("Ping", $"{connection.Connection.Ping}ms", true);
            builder.AddField("Connection Duration", (DateTime.Now - connection.ConnectTime).ToString(), true);
            builder.AddField("Bitrate", connection.Connection.Channel.Bitrate.ToString(), true);

            return Task.FromResult<CommandResult>(builder.Build());
        }
    }
}
