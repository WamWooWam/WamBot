using DSharpPlus.VoiceNext;
using Google.Apis.YouTube.v3;
using MusicCommands.Models;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using WamBot.Api;

namespace MusicCommands
{
    internal class Static
    {
        static Static()
        {
            MediaFoundationApi.Startup();
        }

        ~Static()
        {
            foreach (ConnectionModel connection in Connections.Values)
            {
                connection.Connection.Disconnect();
            }

            MediaFoundationApi.Shutdown();
        }
        internal static YouTubeService YouTubeService { get; set; }
        internal static VoiceNextExtension VoiceExtention;
        internal static Dictionary<ulong, ConnectionModel> Connections = new Dictionary<ulong, ConnectionModel>();
    }

    public class CommandsAssemblyInfo : ICommandsAssembly
    {
        public string Name => "Music";

        public string Description => "Allows WamBot to connect to voice channels and play music";

        public Version Version => new Version(1, 0, 3, 1);
    }
}
