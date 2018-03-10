using System;
using System.Collections.Generic;
using System.Text;

namespace WamBotRewrite.Api
{
    /// <summary>
    /// Defines a command as only runnable by the bot's owner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class OwnerOnlyAttribute : Attribute
    {
    }
}
