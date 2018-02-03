using DSharpPlus.VoiceNext;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicCommands.Models
{
    class ConnectionModel
    {
        public ConnectionModel(VoiceNextConnection connection)
        {
            Connection = connection;
            Connected = true;
            Songs = new ConcurrentQueue<SongModel>();
            ConnectTime = DateTime.Now;
            TokenSource = new CancellationTokenSource();
        }

        public VoiceNextConnection Connection { get; private set; }
        public VolumeSampleProvider SampleProvider { get; set; }
        public ConcurrentQueue<SongModel> Songs { get; private set; }

        public SongModel NowPlaying { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan Total { get; set; }

        public bool Skip { get; set; }
        public bool Pause { get; set; }

        public DateTime ConnectTime { get; set; }

        public bool Connected { get; internal set; }
        public CancellationToken Token => TokenSource.Token;
        public CancellationTokenSource TokenSource { get; internal set; }
    }
}
