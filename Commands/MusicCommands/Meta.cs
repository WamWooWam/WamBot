using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using WamBot.Api;

namespace MusicCommands
{
    
    public class CommandsAssemblyInfo : ICommandsAssembly
    {
        public string Name => "Music";

        public string Description => "Allows WamBot to connect to voice channels and play music";

        public Version Version => new Version(1, 0, 3, 1);
    }
}
