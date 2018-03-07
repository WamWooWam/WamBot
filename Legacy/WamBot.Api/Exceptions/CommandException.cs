using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Api
{
    public class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }
}
