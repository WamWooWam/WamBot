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

namespace WamBotRewrite.Models
{
    internal class ConnectionModel
    {
        internal float _volume = 0.66f;
        private bool _connected;

        public ConnectionModel(IAudioClient connection)
        {
            Connection = connection;
            connection.StreamCreated += Connection_StreamCreated;
            connection.StreamDestroyed += Connection_StreamDestroyed;
            Connected = true;
            Songs = new ConcurrentQueue<SongModel>();
            ConnectTime = DateTime.Now;
            TokenSource = new CancellationTokenSource();
            RecordBuffer = new BufferedWaveProvider(new WaveFormat(48000, 2)) { BufferDuration = TimeSpan.FromSeconds(1), DiscardOnBufferOverflow = true };
            AvailableStreams = new ConcurrentDictionary<ulong, AudioInStream>();
        }

        private Task Connection_StreamDestroyed(ulong arg)
        {
            AvailableStreams.TryRemove(arg, out _);
            return Task.CompletedTask;
        }

        private Task Connection_StreamCreated(ulong arg1, AudioInStream arg2)
        {
            AvailableStreams[arg1] = arg2;
            return Task.CompletedTask;
        }

        public IAudioClient Connection { get; private set; }
        public VolumeSampleProvider SampleProvider { get; set; }
        public BufferedWaveProvider RecordBuffer { get; set; }
        public ConcurrentQueue<SongModel> Songs { get; private set; }
        public ConcurrentDictionary<ulong, AudioInStream> AvailableStreams { get; set; }

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
