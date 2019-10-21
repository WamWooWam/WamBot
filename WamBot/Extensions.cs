using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace WamBot
{
    internal static class InternalExtensions
    {
        public static DiscordEmbedBuilder GetEmbedBuilder(this CommandContext ctx, string title, string footer = null)
        {
            var asm = Assembly.GetExecutingAssembly().GetName();
            var embedBuilder = new DiscordEmbedBuilder()
                .WithFooter((footer != null ? footer + " - " : "") + "WamBot " + asm.Version.ToString(3))
                .WithAuthor(title, iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .WithColor(ctx.Guild?.CurrentMember.Color ?? default);

            return embedBuilder;
        }

        public static async Task SendFailureMessageAsync(this CommandContext ctx, string description, Exception ex)
        {
            try
            {
                var builder = ctx.GetEmbedBuilder("Error")
                    .WithDescription(description)
                    .WithColor(DiscordColor.Red)
                    .WithFooter("This message will be deleted in 10 seconds")
                    .WithTimestamp(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10));

                builder.AddField("Message", $"```{ex.Message.Truncate(1016)}```");
#if DEBUG
                builder.AddField("Stack Trace", $"```{ex.StackTrace.Truncate(1016)}```");
#endif

                var message = await ctx.RespondAsync(embed: builder);
                await Task.Delay(10_000);
                await message.DeleteAsync();
            }
            catch { }
        }

        public static IEnumerable<T> FilterCommands<T>(this IEnumerable<T> commands, CommandContext ctx) where T : Command
        {
            foreach (var item in commands)
            {
                if ((item.RunChecksAsync(ctx, true).GetAwaiter().GetResult()).Count() == 0)
                {
                    yield return item;
                }
            }
        }

        public static DiscordEmbedBuilder GetGuildEmbed(this CommandContext ctx, DiscordGuild guild)
        {
            var builder = ctx.GetEmbedBuilder(guild.Name);

            if (guild.IconUrl != null)
            {
                builder.WithThumbnailUrl(guild.IconUrl);
            }

            if (guild.SplashUrl != null)
            {
                builder.WithImageUrl(guild.SplashUrl);
            }

            builder.AddField("Id", guild.Id.ToString(), true);
            builder.AddField("Name", guild.Name, true);
            builder.AddField("Created At", guild.CreationTimestamp.ToString(), true);

            builder.AddField("Owner", guild.Owner.Mention, true);
            builder.AddField("Members", $"{guild.MemberCount}: " +
                $"{guild.Members.Values.Count(m => m.Presence?.Status == UserStatus.Online)} Online, " +
                $"{guild.Members.Values.Count(m => m.Presence?.Status == UserStatus.Idle)} Idle, " +
                $"{guild.Members.Values.Count(m => m.Presence?.Status == UserStatus.DoNotDisturb)} Do Not Disturb.", true);
            
            if (ctx.Guild == guild)
            {
                var str = new StringBuilder();
                foreach (var item in guild.Roles.Values.OrderByDescending(g => g.Position))
                {
                    var mention = item.Mention;
                    if ((str.Length + mention.Length + 1) >= 1000)
                    {
                        str.Append("...");
                        break;
                    }

                    str.Append(mention);
                    str.Append(" ");
                }

                builder.AddField("Roles", str.ToString());
            }

            if (guild.Emojis.Any())
            {
                var str = new StringBuilder();
                foreach (var item in guild.Emojis.Values.OrderBy(e => e.Name))
                {
                    var mention = item.ToString();
                    if (str.Length + mention.Length >= 1000)
                    {
                        break;
                    }

                    str.Append(mention);
                }

                builder.AddField("Emojis", str.ToString());
            }

            return builder;
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// From: https://www.dotnetperls.com/levenshtein
        /// </summary>
        public static int Distance(this string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}

