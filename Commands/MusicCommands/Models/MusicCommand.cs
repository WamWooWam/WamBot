using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands.Models
{
    [RequiresGuild]
    internal abstract class MusicCommand : DiscordCommand
    {
        public override Permissions RequiredPermissions => base.RequiredPermissions | Permissions.UseVoice;

        public override async Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            try
            {
                if (context.Author is DiscordMember memb)
                {
                    DiscordVoiceState state = memb?.VoiceState;
                    if (state != null)
                    {
                        if (Static.Connections.TryGetValue(state.Channel.GuildId, out var connection))
                        {
                            return await RunVoiceCommand(args, context, connection);
                        }
                        else
                        {
                            await context.ReplyAsync("I'm not actually connected in here! Fuck off!");
                        }
                    }
                    else
                    {
                        await context.ReplyAsync("You'll need to connect to voice before you can do that!");
                    }
                }
                else
                {
                    await context.ReplyAsync("I can only leave voice channels within guilds, sorry!");
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }
            catch (NullReferenceException)
            {
                await context.ReplyAsync("You'll need to connect to voice before you can do that!");
            }

            return CommandResult.Empty;
        }

        public abstract Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection);
    }
}
