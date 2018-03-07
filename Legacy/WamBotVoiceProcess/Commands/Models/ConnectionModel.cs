using DSharpPlus.VoiceNext;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WamBotVoiceProcess.Models
{
    class ConnectionModel
    {
        internal float _volume = 0.66f;
        private bool _connected;

        public ConnectionModel(VoiceNextConnection connection)
        {
            Connection = connection;
            Connected = true;
            Songs = new ConcurrentQueue<SongModel>();
            ConnectTime = DateTime.Now;
            TokenSource = new CancellationTokenSource();
            RecordBuffer = new BufferedWaveProvider(new WaveFormat(48000, 2)) { BufferDuration = TimeSpan.FromSeconds(1), DiscardOnBufferOverflow = true };
        }

        public VoiceNextConnection Connection { get; private set; }
        public VolumeSampleProvider SampleProvider { get; set; }
        public BufferedWaveProvider RecordBuffer { get; set; }
        public ConcurrentQueue<SongModel> Songs { get; private set; }

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
