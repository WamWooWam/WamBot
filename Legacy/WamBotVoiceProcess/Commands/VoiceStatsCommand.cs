using DSharpPlus.Entities;
using WamBotVoiceProcess.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamWooWam.Core;

namespace MusicCommands
{
    class VoiceStatsCommand : MusicCommand
    {
        public override string Name => "Voice Stats";

        public override string Description => "Gives voice statistics and diagnostic data";

        public override string[] Aliases => new[] { "voicestats" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            DiscordEmbedBuilder builder = context.GetEmbedBuilder("Voice Stats");
            Process process = Process.GetCurrentProcess();
            AssemblyName mainAssembly = Assembly.GetExecutingAssembly().GetName();

            builder.AddField("Region", connection.Connection.Channel.Guild.VoiceRegion.Id, true);
            builder.AddField("Ping", $"{connection.Connection.Ping}ms", true);
            builder.AddField("Connection Duration", (DateTime.Now - connection.ConnectTime).ToString(), true);
            builder.AddField("Bitrate", connection.Connection.Channel.Bitrate.ToString(), true);
            
            builder.AddField("RAM Usage (Current)", Files.SizeSuffix(process.PrivateMemorySize64), true);
            builder.AddField("RAM Usage (Peak)", Files.SizeSuffix(process.PeakWorkingSet64), true);

            builder.AddField("Ping", $"{context.Client.Ping}ms", true);
            builder.AddField("Version", $"{mainAssembly.Version}", true);
            builder.AddField("Compiled at", new DateTime(2000, 1, 1).AddDays(mainAssembly.Version.Build).AddSeconds(mainAssembly.Version.MinorRevision * 2).ToString(CultureInfo.CurrentCulture), true);


            return Task.FromResult<CommandResult>(builder.Build());
        }
    }
}
