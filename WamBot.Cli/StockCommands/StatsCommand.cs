using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli
{
    class StatsCommand : DiscordCommand
    {
        public override string Name => "Stats";

        public override string Description => "Mildly uninteresting info and data about the current bot state.";

        public override string[] Aliases => new[] { "stats", "info", "about" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override bool Async => true;

        public override Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            DiscordEmbedBuilder builder = context.GetEmbedBuilder("Statistics");
            builder.AddField("Operating System", RuntimeInformation.OSDescription, true);
            builder.AddField("Architecture", RuntimeInformation.OSArchitecture.ToString(), true);
            builder.AddField(".NET", RuntimeEnvironment.GetSystemVersion(), true);

            builder.AddField("Ping", $"{context.Client.Ping}ms");

            builder.AddField("Guilds", context.Client.Guilds.Count.ToString(), true);

            builder.AddField("Total Channels", context.Client.PrivateChannels.Union(context.Client.Guilds.Values.SelectMany(g => g.Channels)).Count().ToString(), true);
            builder.AddField("Total Roles", context.Client.Guilds.Values.SelectMany(g => g.Roles).Count().ToString(), true);
            builder.AddField("Total Emotes", context.Client.Guilds.Values.SelectMany(g => g.Emojis).Count().ToString(), true);
            builder.AddField("Unique Members", context.Client.Guilds.Values.SelectMany(g => g.Members).Select(m => m.Id).Distinct().Count().ToString(), true);

            builder.AddField("Available Commands", Program.Commands.Count.ToString(), true);
            builder.AddField("Available Parse Extensions", Program.ParseExtensions.Count.ToString(), true);

            return Task.FromResult<CommandResult>(builder.Build());
        }
    }
}
