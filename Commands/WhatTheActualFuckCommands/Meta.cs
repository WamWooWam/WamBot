using System;
using WamBot.Api;

namespace WhatTheActualFuckCommands
{
    public class Meta : ICommandsAssembly
    {
        public string Name => "What The Actual Fuck";

        public string Description => "Look, I get bored, okay??";
    }

    public static class Static
    {
        public static string ShrinkToEmbed(string code)
        {
            return (code.Length > 1010 ? code.Substring(0, 1000) + "..." : code);
        }
    }
}
