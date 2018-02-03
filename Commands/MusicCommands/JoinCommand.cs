using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using MusicCommands.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    [RequiresGuild]
    class JoinCommand : DiscordCommand
    {
        public override string Name => "Join";

        public override string Description => "Joins WamBot to a voice channel.";

        public override string[] Aliases => new[] { "join", "connect" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x <= 1;

        public override bool Async => true;

        public override Permissions RequiredPermissions => base.RequiredPermissions | Permissions.UseVoice;

        public override async Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            if (Static.VoiceExtention == null)
            {
                Static.VoiceExtention = context.Client.UseVoiceNext(new VoiceNextConfiguration() { VoiceApplication = VoiceApplication.Music });
            }

            try
            {
                if (context.Invoker is DiscordMember memb)
                {
                    DiscordVoiceState state = memb.VoiceState;
                    if (state != null)
                    {
                        if (!Static.Connections.TryGetValue(memb.Guild.Id, out ConnectionModel connection) || !connection.Connected)
                        {
                            VoiceNextConnection vNextconnection = await Static.VoiceExtention.ConnectAsync(state.Channel);
                            Static.Connections[state.Channel.GuildId] = new ConnectionModel(vNextconnection);
                            await context.ReplyAsync($"Connected to {state.Channel.Name}!");

                            connection = Static.Connections[state.Channel.GuildId];
                            await MusicPlayLoop(context, connection);
                        }
                        else
                        {
                            return "I'm already connected to voice in here! Fuck off!";
                        }
                    }
                    else
                    {
                        return "You'll need to connect to voice before you can do that!";
                    }
                }
                else
                {
                    return "I can only join voice channels within guilds, sorry!";
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }

            return CommandResult.Empty;
        }

        private static async Task MusicPlayLoop(CommandContext context, ConnectionModel connection)
        {
            try
            {
                while (connection.Connected)
                {
                    if (connection.Songs.TryDequeue(out SongModel model))
                    {
                        await context.ReplyAsync($"Now Playing: **{model}**");

                        try
                        {
                            using (MediaFoundationReader reader = new MediaFoundationReader(model.FilePath))
                            using (MediaFoundationResampler resampler = new MediaFoundationResampler(reader, 48000))
                            {
                                VolumeSampleProvider volume = new VolumeSampleProvider(resampler.ToSampleProvider()) { Volume = 0.66f };
                                connection.SampleProvider = volume;
                                IWaveProvider waveProvider = connection.SampleProvider.ToWaveProvider16();

                                connection.Total = reader.TotalTime;
                                connection.NowPlaying = model;

                                byte[] buff = new byte[3840];
                                int br = 0;

                                await connection.Connection.SendSpeakingAsync(true);

                                while ((br = waveProvider.Read(buff, 0, buff.Length)) > 0)
                                {
                                    connection.Token.ThrowIfCancellationRequested();

                                    if (connection.Skip)
                                    {
                                        break;
                                    }

                                    if (br < buff.Length)
                                    {
                                        for (int i = br; i < buff.Length; i++)
                                        {
                                            buff[i] = 0;
                                        }
                                    }

                                    connection.Elapsed = reader.CurrentTime;

                                    await connection.Connection.SendAsync(buff, 20);
                                }

                                if (connection.Connected)
                                {
                                    await connection.Connection.SendSpeakingAsync(false);
                                }

                                connection.Skip = false;
                            }
                        }
                        catch (OperationCanceledException) { break; }
                        catch (InvalidOperationException) { break; }
                        catch (Exception ex)
                        {
                            await context.ReplyAsync($"Something went a bit wrong attempting to play that and a {ex.GetType().Name} was thrown. Sorry!\r\n{ex.Message}");
                        }
                    }

                    connection.NowPlaying = null;
                    connection.Skip = false;

                    await Task.Delay(500);
                }
            }
            catch
            {
                if (connection.Connected)
                {
                    throw;
                }
            }
        }
    }
}
