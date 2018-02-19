using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WamBot.Api.Data;
using WamWooWam.Core;

namespace WamBot.Api
{
    public class CommandContext
    {
        private TaskCompletionSource<DiscordMessage> _replyCompletionSource;
        private DiscordClient _client;
        private int _happiness;
        internal ILogger _logger;

        internal CommandContext(string[] args, DiscordMessage msg, DiscordClient client)
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
        public DiscordMessage Message { get; internal set; }

        /// <summary>
        /// The user that invoked the command
        /// </summary>
        public DiscordUser Author => 
            Message.Author;

        /// <summary>
        /// The channel that this command was invoked within
        /// </summary>
        public DiscordChannel Channel =>
            Message.Channel;

        /// <summary>
        /// The guild (if available) this command was invoked within.
        /// </summary>
        public DiscordGuild Guild =>
            Message.Channel.Guild;
        
        public DiscordClient Client
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
            get => _happiness.Clamp(sbyte.MinValue, sbyte.MaxValue);
            set => _happiness = (sbyte)(value.Clamp(sbyte.MinValue, sbyte.MaxValue));
        }
        
        public HappinessLevel HappinessLevel => 
            Tools.GetHappinessLevel((sbyte)Happiness);

        public GuildData GuildData { get; internal set; }
        public ChannelData ChannelData { get; internal set; }
        public UserData UserData { get; internal set; }

        public void Log(string str, DSharpPlus.LogLevel level = DSharpPlus.LogLevel.Info, Exception ex = null)
        {
            switch (level)
            {
                case DSharpPlus.LogLevel.Debug:
                    _logger.LogDebug(str, new object[] { });
                    break;
                case DSharpPlus.LogLevel.Info:
                    _logger.LogInformation(str, new object[] { });
                    break;
                case DSharpPlus.LogLevel.Warning:
                    _logger.LogWarning(str, new object[] { });
                    break;
                case DSharpPlus.LogLevel.Error:
                    _logger.LogError(ex, str, new object[] { });
                    break;
                case DSharpPlus.LogLevel.Critical:
                    _logger.LogCritical(str, new object[] { });
                    break;
                default:
                    break;
            }
        }

        public Task<DiscordMessage> ReplyAsync(string content = null, bool tts = false, DiscordEmbed emb = null) =>
            Message.Channel.SendMessageAsync(content, tts, emb);

        public async Task<DiscordMessage> ReplyAndAwaitResponseAsync(string content, int timeout = 10_000)
        {
            _replyCompletionSource = new TaskCompletionSource<DiscordMessage>();
            CancellationTokenSource source = new CancellationTokenSource();

            await ReplyAsync(content);

            try
            {
                _client.MessageCreated += Client_MessageCreated;
                source.CancelAfter(timeout);
                using (source.Token.Register(() => _replyCompletionSource.TrySetCanceled()))
                {
                    return await _replyCompletionSource.Task;
                }
            }
            finally
            {
                _client.MessageCreated -= Client_MessageCreated;
            }
        }

        private Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Message.Author.Id == Message.Author.Id)
            {
                _replyCompletionSource.TrySetResult(e.Message);
            }

            return Task.CompletedTask;
        }
    }


    public enum HappinessLevel
    {
        Hate,
        Dislike,
        Indifferent,
        Like,
        Adore
    }
}