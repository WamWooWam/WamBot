using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Api
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class TriggersTypingAttribute : CommandAttribute
    {
        public bool Triggers => _triggers;
        bool _triggers;

        public TriggersTypingAttribute(bool triggers = true)
        {
            _triggers = triggers;
        }
    }
}
