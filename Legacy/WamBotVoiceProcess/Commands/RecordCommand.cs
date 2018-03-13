using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using WamBotVoiceProcess.Models;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    class RecordCommand : MusicCommand
    {
        Dictionary<uint, BufferedWaveProvider> waveProviders = new Dictionary<uint, BufferedWaveProvider>();
        WaveFormat format = new WaveFormat(48000, 2);
        MixingWaveProvider32 mixer = new MixingWaveProvider32();
        WaveRecorder recorder;
        ConnectionModel model;
        string path;
        WaveOut output;

        public override string Name => "Record";

        public override string Description => "Starts or stops recording audio from the current voice channel.";

        public override string[] Aliases => new[] { "rec" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override async Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            if (connection.Recording)
            {
                connection.Recording = false;
            }
            else
            {
                try
                {
                    SilenceProvider silence = new SilenceProvider(format);
                    mixer.AddInputStream(new Wave16ToFloatProvider(silence));
                    mixer.AddInputStream(new Wave16ToFloatProvider(new VolumeWaveProvider16(connection.RecordBuffer) { Volume = 0.1f }));

                    path = Path.ChangeExtension(Path.GetTempFileName(), ".wav");
                    recorder = new WaveRecorder(mixer, path);
                    model = connection;

                    connection.Connection.VoiceReceived += Connection_VoiceReceived;
                    connection.Disconnected += Connection_Disconnected;
                    connection.Recording = true;

                    await context.ReplyAsync("Now recording...");

                    output = new WaveOut();
                    output.Init(new VolumeWaveProvider16(new WaveFloatTo16Provider(recorder)) { Volume = 0.0f });

                    int pingInSamples = (48000 * (connection.Connection.Ping / 1000)) * 2;
                    connection.RecordBuffer.ClearBuffer();
                    connection.RecordBuffer.AddSamples(Enumerable.Repeat((byte)0, pingInSamples).ToArray(), 0, pingInSamples);
                    output.Play();
                }
                catch { }

                while (connection.Connected && connection.Recording) { await Task.Delay(100); }

                await CleanupAsync();

                string newPath = new Uri(Path.Combine(Path.GetTempPath(), $"{context.Guild.Name.ToLower()} - {DateTime.Now.ToString("dd-MM-yyyy HH-mm", CultureInfo.InvariantCulture)}.mp3")).LocalPath;
                MediaFoundationEncoder.EncodeToMp3(new WaveFileReader(path), newPath);
                await context.Channel.SendFileAsync(newPath, $"Here's your ~~clusterfuck~~ recording!");
            }

            return CommandResult.Empty;
        }

        private void Connection_Disconnected(object sender, EventArgs e)
        {
            (sender as ConnectionModel).Connection.VoiceReceived -= Connection_VoiceReceived;
        }

        private Task CleanupAsync()
        {
            model.Connection.VoiceReceived -= Connection_VoiceReceived;
            output.Stop();
            foreach (var input in waveProviders.Values)
            {
                mixer.RemoveInputStream(input);
            }
            recorder.Dispose();
            output.Dispose();
            return Task.CompletedTask;
        }

        private Task Connection_VoiceReceived(VoiceReceiveEventArgs e)
        {
            if (!waveProviders.ContainsKey(e.SSRC))
            {
                BufferedWaveProvider provider = new BufferedWaveProvider(format) { DiscardOnBufferOverflow = true, BufferDuration = TimeSpan.FromMilliseconds(1000) };
                mixer.AddInputStream(new Wave16ToFloatProvider(provider));
                waveProviders[e.SSRC] = provider;
            }

            waveProviders[e.SSRC].AddSamples(e.Voice.ToArray(), 0, e.Voice.Count);
            return Task.CompletedTask;
        }
    }
}
