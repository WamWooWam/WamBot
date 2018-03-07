using System;
using System.Collections.Generic;
using System.Text;
using WamBot.Api;

namespace Variables
{
    class Meta : ICommandsAssembly
    {
        public string Name => "Variables";

        public string Description => "Enables the use of variables within commands! Recall a value with %[name]%!";

        public Version Version => new Version(1, 0, 0, 0);
    }
}
