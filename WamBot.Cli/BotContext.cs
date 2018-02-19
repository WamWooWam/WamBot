using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using DSharpPlus.Entities;
using WamBot.Cli;
using WamBot.Cli.Models;
using System.Linq;
using WamBot.Api;

using System.IO;
using Newtonsoft.Json;
using WamWooWam.Core;
using WamBot.Data;
using WamBot.Api.Data;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace WamBot.Core
{
    public class BotContext
    {
        Config _config;
        DiscordClient _discordClient;
        HttpClient _httpClient;
        Timer _saveTimer;
        Timer _statusTimer;
        TelemetryClient _telemetryClient;
        List<DiscordCommand> _commands = new List<DiscordCommand>();
        List<IParseExtension> _parseExtensions = new List<IParseExtension>();
        Dictionary<ICommandsAssembly, List<DiscordCommand>> _assemblyCommands = new Dictionary<ICommandsAssembly, List<DiscordCommand>>();
        List<ulong> _processedMessageIds = new List<ulong>();
        ILoggerFactory _loggerFactory = new Data.LoggerFactory();

        bool _connected = false;
        Random random = new Random();

        public Config Config => _config;
        public DiscordClient Client => _discordClient;
        public HttpClient HttpClient => _httpClient;
        public List<DiscordCommand> Commands => _commands;
        public List<IParseExtension> ParseExtensions => _parseExtensions;
        internal Dictionary<ICommandsAssembly, List<DiscordCommand>> AssemblyCommands => _assemblyCommands;

        public event EventHandler<string> LogMessage;
        public event EventHandler<DebugLogMessageEventArgs> DSharpPlusLogMessage;

        public BotContext(Config config)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("user-agent", $"Mozilla/5.0 ({Environment.OSVersion.ToString()}; {Environment.OSVersion.Platform}; {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}) WamBot/3.0.0 (fuck you)");
            _config = config;
            _discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.Token,
                LogLevel = DSharpPlus.LogLevel.Debug
            });

            _discordClient.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
            _discordClient.Ready += Client_Ready;
            _discordClient.MessageCreated += Client_MessageCreated;
            _discordClient.MessageUpdated += Client_MessageUpdated;
            _discordClient.GuildCreated += Client_GuildCreated;
            _discordClient.GuildDeleted += Client_GuildDeleted;
            _discordClient.GuildAvailable += Client_GuildAvailable;
            _discordClient.GuildUnavailable += Client_GuildUnavailable;
            _discordClient.ClientErrored += Client_discordClientErrored;
            _discordClient.SocketErrored += Client_SocketErrored;

            _saveTimer?.Stop();
            _saveTimer = new Timer(30_000) { AutoReset = true };
            _saveTimer.Elapsed += _saveTimer_Elapsed;
            _saveTimer.Start();
        }

        public async Task ConnectAsync()
        {
            if (_connected)
            {
                throw new InvalidOperationException("God you're a twat.");
            }

            _statusTimer?.Stop();
            _statusTimer = new Timer(_config.StatusUpdateInterval.TotalMilliseconds) { AutoReset = true };
            _statusTimer.Elapsed += _statusTimer_Elapsed;
            _statusTimer.Start();

            _telemetryClient?.Flush();
            if (_config.ApplicationInsightsKey != Guid.Empty)
            {
                TelemetryConfiguration.Active.InstrumentationKey = _config.ApplicationInsightsKey.ToString();
                _telemetryClient = new TelemetryClient();
                _telemetryClient.TrackEvent(new EventTelemetry("Startup"));

                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                LogMessage?.Invoke(this, $"Application Insights telemetry configured! {_telemetryClient.IsEnabled()}");
            }
            else
            {
                LogMessage?.Invoke(this, "Application Insights telemetry id unavailable, disabling...");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }

            await LoadPluginsAsync();

            LogMessage?.Invoke(this, "Connecting to Discord... ");

            await _discordClient.ConnectAsync();
        }

        private void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                File.WriteAllText("config.json", JsonConvert.SerializeObject(_config, Formatting.Indented));
                _telemetryClient?.Flush();
            }
            catch { }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _telemetryClient?.TrackEvent(new EventTelemetry("Exit"));
            _telemetryClient?.Flush();
        }

        private async void _statusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_discordClient != null)
            {
                await UpdateStatusAsync();
            }
        }

        private void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            DSharpPlusLogMessage?.Invoke(this, e);
        }

        private async Task Client_Ready(ReadyEventArgs e)
        {
            LogMessage?.Invoke(this, "Ready!");
            await UpdateStatusAsync();
        }

        private async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            LogMessage?.Invoke(this, $"Guild {e.Guild.Name} is now available");
            _telemetryClient?.TrackEvent($"GuildAvailable", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });

            _config.SeenGuilds.Add(e.Guild.Id);
            if (_config.DisallowedGuilds.Contains(e.Guild.Id))
            {
                DiscordChannel c = await InternalTools.GetFirstChannelAsync(e.Guild);
                await c.SendMessageAsync(":middle_finger:");
                await e.Guild.LeaveAsync();
            }
        }

        private async Task Client_GuildCreated(GuildCreateEventArgs e)
        {
            LogMessage?.Invoke(this, $"Guild {e.Guild.Name} has been added!");
            _telemetryClient?.TrackEvent($"GuildJoin", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });

            if (_config.DisallowedGuilds.Contains(e.Guild.Id))
            {
                DiscordChannel c = await InternalTools.GetFirstChannelAsync(e.Guild);
                await c.SendMessageAsync(":middle_finger:");
                await e.Guild.LeaveAsync();
            }
            else
            {
                await InternalTools.SendWelcomeMessage(e.Guild, _config);
            }
        }

        private Task Client_GuildUnavailable(GuildDeleteEventArgs e)
        {
            LogMessage?.Invoke(this, $"Guild {e.Guild.Name} is now unavailable");
            _telemetryClient?.TrackEvent($"GuildUnavailable", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });
            return Task.CompletedTask;
        }

        private Task Client_GuildDeleted(GuildDeleteEventArgs e)
        {
            LogMessage?.Invoke(this, $"Guild {e.Guild.Name} has been removed (kick/ban)");
            _telemetryClient?.TrackEvent($"GuildLeave", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });
            return Task.CompletedTask;
        }

        private async Task UpdateStatusAsync()
        {
            try
            {
                StatusModel model = _config.StatusMessages.ElementAt(random.Next(_config.StatusMessages.Count));
                await _discordClient.UpdateStatusAsync(new DiscordActivity(model.Status, model.Type));
                LogMessage?.Invoke(this, $"Status set to {model.Type} {model.Status}");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"An {ex.GetType().Name} occured updating status.");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            await ProcessMessage(e.Message, e.Author, e.Channel);
        }

        private async Task Client_MessageUpdated(MessageUpdateEventArgs e)
        {
            if (e.Channel != null && e.Message != null && e.Author != null)
            {
                await ProcessMessage(e.Message, e.Author, e.Channel);
            }
        }

        private async Task LoadPluginsAsync()
        {
            LogMessage?.Invoke(this, "");
            List<string> dlls = Directory.EnumerateFiles("Plugins").Where(f => Path.GetExtension(f) == ".dll").ToList();
            dlls.Insert(0, Assembly.GetExecutingAssembly().Location);
            foreach (string str in _config.AdditionalPluginDirectories)
            {
                dlls.AddRange(Directory.EnumerateFiles(str).Where(f => Path.GetExtension(f) == ".dll"));
            }

            LogMessage?.Invoke(this, $"{(_commands.Any() ? "Reloading" : "Loading")} {dlls.Count()} plugins...");

            _commands.Clear();
            _assemblyCommands.Clear();
            _parseExtensions.Clear();

            foreach (string dllPath in dlls)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(dllPath);
                    if (assembly != null && assembly.DefinedTypes.Any(t => t.GetInterfaces()?.Contains(typeof(ICommandsAssembly)) == true))
                    {
                        LogMessage?.Invoke(this, $"Searching {Path.GetFileName(dllPath)} for commands.");
                        ICommandsAssembly asm = null;
                        List<DiscordCommand> asmCommands = new List<DiscordCommand>();
                        foreach (Type type in assembly.GetTypes())
                        {
                            if (type.GetInterfaces().Contains(typeof(ICommandsAssembly)))
                            {
                                asm = (ICommandsAssembly)Activator.CreateInstance(type);
                            }

                            if (type.GetInterfaces().Contains(typeof(IParseExtension)))
                            {
                                _parseExtensions.Add((IParseExtension)Activator.CreateInstance(type));
                            }

                            if (type.GetInterfaces().Contains(typeof(IParamConverter)))
                            {
                                Api.Tools.RegisterPatameterParseExtension((IParamConverter)Activator.CreateInstance(type));
                            }

                            if (type.IsSubclassOf(typeof(DiscordCommand)) && !type.IsAbstract)
                            {
                                DiscordCommand command = InstantateDiscordCommand(type);

                                if (command != null)
                                {
                                    asmCommands.Add(command);
                                }
                            }
                        }

                        if (asm == null)
                        {
                            LogMessage?.Invoke(this, $"Loaded {asmCommands.Count()} commands from {Path.GetFileName(dllPath)} without command assembly manifest. Categorised help will be unavailable for these commands.");
                            LogMessage?.Invoke(this, "Consider adding an \"ICommandAssembly\" class as soon as possible.");
                        }
                        else
                        {
                            if (asm is IBotStartup s)
                            {
                                await s.Startup(_discordClient);
                            }

                            LogMessage?.Invoke(this, $"Loaded {asmCommands.Count()} plugins from {Path.GetFileName(dllPath)}!");
                        }

                        _commands.AddRange(asmCommands);
                        if (asm != null)
                        {
                            _assemblyCommands.Add(asm, asmCommands);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke(this, $"An {ex.GetType().Name} occured while loading from {Path.GetFileName(dllPath)}\n{ex.Message}");
                }
            }

            LogMessage?.Invoke(this, "");
            LogMessage?.Invoke(this, $"Plugins loaded!");
            LogMessage?.Invoke(this, $"{_commands.Count} commands available: {string.Join(", ", _commands.Select(c => c.Name))}");
            LogMessage?.Invoke(this, $"{_parseExtensions.Count} parse extensions available: {string.Join(", ", _parseExtensions.Select(c => c.Name))}");
            LogMessage?.Invoke(this, "");
        }

        private async Task ProcessMessage(DiscordMessage message, DiscordUser author, DiscordChannel channel)
        {
            DateTimeOffset startTime = DateTimeOffset.Now;
            if (!string.IsNullOrWhiteSpace(message?.Content) && !author.IsBot && !author.IsCurrent && !_processedMessageIds.Contains(message.Id))
            {
                if (message.Content.ToLower().StartsWith(_config.Prefix.ToLower()))
                {
                    LogMessage?.Invoke(this, ($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Found command prefix, parsing..."));
                    IEnumerable<string> commandSegments = Strings.SplitCommandLine(message.Content.Substring(_config.Prefix.Length));

                    foreach (IParseExtension extenstion in _parseExtensions)
                    {
                        commandSegments = extenstion.Parse(commandSegments, channel);
                    }

                    if (commandSegments.Any())
                    {
                        string commandAlias = commandSegments.First().ToLower();
                        IEnumerable<DiscordCommand> foundCommands = _commands.Where(c => c.Aliases.Any(a => a.ToLower() == commandAlias));
                        DiscordCommand commandToRun = foundCommands.FirstOrDefault();
                        if (foundCommands.Count() == 1)
                        {
                            await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(1), commandAlias, commandToRun, startTime);
                        }
                        else if (commandSegments.Count() >= 2)
                        {
                            foundCommands = _assemblyCommands.FirstOrDefault(c => c.Key.Name.ToLowerInvariant() == commandAlias)
                                .Value?.Where(c => c.Aliases.Contains(commandSegments.ElementAt(1).ToLower()));

                            if (foundCommands != null && foundCommands.Count() == 1)
                            {
                                await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(2), commandAlias, foundCommands.First(), startTime);
                            }
                            else
                            {
                                if (commandToRun != null)
                                {
                                    await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(1), commandAlias, foundCommands.First(), startTime);
                                }
                                else
                                {
                                    LogMessage?.Invoke(this, ($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Unable to find command with alias \"{commandAlias}\"."));
                                    await InternalTools.SendTemporaryMessage(message, author, channel, $"```\r\n{commandAlias}: command not found!\r\n```");
                                    _telemetryClient?.TrackRequest(InternalTools.GetRequestTelemetry(author, channel, null, startTime, "404", false));
                                }
                            }
                        }
                        else if (commandToRun != null)
                        {
                            await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(1), commandAlias, foundCommands.First(), startTime);
                        }
                    }
                }
            }
        }

        private async Task ExecuteCommandAsync(DiscordMessage message, DiscordUser author, DiscordChannel channel, IEnumerable<string> commandSegments, string commandAlias, DiscordCommand command, DateTimeOffset start)
        {
            LogMessage?.Invoke(this, ($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Found {command.Name} command!"));
            _processedMessageIds.Add(message.Id);
            if (command.ArgumentCountPrecidate(commandSegments.Count()))
            {
                if (InternalTools.CheckPermissions(_discordClient, channel.Guild?.CurrentMember ?? _discordClient.CurrentUser, channel, command))
                {
                    if (InternalTools.CheckPermissions(_discordClient, author, channel, command))
                    {
                        LogMessage?.Invoke(this, ($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Running command \"{command.Name}\" asynchronously."));
                        new Task(async () => await RunCommandAsync(message, author, channel, commandSegments, commandAlias, command, start)).Start();
                    }
                    else
                    {
                        LogMessage?.Invoke(this, ($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Attempt to run command without correct user permissions."));
                        await InternalTools.SendTemporaryMessage(message, author, channel, $"Oi! You're not alowed to run that command! Fuck off!");
                        _telemetryClient?.TrackRequest(InternalTools.GetRequestTelemetry(author, channel, command, start, "401", false));
                    }
                }
                else
                {
                    LogMessage?.Invoke(this, ($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Attempt to run command without correct bot permissions."));
                    await InternalTools.SendTemporaryMessage(message, author, channel, $"Sorry! I don't have permission to run that command in this server! Contact an admin/mod for more info.");
                    _telemetryClient?.TrackRequest(InternalTools.GetRequestTelemetry(author, channel, command, start, "403", false));
                }
            }
            else
            {
                await HandleBadRequest(message, author, channel, commandSegments, commandAlias, command, start);
            }
        }

        private async Task RunCommandAsync(DiscordMessage message, DiscordUser author, DiscordChannel channel, IEnumerable<string> commandSegments, string commandAlias, DiscordCommand command, DateTimeOffset start)
        {
            bool success = true;
            try
            {
                using (WamBotContext dbContext = new WamBotContext())
                {
                    await channel.TriggerTypingAsync();
                    command = InstantateDiscordCommand(command.GetType());
                    _config.Happiness.TryGetValue(author.Id, out sbyte h);

                    CommandContext context = new CommandContext(commandSegments.ToArray(), message, _discordClient)
                    {
                        Happiness = h,
                        _logger = _loggerFactory.CreateLogger("Commands"),
                        UserData = await dbContext.Users.FindOrCreateAsync(author.Id, () => new UserData(author))
                    };

                    context.AdditionalData["botContext"] = this;

                    CommandResult result = await command.RunCommand(commandSegments.ToArray(), context);

                    LogMessage?.Invoke(this, ($"[{message.Channel.Guild.Name ?? message.Channel.Name}] \"{command.Name}\" returned ReturnType.{result.ReturnType}."));
                    if (result.ReturnType != ReturnType.None)
                    {
                        if (result.ReturnType == ReturnType.Text && result.ResultText.Length > 2000)
                        {
                            for (int i = 0; i < result.ResultText.Length; i += 1993)
                            {
                                string str = result.ResultText.Substring(i, Math.Min(1993, result.ResultText.Length - i));
                                if (result.ResultText.StartsWith("```") && !str.StartsWith("```"))
                                {
                                    str = "```" + str;
                                }
                                if (result.ResultText.EndsWith("```") && !str.EndsWith("```"))
                                {
                                    str = str + "```";
                                }

                                LogMessage?.Invoke(this, ($"Chunking message to {str.Length} chars"));
                                await channel.SendMessageAsync(str);

                                await Task.Delay(2000);
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(result.Attachment))
                            {
                                await channel.SendFileAsync(result.Attachment, result.ResultText, embed: result.ResultEmbed);
                            }
                            else if (result.Stream != null || result.ReturnType == ReturnType.File)
                            {
                                if (result.Stream.Length <= 8 * 1024 * 1024)
                                {
                                    await channel.SendFileAsync(result.Stream, result.FileName, result.ResultText, false, result.ResultEmbed);
                                }
                                else
                                {
                                    await channel.SendMessageAsync("This command has resulted in an attachment that is over 8MB in size and cannot be sent.");
                                }
                            }
                            else
                            {
                                await channel.SendMessageAsync(result.ResultText, embed: result.ResultEmbed);
                            }
                        }
                    }


                    RequestTelemetry request = InternalTools.GetRequestTelemetry(author, channel, command, start, success ? "200" : "500", success);
                    foreach (var pair in result.InsightsData)
                    {
                        request.Properties.Add(pair);
                    }

                    _telemetryClient?.TrackRequest(request);

                    _config.Happiness[author.Id] = (sbyte)((int)(context.Happiness).Clamp(sbyte.MinValue, sbyte.MaxValue));

                }
            }
            catch (BadArgumentsException)
            {
                await HandleBadRequest(message, author, channel, commandSegments, commandAlias, command, start);
            }
            catch (CommandException ex)
            {
                await channel.SendMessageAsync(ex.Message);
                _telemetryClient?.TrackRequest(InternalTools.GetRequestTelemetry(author, channel, command, start, "400", false));
            }
            catch (ArgumentOutOfRangeException)
            {
                await channel.SendMessageAsync("Hey there! That's gonna cause some issues, no thanks!!");
                _telemetryClient?.TrackRequest(InternalTools.GetRequestTelemetry(author, channel, command, start, "400", false));
            }
            catch (Exception ex)
            {
                success = false;
                ManageException(message, author, channel, ex, command);

                sbyte h = 0;
                _config.Happiness?.TryGetValue(author.Id, out h);

                _config.Happiness[author.Id] = (sbyte)((int)h - 1).Clamp(sbyte.MinValue, sbyte.MaxValue);
            }
            finally
            {
                if (command is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        private async Task HandleBadRequest(DiscordMessage message, DiscordUser author, DiscordChannel channel, IEnumerable<string> commandSegments, string commandAlias, DiscordCommand command, DateTimeOffset start)
        {
            LogMessage?.Invoke(this, $"[{message.Channel.Guild.Name ?? message.Channel.Name}] {command.Name} does not take {commandSegments.Count()} arguments.");
            _telemetryClient?.TrackRequest(InternalTools.GetRequestTelemetry(author, channel, command, start, "400", false));

            if (command.Usage != null)
            {
                await InternalTools.SendTemporaryMessage(message, author, channel, $"```\r\n{commandAlias} usage: {_config.Prefix}{commandAlias} [{command.Usage}]\r\n```");
            }
        }

        private DiscordCommand InstantateDiscordCommand(Type type)
        {
            DiscordCommand command = null;
            IEnumerable<CommandAttribute> attributes = type.GetCustomAttributes<CommandAttribute>(true);

            if (!attributes.Any())
            {
                command = (DiscordCommand)Activator.CreateInstance(type);
            }
            else
            {
                List<object> commandParameters = new List<object>();

                if (attributes.Any(a => a.GetType() == typeof(HttpClientAttribute)))
                {
                    commandParameters.Add(_httpClient);
                }

                command = (DiscordCommand)Activator.CreateInstance(type, commandParameters.ToArray());
            }

            return command;
        }


        private void ManageException(DiscordMessage message, DiscordUser author, DiscordChannel channel, Exception ex, DiscordCommand command)
        {
            LogMessage?.Invoke(this, $"\n --- Something's fucked up! --- \n{ex.ToString()}\n");
            _telemetryClient?.TrackException(ex, new Dictionary<string, string> { { "command", command.GetType().Name } });

            if (!(ex is TaskCanceledException) && !(ex is OperationCanceledException))
            {

                new Task(async () =>
                {
                    try
                    {
                        DiscordMessage msg = await channel.SendMessageAsync(
                            $"Something's gone very wrong executing that command, and an {ex.GetType().Name} occured." +
                            "\r\nThis error has been reported, and should be fixed soon:tm:!" +
                            $"\r\nThis message will be deleted in 10 seconds.");

                        await Task.Delay(10_000);
                        await msg.DeleteAsync();
                    }
                    catch { }
                }).Start();
            }
        }

        private Task Client_SocketErrored(SocketErrorEventArgs e)
        {
            _telemetryClient?.TrackException(e.Exception);
            return Task.CompletedTask;
        }

        private Task Client_discordClientErrored(ClientErrorEventArgs e)
        {
            _telemetryClient?.TrackException(e.Exception);
            return Task.CompletedTask;
        }
    }
}
