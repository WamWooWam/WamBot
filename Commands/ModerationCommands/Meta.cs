using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ModerationCommands.Data;
using WamBot.Api;

namespace ModerationCommands
{
    public class Meta : ICommandsAssembly, IBotStartup
    {
        internal static List<ulong> LockedGuilds = new List<ulong>();

        public string Name => "Moderation";

        public string Description => "Provides a basic set of moderation tools to do many things";

        public Version Version => new Version(1, 0, 0, 1);

        public Task Startup(DiscordClient client)
        {
            client.MessageCreated += Client_MessageCreated;
            client.GuildMemberAdded += Client_GuildMemberAdded;
            return Task.CompletedTask;
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            GuildData d = new HackBanCommand().GetData<GuildData>(e.Guild.Id.ToString());
            if (d?.Hackbans.Any(b => b.User == e.Member.Id) == true)
            {
                await e.Member.BanAsync(reason: "Hackban by WamBot");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (LockedGuilds.Any(g => g == e.Guild?.Id) && (e.Author as DiscordMember)?.PermissionsIn(e.Channel).HasPermission(Permissions.Administrator) != true)
            {
                try
                {
                    await e.Message.DeleteAsync();
                }
                catch { }
            }
        }
    }
}
