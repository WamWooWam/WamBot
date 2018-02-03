using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Threading;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace WamBot.Api
{
    public delegate void LoggerCallback(string log);

    public class CommandContext
    {
        private TaskCompletionSource<DiscordMessage> _replyCompletionSource;

        public string[] Arguments { get; set; }

        public DiscordMessage Message { get; internal set; }

        public DiscordUser Invoker { get; internal set; }

        public DiscordClient Client { get => _client; internal set => _client = value; }

        public string Prefix { get; internal set; }

        public int Happiness { get => (sbyte)(_happiness.Clamp(sbyte.MinValue, sbyte.MaxValue)); set => _happiness = (sbyte)(value.Clamp(sbyte.MinValue, sbyte.MaxValue)); }

        public HappinessLevel HappinessLevel => Tools.GetHappinessLevel((sbyte)Happiness);

        public DiscordChannel Channel => Message.Channel;

        public DiscordGuild Guild => Message.Channel.Guild;

        public LoggerCallback Log { get; internal set; }
        private DiscordClient _client;
        private int _happiness;

        public Task<DiscordMessage> ReplyAsync(string content = null, bool tts = false, DiscordEmbed emb = null) => Message.Channel.SendMessageAsync(content, tts, emb);

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