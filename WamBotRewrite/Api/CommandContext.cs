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
            .WithColor(Message.Author is IGuildUser m ? Tools.GetUserColor(m) : new Color(0, 137, 255));

            return embedBuilder;
        }

        public virtual async Task ReplyAsync(string content = "", bool tts = false, Embed emb = null)
        {
            if (content.Length > 2000)
            {
                for (int i = 0; i < content.Length; i += 1993)
                {
                    string str = content.Substring(i, Math.Min(1993, content.Length - i));
                    if (content.StartsWith("```") && !str.StartsWith("```"))
                    {
                        str = "```" + str;
                    }
                    if (content.EndsWith("```") && !str.EndsWith("```"))
                    {
                        str = str + "```";
                    }

                    await Program.LogMessage(this, $"Chunking message to {str.Length} chars");

                    await Channel.SendMessageAsync(str);

                    await Task.Delay(2000);
                }
            }
            else
            {
                await Channel.SendMessageAsync(content, false, emb);
            }
        }

        public virtual async Task<IMessage> ReplyAndAwaitResponseAsync(string content, Embed embed = null, int timeout = 10_000)
        {
            _replyCompletionSource = new TaskCompletionSource<IMessage>();
            CancellationTokenSource source = new CancellationTokenSource();

            await ReplyAsync(content, emb: embed);

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
