using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamBotVoiceProcess;

namespace WamBotVoiceProcess.Models
{
    [RequiresGuild]
    internal abstract class MusicCommand : BaseDiscordCommand
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
                        //if (Program.ConnectionModel != null)
                        //{
                            return await RunVoiceCommand(args, context, Program.ConnectionModel);
                        //}
                        //else
                        //{
                        //    await context.ReplyAsync("I'm not actually connected in here! Fuck off!");
                        //}
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

            return CommandResult.Empty;
        }

        public abstract Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection);
    }
}
