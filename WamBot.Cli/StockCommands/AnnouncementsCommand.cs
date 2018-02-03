using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    [RequiresGuild]
    class AnnouncementsCommand : ModernDiscordCommand
    {
        public override string Name => "Announcements";

        public override string Description => "Enables or disables WamBot's announcements im this server";

        public override string[] Aliases => new[] { "announce" };

        public override Permissions? BotPermissions => Permissions.SendMessages;

        public override Permissions RequiredPermissions => base.RequiredPermissions | Permissions.ManageGuild;

        public async Task<CommandResult> Run(bool enabled, DiscordChannel channel = null)
        {
            if (enabled)
            {
                channel = channel ?? Context.Channel;
                if (channel != null)
                {
                    Program.Config.AnnouncementChnanels[Context.Guild.Id] = channel.Id;
                    await channel.SendMessageAsync("Announcements configured for this channel! WamBot will periodically announce new features and information here.");
                    return CommandResult.Empty;
                }
                else
                {
                    return "Unable to find channel.";
                }
            }
            else
            {
                Program.Config.AnnouncementChnanels.Remove(Context.Guild.Id);
                return "Announcements disabled.";
            }
        }
    }
}
