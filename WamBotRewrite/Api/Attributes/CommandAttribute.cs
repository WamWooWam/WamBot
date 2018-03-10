using System;
using System.Collections.Generic;
using System.Text;

using Discord;

namespace WamBotRewrite.Api
{
    /// <summary>
    /// Defines a method as a command that can be invoked by bot users.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        internal string Name { get; private set; }
        internal string Description { get; private set; }
        internal string[] Aliases { get; private set; }

        public CommandAttribute(string name, string description, string[] aliases)
        {
            Name = name;
            Description = description;
            Aliases = aliases;
        }

        public string ExtendedDescription { get; set; }

    }
}
