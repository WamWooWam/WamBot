using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Data;

namespace WamBotRewrite.Api.Pipes
{
    class PipeContext
    {
        [JsonConstructor]
        public PipeContext() { }

        public PipeContext(CommandContext ctx)
        {
            MessageId = ctx.Message.Id;
            AuthorId = ctx.Author.Id;
            ChannelId = ctx.Channel.Id;
            GuildId = ctx.Guild?.Id;
            UserData = ctx.UserData;
            Happiness = ctx.Happiness;
            Arguments = ctx.Arguments;
        }

        public string[] Arguments { get; set; }

        public ulong MessageId { get; set; }

        public ulong AuthorId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong? GuildId { get; set; }

        public User UserData { get; set; }

        public int Happiness { get; set; }
    }
}
