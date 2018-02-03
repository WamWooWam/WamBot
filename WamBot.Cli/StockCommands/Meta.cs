using System;
using System.Collections.Generic;
using System.Text;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    class Meta : ICommandsAssembly
    {
        public string Name => "Stock";

        public string Description => "WamBot's built in commands.";

        public Version Version => new Version(1, 0, 0, 0);
    }
}
