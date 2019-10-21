using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Commands
{
    [Group("bot")]
    [RequireOwner]
    [Description("Bot management commands.")]
    class BotCommands : BaseCommandModule
    {
        [Group("guild")]
        class GuildCommands : BaseCommandModule
        {
            [Command("List")]
            [Description("Lists the guilds the bot is in.")]
            public async Task ListAsync(CommandContext ctx)
            {
                var interactivity = ctx.Client.GetInteractivity();
                var pages = new List<Page>();

                foreach (var item in ctx.Client.Guilds)
                {
                    var guild = item.Value;
                    var builder = ctx.GetGuildEmbed(guild);
                    var page = new Page(embed: builder);
                    pages.Add(page);
                }

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
            }

            [Command("Leave")]
            [Description("Leaves a guild the bot is in.")]
            public async Task LeaveAsync(CommandContext ctx, DiscordGuild guild)
            {
                var interactivity = ctx.Client.GetInteractivity();
                var builder = ctx.GetGuildEmbed(guild);
                builder.Description = "Are you sure you want to leave this guild?";

                var message = await ctx.RespondAsync(embed: builder.Build());
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode("❎"));

                var context = await interactivity.WaitForReactionAsync(message, ctx.User);
                if (context.Result.Emoji == "✅")
                {
                    await guild.LeaveAsync();
                }

                await message.DeleteAsync();
            }

            [Command("Info")]
            [Description("Retrieves guild information.")]
            public async Task InfoAsync(CommandContext ctx, DiscordGuild guild)
            {
                var builder = ctx.GetGuildEmbed(guild);
                await ctx.RespondAsync(embed: builder.Build());
            }
        }
    }
}
