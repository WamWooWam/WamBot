using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace WamBot.Commands
{
    [Description("Commands every bot should have!")]
    public class BaseCommands : BaseCommandModule
    {
        private Random _random;
        private DiscordClient _client;
        private ILogger<BaseCommands> _logger;
        private static DateTime? _startupTime = null;

        static BaseCommands()
        {
            if (_startupTime == null)
            {
                _startupTime = DateTime.Now;
            }
        }

        public BaseCommands(DiscordClient client, ILogger<BaseCommands> logger, Random random)
        {
            _random = random;
            _client = client;
            _logger = logger;
        }

        [Command("Ping")]
        [Description("Ping my bot!")]
        public async Task PingAsync(CommandContext ctx)
        {
            _logger.Log(LogLevel.Information, "Someone pinged me!");
            await ctx.RespondAsync($"Hola! My ping is currently {ctx.Client.Ping}ms!");
        }

        [Command("Dice")]
        [Aliases("roll")]
        [Description("Take a risk, roll the dice...")]
        public async Task Dice(CommandContext ctx, string str)
        {
            var d = str;
            var splitd = d.Split('d');

            if (splitd.Length == 2 && int.TryParse(splitd[0], out var count) && int.TryParse(splitd[1], out var max))
            {
                if (count < 0 || max < 0)
                {
                    await ctx.RespondAsync("I can't generate a negative number of dice you twat.");
                    return;
                }

                if (count > 4096)
                {
                    await ctx.RespondAsync("Yeah more than 4096 dice is probably not a great idea let's be honest.");
                    return;
                }

                var builder = new StringBuilder();
                builder.Append($"{ctx.User.Username} rolled {d} and got: ");

                for (var i = 0; i < count - 1; i++)
                {
                    builder.Append($"{_random.Next(1, max + 1)}, ");
                }

                builder.Append(_random.Next(1, max + 1));
                builder.Append("!");
                await ctx.RespondAsync(builder.ToString());
                return;
            }
        }

        [Command]
        [Description("Gives information about a user.")]
        [Aliases("user", "whodis")]
        public async Task UserInfoAsync(CommandContext ctx, DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.Message.Author;
            }

            var memb = user as DiscordMember;
            var builder = ctx.GetEmbedBuilder(user.Username)
                .WithThumbnailUrl(user.GetAvatarUrl(ImageFormat.Png, 256));

            builder.AddField("Username", $"{user.Username}#{user.Discriminator}", memb != null);
            if (memb != null)
            {
                builder.AddField("Display Name", memb.Nickname ?? memb.Username, true);
            }

            builder.AddField("Id", $"{user.Id}", true);
            builder.AddField("Mention", $"\\{user.Mention}", true);

            builder.AddField("Joined Discord", user.CreationTimestamp.UtcDateTime.ToString(), memb != null);
            if (memb != null)
            {
                builder.AddField("Joined Server", memb.JoinedAt.UtcDateTime.ToString(), true);
            }

            var guilds = ctx.Client.Guilds.Values.Where(g => g.Members.Values.Any(m => m.Id == user.Id)).Count();
            builder.AddField("Guilds with me", guilds.ToString());

            builder.AddField("Bot?", user.IsBot.ToString(), true);
            builder.AddField("Current?", user.IsCurrent.ToString(), true);

            if (memb != null)
            {
                builder.AddField("Muted?", memb.IsMuted.ToString(), true);
                builder.AddField("Deafened?", memb.IsDeafened.ToString(), true);
                builder.AddField("In voice?", (memb.VoiceState != null).ToString(), true);

                if (memb.VoiceState != null)
                    builder.AddField("Voice Channel", memb.VoiceState.Channel.Mention, true);

                builder.AddField("Roles", memb.Roles.Any() ? string.Join(", ", memb.Roles.Select(r => r.Mention)) : "None");
            }

            await ctx.RespondAsync(embed: builder.Build());
        }

        [Command]
        [Description("Gives information about a user.")]
        [Aliases("guild")]
        public async Task GuildInfoAsync(CommandContext ctx, DiscordGuild guild = null)
        {
            if (guild == null)
            {
                guild = ctx.Guild;
            }

            var builder = ctx.GetGuildEmbed(guild);

            await ctx.RespondAsync(embed: builder.Build());
        }

        [Command]
        [Description("Mildly uninteresting info and data about my current state.")]
        [Aliases("info", "about")]
        public async Task StatsAsync(CommandContext ctx)
        {
            var process = Process.GetCurrentProcess();
            var mainAssembly = Assembly.GetExecutingAssembly().GetName();

            var targetFramework = Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>();

            var builder = ctx.GetEmbedBuilder("Statistics")
                .WithThumbnailUrl(ctx.Client.CurrentApplication.Icon);

            builder.AddField("Ping", $"{ctx.Client.Ping}ms", false);
            builder.AddField("Operating System", RuntimeInformation.OSDescription, false);

            if (targetFramework?.FrameworkName != null)
                builder.AddField("Target Framework", targetFramework.FrameworkName, false);

            builder.AddField("RAM Usage (GC)", Files.SizeSuffix(GC.GetTotalMemory(false)), true);
            builder.AddField("RAM Usage (Process)", Files.SizeSuffix(process.WorkingSet64), true);
            builder.AddField("Version", $"{mainAssembly.Version}", true);
            //builder.AddField("Compiled at", Tools.GetAssemblyDate(mainAssembly).ToString(), true);
            if (ctx.Member != null)
                builder.AddField("Accent Colour", $"#{ctx.Member.Color.Value.ToString("X6")}", false);

            builder.AddField("Guilds", ctx.Client.Guilds.Count.ToString(), true);
            builder.AddField("Total Channels", (ctx.Client.PrivateChannels.Count + ctx.Client.Guilds.SelectMany(g => g.Value.Channels).Count()).ToString(), true);

            builder.AddField("Total Roles", ctx.Client.Guilds.SelectMany(g => g.Value.Roles).Count().ToString(), true);
            builder.AddField("Total Emotes", ctx.Client.Guilds.SelectMany(g => g.Value.Emojis).Count().ToString(), true);
            builder.AddField("Total Members", ctx.Client.Guilds.Sum(g => g.Value.MemberCount).ToString(), true);
            builder.AddField("Available Commands", ctx.CommandsNext.RegisteredCommands.Count.ToString(), true);

            builder.AddField("Uptime", (DateTime.Now - _startupTime.Value).ToNaturalString());

            await ctx.RespondAsync(embed: builder.Build());
        }
    }
}

