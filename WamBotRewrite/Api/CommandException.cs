using System;
using System.Collections.Generic;
using System.Text;

namespace WamBotRewrite.Api
{
    class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }

    }
}
