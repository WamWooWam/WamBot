using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WamBotRewrite.Misc
{
    class BetterWaveStream : WaveStream
    {
        private readonly Stream sourceStream;
        private readonly WaveFormat waveFormat;
        private readonly Process process;

        public Process Process => process;

        /// <summary>
        /// Initialises a new instance of RawSourceWaveStream
        /// </summary>
        /// <param name="sourceStream">The source stream containing raw audio</param>
        /// <param name="waveFormat">The waveformat of the audio in the source stream</param>
        public BetterWaveStream(Stream sourceStream, WaveFormat waveFormat, Process process)
        {
            this.sourceStream = sourceStream;
            this.waveFormat = waveFormat;
            this.process = process;
        }

        /// <summary>
        /// The WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Zero
        /// </summary>
        public override long Length => 0;

        /// <summary>
        /// The current position in this stream
        /// </summary>
        public override long Position
        {
            get => sourceStream.Position;
            set { }
        }

        /// <summary>
        /// Reads data from the stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return sourceStream.Read(buffer, offset, count);
            }
            catch (EndOfStreamException)
            {
                return 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            sourceStream.Dispose();
            if (!process.HasExited)
                process.Kill();
        }
    }
}
