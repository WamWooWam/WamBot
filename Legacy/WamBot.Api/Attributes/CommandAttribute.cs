using System;

namespace WamBot.Api
{
    public class CommandAttribute : BaseCommandAttribute
    {
        internal string _displayName;
        internal string _discription;
        internal string[] _aliases;

        public CommandAttribute(string displayName, string discription, string[] aliases)
        {
            _displayName = displayName;
            _discription = discription;
            _aliases = aliases;
        }
    }
}