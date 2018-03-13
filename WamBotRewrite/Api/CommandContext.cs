using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamBotRewrite.Data;
using WamWooWam.Core;

namespace WamBotRewrite.Api
{
    public class CommandContext
    {
        private TaskCompletionSource<IMessage> _replyCompletionSource;
        private DiscordSocketClient _client;

        internal CommandContext(string[] args, IMessage msg, DiscordSocketClient client)
        {
            Arguments = args;
            Message = msg;
            _client = client;
        }

        /// <summary>
        /// Raw arguments
        /// </summary>
        public string[] Arguments { get; set; }

        /// <summary>
        /// The raw message that invoked this command
        /// </summary>
        public IMessage Message { get; internal set; }

        /// <summary>
        /// The user that invoked the command
        /// </summary>
        public IUser Author =>
            Message.Author;

        /// <summary>
        /// The channel that this command was invoked within
        /// </summary>
        public IMessageChannel Channel =>
            Message.Channel;

        /// <summary>
        /// The guild (if available) this command was invoked within.
        /// </summary>
        public IGuild Guild =>
            Message.Channel is IGuildChannel c ? c.Guild : null;

        public DiscordSocketClient Client
        {
            get => _client;
            internal set => _client = value;
        }

        public Dictionary<string, object> AdditionalData { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// The bot's current happiness, will be synched when command returns.
        /// </summary>
        public int Happiness
        {
            get => UserData.Happiness.Clamp(sbyte.MinValue, sbyte.MaxValue);
            set => UserData.Happiness = (sbyte)(value.Clamp(sbyte.MinValue, sbyte.MaxValue));
        }

        public HappinessLevel HappinessLevel =>
            Tools.GetHappinessLevel((sbyte)Happiness);
        
        public User UserData { get; internal set; }

        public EmbedBuilder GetEmbedBuilder(string title = null)
        {
            var asm = Assembly.GetExecutingAssembly().GetName();
            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor($"{(title != null ? $"{title} - " : "")}WamBot {asm.Version.ToString(3)}", Program.Application.IconUrl)
            .WithColor(Message.Author is IGuildUser m ? Tools.GetUserColor(m) : new Color(0, 137, 255));

            return embedBuilder;
        }

        public Task<IUserMessage> ReplyAsync(string content = null, bool tts = false, Embed emb = null) =>
            Message.Channel.SendMessageAsync(content, tts, emb);

        public async Task<IMessage> ReplyAndAwaitResponseAsync(string content, int timeout = 10_000)
        {
            _replyCompletionSource = new TaskCompletionSource<IMessage>();
            CancellationTokenSource source = new CancellationTokenSource();

            await ReplyAsync(content);

            try
            {
                _client.MessageReceived += _client_MessageReceived;
                source.CancelAfter(timeout);
                using (source.Token.Register(() => _replyCompletionSource.TrySetCanceled()))
                {
                    return await _replyCompletionSource.Task;
                }
            }
            finally
            {
                _client.MessageReceived -= _client_MessageReceived;
            }
        }

        private Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg.Author.Id == Message.Author.Id)
            {
                _replyCompletionSource.TrySetResult(arg);
            }

            return Task.CompletedTask;
        }
    }
}
