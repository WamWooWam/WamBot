using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamBot.Commands;
using WamBot.Data;
using WamWooWam.Core;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace WamBot.Services
{
    internal class BotService : IHostedService, IDisposable
    {
        private readonly DiscordClient _discordClient;
        private readonly ILogger<DiscordClient> _clientLogger;
        private readonly ILogger<CommandsNextExtension> _commandsLogger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;

        public BotService(
            DiscordClient client,
            IConfiguration config,
            ILogger<DiscordClient> clientLogger,
            ILogger<CommandsNextExtension> commandsLogger,
            IServiceProvider serviceProvider)
        {
            _config = config;
            _discordClient = client;
            _clientLogger = clientLogger;
            _commandsLogger = commandsLogger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ConfigureLogging();

            var commands = _discordClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { _config["Bot:Prefix"] },
                Services = _serviceProvider,
                EnableDefaultHelp = false
            });

            _discordClient.UseInteractivity(new InteractivityConfiguration() { PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround });

            commands.RegisterCommands<HelpCommand>();
            commands.RegisterCommands<BaseCommands>();
            commands.RegisterCommands<BotCommands>();
            commands.RegisterCommands<WpfCommands>();
            commands.RegisterCommands<OcrCommands>();
            commands.RegisterCommands<JavaScriptCommands>();

            commands.CommandErrored += Commands_CommandErrored;
            commands.CommandExecuted += Commands_CommandExecuted;

            await _discordClient.ConnectAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _discordClient.DisconnectAsync();
                _discordClient.Dispose();
            }
            catch { }
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            _commandsLogger.Log(LogLevel.Information, "{user} ran {prefix}{command} in {channel}!",
                e.Context.User,
                e.Context.Prefix,
                e.Context.Command.Name,
                e.Context.Channel);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            _commandsLogger.LogError(e.Exception, "An error occured when {user} ran {prefix}{command} in {channel}!",
                e.Context.User,
                e.Context.Prefix,
                e.Context.Command?.Name ?? "unknown",
                e.Context.Channel);

            try
            {
                var builder = e.Context.GetEmbedBuilder("Something's gone wrong!")
                    .WithDescription($"Something's gone very wrong executing that command, and an {e.Exception.GetType().Name} occured.")
                    .WithFooter("This message will be deleted in 10 seconds")
                    .WithTimestamp(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10))
                    .WithColor(new DiscordColor(255, 0, 0));

                builder.AddField("Message", $"```{e.Exception.Message.Truncate(1016)}```");
#if DEBUG
                var stackTrace = e.Exception.StackTrace;
                if (!string.IsNullOrWhiteSpace(stackTrace))
                    builder.AddField("Stack Trace", $"```{stackTrace.Truncate(1016)}```");
#endif

                var msg = await e.Context.Channel.SendMessageAsync("", embed: builder.Build());

                await Task.Delay(10_000);
                await msg.DeleteAsync();
            }
            catch { }
        }

        private void ConfigureLogging()
        {
            _discordClient.DebugLogger.LogMessageReceived += (o, e) =>
            {
                if (e.Exception != null)
                {
                    _clientLogger.LogError(e.Exception, e.Message);
                    return;
                }

                var level = LogLevel.None;

                switch (e.Level)
                {
                    case DSharpPlus.LogLevel.Debug:
                        level = LogLevel.Debug;
                        break;
                    case DSharpPlus.LogLevel.Info:
                        level = LogLevel.Information;
                        break;
                    case DSharpPlus.LogLevel.Warning:
                        level = LogLevel.Warning;
                        break;
                    case DSharpPlus.LogLevel.Error:
                        level = LogLevel.Error;
                        break;
                    case DSharpPlus.LogLevel.Critical:
                        level = LogLevel.Critical;
                        break;
                }

                _clientLogger.Log(level, e.Message);
            };
        }

        public void Dispose() { }
    }
}
