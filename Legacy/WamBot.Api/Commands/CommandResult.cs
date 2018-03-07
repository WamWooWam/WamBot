using System;
using System.Collections.Generic;
using System.IO;
using DSharpPlus.Entities;

namespace WamBot.Api
{
    public class CommandResult : IDisposable
    {
        public CommandResult()
        {
            ReturnType = ReturnType.None;
        }

        public CommandResult(string text, string attachment = null)
        {
            ReturnType = ReturnType.Text;
            ResultText = text;
            Attachment = attachment;
        }

        public CommandResult(DiscordEmbed embed, string attachment = null)
        {
            ReturnType = ReturnType.Embed;
            ResultEmbed = embed;
            Attachment = attachment;
        }

        public CommandResult(Stream str, string filePath)
        {
            ReturnType = ReturnType.File;
            FileName = filePath;
            Stream = str;
        }

        public static CommandResult Empty => new CommandResult();

        public static implicit operator CommandResult(string text) => new CommandResult(text);

        public static implicit operator CommandResult(DiscordEmbed embed) => new CommandResult(embed);

        public ReturnType ReturnType { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }
        public string ResultText { get; set; }
        public DiscordEmbed ResultEmbed { get; set; }
        public string Attachment { get; set; }
        public Dictionary<string, string> InsightsData { get; set; } = new Dictionary<string, string>();

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }

    public enum ReturnType
    {
        Text, Embed, File, None
    }
}