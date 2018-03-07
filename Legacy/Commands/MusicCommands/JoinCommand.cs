using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    [RequiresGuild]
    class JoinCommand : BaseDiscordCommand
    {
        public override string Name => "Join";

        public override string Description => "Joins WamBot to a voice channel.";

        public override string[] Aliases => new[] { "join", "connect" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x <= 1;

        public override Permissions RequiredPermissions => base.RequiredPermissions | Permissions.UseVoice;

        private static Dictionary<ulong, Process> RunningConnections { get; set; } = new Dictionary<ulong, Process>();

        public override Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            try
            {
                string evalExePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Tools", "WamBotVoiceProcess.exe");

                if (File.Exists(evalExePath))
                {
                    if (context.Author is DiscordMember memb)
                    {
                        DiscordVoiceState state = memb.VoiceState;
                        if (state != null)
                        {
                            if (!RunningConnections.TryGetValue(memb.Guild.Id, out Process connection))
                            {
                                Process evalProcess = new Process();
                                evalProcess.StartInfo.FileName = evalExePath;
                                evalProcess.StartInfo.Arguments =
                                    $"{context.Guild.Id} " +
                                    $"{state.Channel.Id} " +
                                    $"{context.Channel.Id} " +
                                    $"\"{Path.Combine(Directory.GetCurrentDirectory(), "config.json")}\"";
                                evalProcess.StartInfo.UseShellExecute = false;
                                RunningConnections[memb.Guild.Id] = evalProcess;

                                evalProcess.Start();
                                evalProcess.WaitForExit();
                                RunningConnections.Remove(memb.Guild.Id);
                            }
                            else
                            {
                                return Task.FromResult<CommandResult>("I'm already connected to voice in here! Fuck off!");
                            }
                        }
                        else
                        {
                            return Task.FromResult<CommandResult>("You'll need to connect to voice before you can do that!");
                        }
                    }
                    else
                    {
                        return Task.FromResult<CommandResult>("I can only join voice channels within guilds, sorry!");
                    }
                }
                else
                {
                    return Task.FromResult<CommandResult>("Voice is currently not available as required executables are missing. Sorry!");
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }

            return Task.FromResult(CommandResult.Empty);
        }

    }
}
