using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WamBot.Api;

namespace BaseCommands
{
    class CommandsAssemblyInfo : ICommandsAssembly
    {
        public string Name => "Base";

        public string Description => "WamBot's standard library of commands.";

        public Version Version => new Version(1, 0, 1, 12);
    }
}
