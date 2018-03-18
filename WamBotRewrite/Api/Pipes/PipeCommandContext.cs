using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Data;

namespace WamBotRewrite.Api.Pipes
{
    class PipeCommandContext : CommandContext
    {
        private IMessage _message;
        private IUser _author;
        private IMessageChannel _channel;
        private IGuild _guild;
        private PipeContext _ctx;

        public PipeCommandContext(PipeContext ctx) : base(ctx.Arguments, null, null, null)
        {
            _ctx = ctx;
        }

        public override IMessage Message { get => _message ?? GetMessage(); internal set => _message = value; }
        
        public override IUser Author => _author ?? GetAuthor();

        public override IMessageChannel Channel => _channel ?? GetChannel();

        public override IGuild Guild => _guild ?? GetGuild();

        public override EmbedBuilder GetEmbedBuilder(string title = null)
        {
            var asm = Assembly.GetExecutingAssembly().GetName();
            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor($"{(title != null ? $"{title} - " : "")}WamBot {asm.Version.ToString(3)}", Program.Application?.IconUrl)
            .WithColor(Message.Author is IGuildUser m ? Tools.GetUserColor(m) : new Color(0, 137, 255));

            return embedBuilder;
        }

        public override async Task ReplyAsync(string content = "", bool tts = false, Embed emb = null)
        {
            IChannel channel = (IChannel)_channel ?? await Program.RestClient.GetChannelAsync(_ctx.ChannelId);
            if(channel is IMessageChannel msg)
            {
                await msg.SendMessageAsync(content, tts, emb);
            }
            else
            {
                throw new InvalidOperationException("Okay, what the fuck??");
            }
        }

        private IUser GetAuthor()
        {
            _author = Program.RestClient.GetUserAsync(_ctx.AuthorId).GetAwaiter().GetResult();
            return _author;
        }

        private IMessageChannel GetChannel()
        {
            _channel = Program.RestClient.GetChannelAsync(_ctx.ChannelId).GetAwaiter().GetResult() as IMessageChannel;
            return _channel;
        }

        private IMessage GetMessage()
        {
            _message = Channel.GetMessageAsync(_ctx.MessageId).GetAwaiter().GetResult();
            return _message;
        }

        private IGuild GetGuild()
        {
            if (_ctx.GuildId != null)
            {
                _guild = Program.RestClient.GetGuildAsync(_ctx.GuildId.Value).GetAwaiter().GetResult();
                return _guild;
            }
            else
            {
                return null;
            }
        }
    }
}
