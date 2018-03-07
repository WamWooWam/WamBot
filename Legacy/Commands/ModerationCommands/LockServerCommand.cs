using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using WamBot.Api;

namespace ModerationCommands
{
    [RequiresGuild]
    class LockServerCommand : DiscordCommand
    {
        public override string Name => "Lock Server";

        public override string Description => "Locks a server and disables sending messages";

        public override string[] Aliases => new[] { "lock" };

        public override Permissions? UserPermissions => Permissions.Administrator;

        public override Permissions? BotPermissions => Permissions.ManageMessages;

        public async Task<CommandResult> Run(bool l)
        {
            if (l)
            {
                await Context.ReplyAsync("Locking server...");
                Meta.LockedGuilds.Add(Context.Guild.Id);

                return "Server locked!";
            }
            else
            {
                Meta.LockedGuilds.Remove(Context.Guild.Id);
                return "Server unlocked!";
            }
        }
    }
}
