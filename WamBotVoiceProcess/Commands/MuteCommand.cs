using DSharpPlus;
using DSharpPlus.Entities;
using WamBotVoiceProcess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    class MuteCommand : MusicCommand
    {
        public override string Name => "Mute";

        public override string Description => "Mass mutes/unmutes everyone in the voice channel.";

        public override string[] Aliases => new[] { "mute" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x == 1;

        public override Permissions? UserPermissions => Permissions.ManageMessages;

        public override async Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            if (bool.TryParse(args[0], out bool mute))
            {
                var states = context.Guild.VoiceStates
                    .Where(v => v?.Channel.Id == connection.Connection.Channel.Id && v?.User.IsCurrent != true);

                if (context.Guild.CurrentMember.Roles.Any(r => r.Permissions.HasFlag(Permissions.ManageChannels)))
                {
                    try
                    {
                        await connection.Connection.Channel.AddOverwriteAsync(
                            context.Guild.EveryoneRole,
                            !mute ? Permissions.None : Permissions.UseVoiceDetection | Permissions.Speak,
                            mute ? Permissions.None : Permissions.UseVoiceDetection | Permissions.Speak,
                            "Singalong!");
                    }
                    catch { }
                }

                foreach (var s in states)
                {
                    if (s != null)
                    {
                        DiscordMember memb = await context.Guild.GetMemberAsync(s.User.Id);
                        if (memb != null)
                        {
                            try
                            {
                                await memb.SetMuteAsync(mute, "Singalong!");
                            }
                            catch (Exception ex)
                            {
                                await context.ReplyAsync($"Unable to unmute {memb.Username}. {ex.GetType().Name}");
                            }
                        }
                    }
                }

                return $"{(mute ? "Muted" : "Unmuted")} {states.Count()} members.";
            }
            else
            {
                return "Bool plz";
            }
        }
    }
}
