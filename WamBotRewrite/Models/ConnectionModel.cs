using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using System.IO;
using WamBotRewrite.Api;
using System.Globalization;
using Discord.WebSocket;

namespace WamBotRewrite.Models
{
    internal class ConnectionModel : IDisposable
    {
        private static WaveFormat _format = new WaveFormat(48000, 2);
        internal float _volume = 0.66f;
        private bool _connected;
        private string path;
        private WaveRecorder recorder;
        private WaveOut output;
        private CommandContext _context;
        private ConcurrentDictionary<ulong, BufferedWaveProvider> buffers = new ConcurrentDictionary<ulong, BufferedWaveProvider>();

        IVoiceChannel _voiceChannel;

        public ConnectionModel(IAudioClient connection, IVoiceChannel voiceChannel, CommandContext context)
        {
            _context = context;
            _voiceChannel = voiceChannel;

            Connection = connection;
            connection.StreamCreated += Connection_StreamCreated;
            connection.StreamDestroyed += Connection_StreamDestroyed;
            connection.Disconnected += Connection_Disconnected;

            Connected = true;
            Songs = new ConcurrentQueue<SongModel>();
            ConnectTime = DateTime.Now;
            TokenSource = new CancellationTokenSource();
            RecordBuffer = new BufferedWaveProvider(new WaveFormat(48000, 2)) { BufferDuration = TimeSpan.FromSeconds(1), DiscardOnBufferOverflow = true };

            AvailableStreams = new ConcurrentDictionary<ulong, AudioInStream>();
            Mixer = new MixingWaveProvider32();
        }

        private Task Connection_Disconnected(Exception arg)
        {
            Connected = false;
            return Task.CompletedTask;
        }

        private Task Connection_StreamDestroyed(ulong arg)
        {
            if (buffers.TryRemove(arg, out var provider))
            {
                Mixer.RemoveInputStream(provider);
            }

            return Task.CompletedTask;
        }

        private Task Connection_StreamCreated(ulong arg1, AudioInStream arg2)
        {
            AvailableStreams[arg1] = arg2;
            return Task.CompletedTask;
        }

        public async Task StartRecordingAsync()
        {
            Recording = true;

            SilenceProvider silence = new SilenceProvider(_format);
            Mixer.AddInputStream(new Wave16ToFloatProvider(silence));
            Mixer.AddInputStream(new Wave16ToFloatProvider(new VolumeWaveProvider16(RecordBuffer) { Volume = 0.1f }));

            path = Path.ChangeExtension(Path.GetTempFileName(), ".wav");
            recorder = new WaveRecorder(Mixer, path);

            var users = (await _voiceChannel.GetUsersAsync().FlattenAsync()).Cast<SocketGuildUser>().Where(u => u.AudioStream != null);
            foreach (var user in users)
            {
                AvailableStreams.TryAdd(user.Id, user.AudioStream);
            }

            output = new WaveOut();
            output.Init(/* new VolumeWaveProvider16(new WaveFloatTo16Provider(recorder)) { Volume = 0.0f }*/ recorder);

            int pingInSamples = (48000 * (Connection.UdpLatency / 1000)) * 2;
            RecordBuffer.ClearBuffer();
            RecordBuffer.AddSamples(Enumerable.Repeat((byte)0, pingInSamples).ToArray(), 0, pingInSamples);
            output.Play();

            CancellationTokenSource source = new CancellationTokenSource();
            while (Connected && Recording)
            {
                foreach(ulong key in AvailableStreams.Keys)
                {
                    var i = AvailableStreams[key];
                    if(buffers.TryGetValue(key, out var provider))
                    {
                        ReadToWaveProvider(source, i, provider);
                    }
                    else
                    {
                        BufferedWaveProvider prov = new BufferedWaveProvider(_format);
                        Mixer.AddInputStream(new Wave16ToFloatProvider(prov));
                        buffers[key] = prov;
                        ReadToWaveProvider(source, i, prov);
                    }
                }
            }

            Cleanup();

            string newPath = new Uri(Path.Combine(Path.GetTempPath(), $"{_context.Guild.Name.ToLower()} - {DateTime.Now.ToString("dd-MM-yyyy HH-mm", CultureInfo.InvariantCulture)}.mp3")).LocalPath;
            MediaFoundationEncoder.EncodeToMp3(new WaveFileReader(path), newPath);
            await _context.Channel.SendFileAsync(newPath, $"Here's your ~~clusterfuck~~ recording!");
        }

        private static void ReadToWaveProvider(CancellationTokenSource source, AudioInStream i, BufferedWaveProvider provider)
        {
            source.CancelAfter(66);
            if (i.TryReadFrame(source.Token, out RTPFrame frame))
            {
                provider.AddSamples(frame.Payload, 0, frame.Payload.Length);
            }
            source = new CancellationTokenSource();
        }

        private void Cleanup()
        {
            Connection.StreamCreated -= Connection_StreamCreated;
            Connection.StreamDestroyed -= Connection_StreamDestroyed;
            Connection.Disconnected -= Connection_Disconnected;
            output.Stop();
            foreach (var input in buffers.Values)
            {
                Mixer.RemoveInputStream(input);
            }
            recorder.Dispose();
            output.Dispose();
        }

        public void Dispose()
        {

        }

        public IAudioClient Connection { get; private set; }
        public IVoiceChannel Channel => _voiceChannel;
        public VolumeSampleProvider SampleProvider { get; set; }
        public BufferedWaveProvider RecordBuffer { get; set; }
        public ConcurrentQueue<SongModel> Songs { get; private set; }
        public ConcurrentDictionary<ulong, AudioInStream> AvailableStreams { get; set; }
        public MixingWaveProvider32 Mixer { get; set; }

        public SongModel NowPlaying { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan Total { get; set; }

        public bool Skip { get; set; }
        public bool Recording { get; set; }

        public DateTime ConnectTime { get; set; }

        public bool Connected
        {
            get => _connected; internal
            set
            {
                _connected = value;
                if (value == false)
                    Disconnected?.Invoke(this, null);
            }
        }

        public CancellationToken Token => TokenSource.Token;
        public CancellationTokenSource TokenSource { get; internal set; }

        public event EventHandler Disconnected;

        public float Volume
        {
            get => SampleProvider?.Volume ?? _volume;
            set
            {
                _volume = value;
                if (SampleProvider != null)
                    SampleProvider.Volume = value;
            }
        }
    }
}
