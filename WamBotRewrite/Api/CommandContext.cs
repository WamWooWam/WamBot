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
        private DiscordSocketClient _client;

        internal CommandContext(string[] args, IMessage msg, DiscordSocketClient client, WamBotContext db)
        {
            Arguments = args;
            Message = msg;
            _client = client;
            DbContext = db;
        }

        /// <summary>
        /// Raw arguments
        /// </summary>
        public string[] Arguments { get; set; }

        /// <summary>
        /// The raw message that invoked this command
        /// </summary>
        public virtual IMessage Message { get; internal set; }

        /// <summary>
        /// The user that invoked the command
        /// </summary>
        public virtual IUser Author =>
            Message.Author;

        /// <summary>
        /// The channel that this command was invoked within
        /// </summary>
        public virtual IMessageChannel Channel =>
            Message.Channel;

        /// <summary>
        /// The guild (if available) this command was invoked within.
        /// </summary>
        public virtual IGuild Guild =>
            Message.Channel is IGuildChannel c ? c.Guild : null;

        public DiscordSocketClient Client
        {
            get => _client;
            internal set => _client = value;
        }

        public WamBotContext DbContext { get; private set; }

        internal CommandRunner Command { get; set; }

        public Dictionary<string, object> AdditionalData { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// The bot's current happiness, will be synched when command returns.
        /// </summary>
        public int Happiness
        {
            get => UserData.Happiness;
            set => UserData.Happiness = (sbyte)((value.Clamp(sbyte.MinValue, sbyte.MaxValue)));
        }

        public HappinessLevel HappinessLevel =>
            Tools.GetHappinessLevel((sbyte)Happiness);

        public User UserData { get; internal set; }
        public Channel ChannelData { get; internal set; }
        public Guild GuildData { get; internal set; }

        public virtual EmbedBuilder GetEmbedBuilder(string title = null)
        {
            var asm = Assembly.GetExecutingAssembly().GetName();
            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor($"{(title != null ? $"{title} - " : "")}WamBot {asm.Version.ToString(3)}", Program.Application.IconUrl)
            .WithColor(Program.AccentColour);

            return embedBuilder;
        }

        public virtual async Task ReplyAsync(string content = "", bool tts = false, Embed emb = null)
        {
            if (DateTime.Now.IsAprilFools())
            {
                content = content.Replace("n", "ny").Replace("r", "w");
            }

            if (content.Length > 2000)
            {
                await Channel.SendChunkedMessageAsync(content, tts, ChannelData);
            }
            else
            {
                if (ChannelData != null)
                    ChannelData.MessagesSent += 1;

                await Channel.SendMessageAsync(content, tts, emb);
            }
        }

        public virtual async Task<IMessage> ReplyAndAwaitResponseAsync(string content, Embed embed = null, int timeout = 10_000)
        {
            var _replyCompletionSource = new TaskCompletionSource<IMessage>();
            CancellationTokenSource source = new CancellationTokenSource();

            await ReplyAsync(content, emb: embed);

            Func<SocketMessage, Task> task = (arg) =>
            {
                if (arg.Author.Id == Message.Author.Id)
                {
                    _replyCompletionSource.TrySetResult(arg);
                }

                return Task.CompletedTask;
            };

            try
            {
                _client.MessageReceived += task;
                source.CancelAfter(timeout);
                using (source.Token.Register(() => _replyCompletionSource.TrySetCanceled()))
                {
                    return await _replyCompletionSource.Task;
                }
            }
            finally
            {
                _client.MessageReceived -= task;
            }
        }
    }
}
