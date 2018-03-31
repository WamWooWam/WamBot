using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;

namespace WamBotRewrite.Api
{
    internal class CommandRequest
    {
        public CommandRequest(IMessage message, IEnumerable<string> commandSegments, CommandRunner command, DateTimeOffset start)
        {
            Message = message;
            CommandSegments = commandSegments;
            Command = command;
            Start = start;
        }

        internal IMessage Message { get; set; }
        internal IUser Author => Message.Author;
        internal IMessageChannel Channel => Message.Channel;
        internal IEnumerable<string> CommandSegments { get; set; }
        internal CommandRunner Command { get; set; }
        internal DateTimeOffset Start { get; set; }
    }
}