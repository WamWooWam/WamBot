using DSharpPlus.Entities;
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
using WamBot.Core;
using WamWooWam.Core;

namespace WamBot.Cli
{
    class StatsCommand : DiscordCommand
    {
        public override string Name => "Stats";

        public override string Description => "Mildly uninteresting info and data about the current bot state.";

        public override string[] Aliases => new[] { "stats", "info", "about" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            Process process = Process.GetCurrentProcess();
            AssemblyName mainAssembly = Assembly.GetExecutingAssembly().GetName();
            BotContext botContext = ((BotContext)context.AdditionalData["botContext"]);

            DiscordEmbedBuilder builder = context.GetEmbedBuilder("Statistics");

            builder.AddField("Operating System", RuntimeInformation.OSDescription, true);
            builder.AddField("RAM Usage (Current)", Files.SizeSuffix(process.PrivateMemorySize64), true);
            builder.AddField("RAM Usage (Peak)", Files.SizeSuffix(process.PeakWorkingSet64), true);

            builder.AddField("Ping", $"{context.Client.Ping}ms", true);
            builder.AddField("Version", $"{mainAssembly.Version}", true);
            builder.AddField("Compiled at", new DateTime(2000, 1, 1).AddDays(mainAssembly.Version.Build).AddSeconds(mainAssembly.Version.MinorRevision * 2).ToString(CultureInfo.CurrentCulture), true);

            builder.AddField("Guilds", context.Client.Guilds.Count.ToString(), true);
            builder.AddField("Total Channels", context.Client.PrivateChannels.Union(context.Client.Guilds.Values.SelectMany(g => g.Channels)).Count().ToString(), true);
            builder.AddField("Total Roles", context.Client.Guilds.Values.SelectMany(g => g.Roles).Count().ToString(), true);
            builder.AddField("Total Emotes", context.Client.Guilds.Values.SelectMany(g => g.Emojis).Count().ToString(), true);
            builder.AddField("Total Members", context.Client.Guilds.Sum(g => g.Value.MemberCount).ToString(), true);
            builder.AddField("Available Commands", botContext.Commands.Count.ToString(), true);
            builder.AddField("Available Parse Extensions", botContext.ParseExtensions.Count.ToString(), true);

            return Task.FromResult<CommandResult>(builder.Build());
        }
    }
}
