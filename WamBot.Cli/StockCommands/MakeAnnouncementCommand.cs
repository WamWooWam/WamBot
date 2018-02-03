using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    [Owner]
    class MakeAnnouncementCommand : ModernDiscordCommand
    {
        public override string Name => "Make Announcement";

        public override string Description => "Announces a message to all specified announcement channels.";

        public override string[] Aliases => new[] { "makeann" };

        public async Task<CommandResult> Run(params string[] lines)
        {
            string announcement = string.Join("\r\n", lines);
            await Context.ReplyAsync(announcement);
            foreach (ulong id in Program.Config.AnnouncementChnanels.Values)
            {
                try
                {
                    DiscordChannel channel = await Context.Client.GetChannelAsync(id);
                    await channel?.SendMessageAsync(announcement);
                }
                catch (Exception ex)
                {
                    await Context.ReplyAsync($"Failed to send announcement to {id}. {ex}");
                }
            }
            return "Announcement sent!";
        }
    }
}

