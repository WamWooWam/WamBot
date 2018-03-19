using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;

namespace WamBotRewrite.Commands
{
    [RequiresGuild]
    class ModerationCommands : CommandCategory
    {
        const string err = "Discord doesn't allow me to get more than 100 messages. Sorry!";

        public override string Name => "Moderation";

        public override string Description => "I can help you moderate your server with these commands!";

        [Command("Mass Delete", "Mass deletes a set number of messages.", new[] { "massdel", "deltree", "rmdir", "rm" })]
        [Permissions(BotPermissions = GuildPermission.ManageMessages, UserPermissions = GuildPermission.ManageMessages)]
        public async Task MassDelete(CommandContext ctx, [Range(0, 100, ErrorMessage = err)]int no, IUser user = null)
        {
            await ctx.Message.DeleteAsync();

            var messages = await ctx.Channel.GetMessagesAsync(100).FlattenAsync();

            if (user == null)
            {
                await (ctx.Channel as ITextChannel).DeleteMessagesAsync(messages.Take(messages.Count() > no ? no : messages.Count()));
            }
            else
            {
                await (ctx.Channel as ITextChannel).DeleteMessagesAsync(messages.Where(m => m.Author.Id == user.Id).Take(messages.Count() > no ? no : messages.Count()));
            }

            var message = await ctx.Channel.SendMessageAsync($"Deleted {(messages.Count() > no ? no : messages.Count())} messages{(user != null ? $" from {user.Username}#{user.Discriminator}" : "")}.");
            await Task.Delay(5_000);
            await message.DeleteAsync();
        }
    }
}
