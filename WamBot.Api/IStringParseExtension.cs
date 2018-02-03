using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Api
{
    public interface IParseExtension
    {
        string Name { get; }
        string Description { get; }
        IEnumerable<string> Parse(IEnumerable<string> args, DiscordChannel c);
    }
}
