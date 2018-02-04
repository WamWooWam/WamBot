using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ModerationCommands
{
    [RequiresGuild]
    class MassDeleteCommand : ModernDiscordCommand
    {
        public override string Name => "Mass Delete";

        public override string Description => "Mass deletes a set number of messages";

        public override string[] Aliases => new[] { "massdel", "deltree", "rmdir" };

        public override Permissions RequiredPermissions => base.RequiredPermissions | Permissions.ManageMessages;

        public async Task<CommandResult> RunCommand(int no, DiscordUser user = null)
        {
            IEnumerable<DiscordMessage> messages = await Context.Channel.GetMessagesBeforeAsync(Context.Channel.LastMessageId, 100);

            if (user != null)
            {
                await Context.Channel.DeleteMessagesAsync(messages.Where(u => u.Author.Id == user.Id).Take(messages.Count() >= no ? no : messages.Count()));
            }
            else
            {
                await Context.Channel.DeleteMessagesAsync(messages.Take(no));
            }


            return CommandResult.Empty;
        }
    }
}
