using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using WamBotVoiceProcess.Models;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WamBot;
using WamBot.Api;
using WamBot.Core;

namespace WamBotVoiceProcess
{
    class Program
    {
        static internal DiscordGuild Guild { get; set; }
        static internal DiscordChannel VoiceChannel { get; set; }
        static internal DiscordChannel TextChannel { get; set; }
        static internal VoiceNextExtension VoiceExtension { get; set; }
        public static ConnectionModel ConnectionModel { get; set; }

        static ulong guildId;
        static ulong voiceChannelId;
        static ulong textChannelId;
        private static bool ready = false;

        static async Task Main(string[] args)
        {
            if (args.Length == 4)
            {
                guildId = ulong.Parse(args[0]);
                voiceChannelId = ulong.Parse(args[1]);
                textChannelId = ulong.Parse(args[2]);

                MediaFoundationApi.Startup();
                Config conf = JsonConvert.DeserializeObject<Config>(File.ReadAllText(args[3]));
                conf.AdditionalPluginDirectories.Clear();
                conf.AdditionalPluginDirectories.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                BotContext context = new BotContext(conf, true) { SingleGuild = guildId };
                context.LogMessage += Context_LogMessage;
                context.DSharpPlusLogMessage += Context_DSharpPlusLogMessage;
                context.Client.GuildAvailable += Client_GuildAvailable;
                await context.ConnectAsync().ConfigureAwait(false);

                while (!ready)
                {
                    await Task.Delay(1000);
                }

                VoiceExtension = context.Client.UseVoiceNext(new VoiceNextConfiguration() { VoiceApplication = VoiceApplication.Music, EnableIncoming = true });

                Guild = await context.Client.GetGuildAsync(guildId);
                Console.WriteLine(Guild);

                VoiceChannel = Guild.Channels.FirstOrDefault(c => c.Id == voiceChannelId);
                Console.WriteLine(VoiceChannel);
                TextChannel = Guild.Channels.FirstOrDefault(c => c.Id == textChannelId);
                Console.WriteLine(TextChannel);

                var connection = await VoiceExtension.ConnectAsync(VoiceChannel).ConfigureAwait(false);
                ConnectionModel = new ConnectionModel(connection);
                await TextChannel.SendMessageAsync($"Connected to {VoiceChannel.Name}!");

                await MusicPlayLoop(TextChannel, ConnectionModel);
            }
            else
            {
                Console.WriteLine("This is an internal tool to be used by WamBot's music commands. Use outside will cause unexpected results and is not recommended.");
                Console.Write("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }

        private static async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            if (e.Guild.Id == guildId)
            {
                ready = true;
            }
        }

        private static void Context_DSharpPlusLogMessage(object sender, DebugLogMessageEventArgs e)
        {
            Console.WriteLine(e);
        }

        private static void Context_LogMessage(object sender, string e)
        {
            Console.WriteLine(e);
        }

        private static async Task MusicPlayLoop(DiscordChannel channel, ConnectionModel connection)
        {
            try
            {
                while (connection.Connected)
                {
                    if (connection.Songs.TryDequeue(out SongModel model))
                    {
                        await channel.SendMessageAsync($"Now Playing: **{model}**");

                        try
                        {
                            using (MediaFoundationReader reader = new MediaFoundationReader(model.FilePath))
                            using (MediaFoundationResampler resampler = new MediaFoundationResampler(reader, 48000))
                            {
                                VolumeSampleProvider volume = new VolumeSampleProvider(resampler.ToSampleProvider()) { Volume = connection.Volume };
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
                                    connection.RecordBuffer.AddSamples(buff, 0, buff.Length);
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
                            await channel.SendMessageAsync($"Something went a bit wrong attempting to play that and a {ex.GetType().Name} was thrown. Sorry!\r\n{ex.Message}");
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
