using System;
using System.Collections.Generic;
using System.Text;

namespace WamBotRewrite.Api
{
    /// <summary>
    /// Allows a command to ignore arguments passed to it and parse 
    /// the raw message content itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class IgnoreArgumentsAttribute : Attribute
    {
    }
}
