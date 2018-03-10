using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using WamBotRewrite.Api;
using WamBotRewrite.Api.Converters;
using WamBotRewrite.Commands;
using WamBotRewrite.Data;
using WamWooWam.Core;

namespace WamBotRewrite
{
    class Program
    {
        internal static List<CommandRunner> Commands { get; private set; } = new List<CommandRunner>();
        internal static IEnumerable<IGrouping<CommandCategory, CommandRunner>> CommandCategories => Commands.GroupBy(c => c.Category);
        internal static List<IParamConverter> ParamConverters { get; private set; } = new List<IParamConverter>();
        internal static DiscordSocketClient Client => _client;
        internal static DiscordRestClient RestClient => _restClient;
        internal static RestApplication Application { get; private set; }
        internal static Config Config => _config;


        static List<ulong> _processedMessageIds = new List<ulong>();
        static DiscordSocketClient _client;
        static DiscordRestClient _restClient;
        static TelemetryClient _telemetryClient;
        static Config _config;

        static async Task Main(string[] args)
        {
            if (File.Exists("config.json"))
            {
                try
                {
                    string str = File.ReadAllText("config.json");
                    _config = JsonConvert.DeserializeObject<Config>(str);

                    if (_config == null)
                    {
                        Console.WriteLine("An error has been detected with your configuration file that needs correcting. Please repair your configuration file before re-running WamBot.");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error has been detected with your configuration file that needs correcting. An {ex.GetType().Name} occured. Please repair your configuration file before re-running WamBot.");
                    Console.WriteLine(ex);
                }
            }
            else
            {
                _config = new Config();

                Console.WriteLine(" ----  Welcome to WamBot  ---- ");
                Console.WriteLine(" Hello! WamBot needs some initial setup before it can run, this won't take long!");

                Console.WriteLine(" -- Tokens -- ");
                Console.Write(" Discord Bot Token: ");
                _config.Token = Tools.ProtectedReadLine();
                Console.Write(" Application Insights Token (leave empty to disable): ");
                string insights = Tools.ProtectedReadLine();
                if (Guid.TryParse(insights, out var guid))
                {
                    _config.ApplicationInsightsKey = guid;
                }

                Console.WriteLine(" -- Settings -- ");
                Console.Write(" Bot Prefix: ");
                _config.Prefix = Console.ReadLine();

                Console.WriteLine();
                Console.WriteLine(" And we're done here! Let's get going!");

                File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 200
            });

            _client.Log += DiscordClient_Log;
            await _client.LoginAsync(TokenType.Bot, _config.Token);

            _restClient = new DiscordRestClient(new DiscordRestConfig()
            {
                LogLevel = LogSeverity.Debug
            });

            await _restClient.LoginAsync(TokenType.Bot, _config.Token);

            _telemetryClient?.Flush();
            if (_config.ApplicationInsightsKey != Guid.Empty)
            {
                TelemetryConfiguration.Active.InstrumentationKey = _config.ApplicationInsightsKey.ToString();
                _telemetryClient = new TelemetryClient();
                _telemetryClient.TrackEvent(new EventTelemetry("Startup"));

                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                Console.WriteLine($"Application Insights telemetry configured! {_telemetryClient.IsEnabled()}");
            }
            else
            {
                Console.WriteLine("Application Insights telemetry id unavailable, disabling...");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }

            await _client.StartAsync();
            Application = await _client.GetApplicationInfoAsync();

            Commands.AddRange(new StockCommands().GetCommands());
            Commands.AddRange(new APICommands().GetCommands());
            Commands.AddRange(new MusicCommands().GetCommands());
            ParamConverters.AddRange(new IParamConverter[] { new DiscordChannelParse(), new DiscordUserParse(), new DiscordRoleParse(), new DiscordGuildParse() });

            Console.WriteLine($"{Commands.Count} commands and {ParamConverters.Count} converters ready and waiting!");

            _client.MessageReceived += DiscordClient_MessageReceived;

            await Task.Delay(-1);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {

        }

        private static async Task DiscordClient_MessageReceived(SocketMessage arg)
        {
            await ProcessMessage(arg, arg.Author, arg.Channel);
        }

        private static async Task ProcessMessage(SocketMessage message, SocketUser author, ISocketMessageChannel channel)
        {
            try
            {
                DateTimeOffset startTime = DateTimeOffset.Now;
                if (!string.IsNullOrWhiteSpace(message?.Content) && !author.IsBot && !author.IsCurrent() && !_processedMessageIds.Contains(message.Id))
                {
                    if (message.Content.ToLower().StartsWith(_config.Prefix.ToLower()))
                    {
                        Console.WriteLine($"[{(message.Channel is IGuildChannel g ? g.Guild?.Name : message.Channel.Name)}] Found command prefix, parsing...");
                        IEnumerable<string> commandSegments = Strings.SplitCommandLine(message.Content.Substring(_config.Prefix.Length));

                        //foreach (IParseExtension extenstion in _parseExtensions)
                        //{
                        //    commandSegments = extenstion.Parse(commandSegments, channel);
                        //}

                        if (commandSegments.Any())
                        {
                            string commandAlias = commandSegments.First().ToLower();
                            IEnumerable<CommandRunner> foundCommands = Commands.Where(c => c?.Aliases?.Any(a => a.ToLower() == commandAlias) == true);
                            CommandRunner commandToRun = foundCommands.FirstOrDefault();
                            if (commandToRun != null)
                            {
                                if (foundCommands.Count() == 1)
                                {
                                    await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(1), commandAlias, commandToRun, startTime);
                                }
                                else if (commandSegments.Count() >= 2)
                                {
                                    foundCommands = CommandCategories
                                        .FirstOrDefault(c => c.Key.Name.ToLowerInvariant() == commandAlias)
                                        .Where(c => c.Aliases.Contains(commandSegments.ElementAt(1).ToLower()));

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
                                            Console.WriteLine($"[{(message.Channel is IGuildChannel c ? c.Guild?.Name : message.Channel.Name)}] Unable to find command with alias \"{commandAlias}\".");
                                            await Tools.SendTemporaryMessage(message, channel, $"```\r\n{commandAlias}: command not found!\r\n```");
                                            _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(author, channel, null, startTime, "404", false));
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task ExecuteCommandAsync(SocketMessage message, SocketUser author, ISocketMessageChannel channel, IEnumerable<string> commandSegments, string commandAlias, CommandRunner command, DateTimeOffset start)
        {
            try
            {
                Console.WriteLine($"[{(message.Channel is IGuildChannel g ? g.Guild?.Name : message.Channel.Name)}] Found {command.Name} command!");
                _processedMessageIds.Add(message.Id);
                //if (command.ArgumentCountPrecidate(commandSegments.Count()))
                //{
                if (await Tools.CheckPermissions(Client, (message.Channel is IGuildChannel c ? (IUser)(await c.Guild?.GetCurrentUserAsync()) : (IUser)Client.CurrentUser), channel, command))
                {
                    if (await Tools.CheckPermissions(Client, author, channel, command))
                    {
                        Console.WriteLine($"[{(message.Channel is IGuildChannel ch ? ch.Guild?.Name : message.Channel.Name)}] Running command \"{command.Name}\" asynchronously.");
                        new Task(async () => await RunCommandAsync(message, author, channel, commandSegments, commandAlias, command, start)).Start();
                    }
                    else
                    {
                        Console.WriteLine($"[{(message.Channel is IGuildChannel ch ? ch.Guild?.Name : message.Channel.Name)}] Attempt to run command without correct user permissions.");
                        await Tools.SendTemporaryMessage(message, channel, $"Oi! You're not allowed to run that command! Fuck off!");
                        _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(author, channel, command, start, "401", false));
                    }
                }
                else
                {
                    Console.WriteLine($"[{(message.Channel is IGuildChannel ch ? ch.Guild?.Name : message.Channel.Name)}] Attempt to run command without correct bot permissions.");
                    await Tools.SendTemporaryMessage(message, channel, $"Sorry! I don't have permission to run that command in this server! Contact an admin/mod for more info.");
                    _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(author, channel, command, start, "403", false));
                }
                //}
                //else
                //{
                //    await HandleBadRequest(message, author, channel, commandSegments, commandAlias, command, start);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task RunCommandAsync(SocketMessage message, SocketUser author, ISocketMessageChannel channel, IEnumerable<string> commandSegments, string commandAlias, CommandRunner command, DateTimeOffset start)
        {
            try
            {
                await channel.TriggerTypingAsync();
                //_config.Happiness.TryGetValue(author.Id, out sbyte h);

                CommandContext context = new CommandContext(commandSegments.ToArray(), message, _client);

                string[] cmdsegarr = commandSegments.ToArray();
                await command.Run(cmdsegarr, context);

                RequestTelemetry request = Tools.GetRequestTelemetry(author, channel, command, start, "200", true);

                _telemetryClient?.TrackRequest(request);

                //_config.Happiness[author.Id] = (sbyte)((int)(context.Happiness).Clamp(sbyte.MinValue, sbyte.MaxValue));
            }
            //catch (BadArgumentsException ex)
            //{
            //    Console.WriteLine(ex);
            //    await HandleBadRequest(message, author, channel, commandSegments, commandAlias, command, start);
            //}
            catch (CommandException ex)
            {
                Console.WriteLine(ex);
                await channel.SendMessageAsync(ex.Message);
                _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(author, channel, command, start, "400", false));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine(ex);
                await channel.SendMessageAsync("Hey there! That's gonna cause some issues, no thanks!!");
                _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(author, channel, command, start, "400", false));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //success = false;
                ManageException(message, channel, ex, command);

                //sbyte h = 0;
                //_config.Happiness?.TryGetValue(author.Id, out h);
                //_config.Happiness[author.Id] = (sbyte)((int)h - 1).Clamp(sbyte.MinValue, sbyte.MaxValue);
            }
            finally
            {
                if (command is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        private static async Task HandleBadRequest(SocketMessage message, SocketUser author, ISocketMessageChannel channel, IEnumerable<string> commandSegments, string commandAlias, CommandRunner command, DateTimeOffset start)
        {
            Console.WriteLine($"[{(message.Channel is IGuildChannel g ? g.Guild?.Name : message.Channel.Name)}] {command.Name} does not take {commandSegments.Count()} arguments.");
            _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(author, channel, command, start, "400", false));

            if (command.Usage != null)
            {
                await Tools.SendTemporaryMessage(message, channel, $"```\r\n{commandAlias} usage: {_config.Prefix}{commandAlias} [{command.Usage}]\r\n```");
            }
        }

        private static void ManageException(SocketMessage message, ISocketMessageChannel channel, Exception ex, CommandRunner command)
        {
            Console.WriteLine($"\n --- Something's fucked up! --- \n{ex.ToString()}\n");
            _telemetryClient?.TrackException(ex, new Dictionary<string, string> { { "command", command.GetType().Name } });

            if (!(ex is TaskCanceledException) && !(ex is OperationCanceledException))
            {

                new Task(async () =>
                {
                    try
                    {
                        IUserMessage msg = await channel.SendMessageAsync(
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

        private static Task DiscordClient_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
