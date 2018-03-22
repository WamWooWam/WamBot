using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MarkovChains;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WamBotRewrite.Api;
using WamBotRewrite.Api.Converters;
using WamBotRewrite.Api.Pipes;
using WamBotRewrite.Commands;
using WamBotRewrite.Data;
using WamWooWam.Core;

namespace WamBotRewrite
{
    class Program
    {
        internal static RestApplication Application { get; private set; }
        internal static List<CommandRunner> Commands { get; private set; } = new List<CommandRunner>();
        internal static IEnumerable<IGrouping<CommandCategory, CommandRunner>> CommandCategories => Commands.GroupBy(c => c.Category);
        internal static List<IParamConverter> ParamConverters { get; private set; } = new List<IParamConverter>();
        internal static List<KeyValuePair<ActivityType, string>> Statuses { get; private set; } = new List<KeyValuePair<ActivityType, string>>();
        internal static bool RunningOutOfProcess { get; private set; } = false;

        internal static DiscordSocketClient Client => _client;
        internal static DiscordRestClient RestClient => _restClient;
        internal static Config Config => _config;

        static List<ulong> _processedMessageIds = new List<ulong>();
        static DiscordSocketClient _client;
        static DiscordRestClient _restClient;
        static TelemetryClient _telemetryClient;
        static Config _config;
        static Random _random = new Random();

#pragma warning disable CS4014 // Sometimes this is intended behaviour

        static async Task Main(string[] rawArgs)
        {
            if (rawArgs.Any())
            {
                Dictionary<string, string> args = new Dictionary<string, string>();
                foreach (string str in rawArgs)
                {
                    string arg = str.TrimStart('-');
                    args.Add(arg.Substring(0, arg.IndexOf('=')), arg.Substring(arg.IndexOf('=') + 1));
                }
                Console.WriteLine(JsonConvert.SerializeObject(args));

                if (args.TryGetValue("type", out string processType))
                {
                    if (processType == "bot")
                    {
                        await NormalMain();
                    }
                    else if (processType == "command")
                    {
                        RunningOutOfProcess = true;
                        string type = args["t"];
                        string method = args["m"];
                        string pipeId = args["p"];
                        bool requiresDiscord = bool.Parse(args["r"]);

                        Type t = Type.GetType(type);
                        MethodInfo info = t.GetMethod(method, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);

                        using (var pipe = new NamedPipeClientStream(".", pipeId, PipeDirection.InOut, PipeOptions.Asynchronous))
                        {
                            await pipe.ConnectAsync();

                            using (var _pipeReader = new StreamReader(pipe))
                            using (var _pipeWriter = new StreamWriter(pipe) { AutoFlush = true })
                            {
                                string configString = await _pipeReader.ReadLineAsync();
                                _config = JsonConvert.DeserializeObject<Config>(configString);

                                string contextString = await _pipeReader.ReadLineAsync();
                                var pipeContext = JsonConvert.DeserializeObject<PipeContext>(contextString);
                                var pipeCommandContext = new PipeCommandContext(pipeContext);

                                _restClient = new DiscordRestClient(new DiscordRestConfig()
                                {
                                    LogLevel = LogSeverity.Debug
                                });
                                _restClient.Log += DiscordClient_Log;

                                _restClient.LoginAsync(TokenType.Bot, _config.Token);

                                if (requiresDiscord)
                                {
                                    await InitialiseDiscordClients();
                                }

                                ParamConverters.AddRange(new IParamConverter[] { new DiscordChannelParse(), new DiscordUserParse(), new DiscordRoleParse(), new DiscordGuildParse(), new ByteArrayConverter() });

                                try
                                {
                                    CommandRunner runner = new CommandRunner(info, (CommandCategory)Activator.CreateInstance(t));
                                    await runner.Run(pipeContext.Arguments, pipeCommandContext);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                    await _pipeWriter.WriteLineAsync(JsonConvert.SerializeObject(ex));
                                }
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                RunDefaultBotProcess();
            }
        }

#pragma warning restore CS4014

        private static void RunDefaultBotProcess()
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    Arguments = $@"--type=bot"
                }
            };

            process.Start();
            process.WaitForExit();
        }

        private static async Task NormalMain()
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

            foreach (string statusString in Config.StatusMessages)
            {
                if (Enum.TryParse<ActivityType>(statusString.First().ToString(), out var s))
                {
                    string t = statusString.Remove(0, 1);
                    Statuses.Add(new KeyValuePair<ActivityType, string>(s, t));
                }
                else
                {
                    Statuses.Add(new KeyValuePair<ActivityType, string>(ActivityType.Playing, statusString));
                }
            }

            bool connected = false;
            int failures = 0;
            while (!connected)
            {
                failures += 1;

                try
                {
                    await InitialiseDiscordClients();

                    _restClient = new DiscordRestClient(new DiscordRestConfig()
                    {
                        LogLevel = LogSeverity.Debug
                    });
                    _restClient.Log += DiscordClient_Log;

                    await _restClient.LoginAsync(TokenType.Bot, _config.Token);
                    await _client.StartAsync();

                    connected = true;
                    break;
                }
                catch
                {
                    Console.WriteLine($"Failed to connect to Discord. Retrying in {5 * failures} seconds");
                    await Task.Delay(5000 * failures);
                }
            }

            _telemetryClient?.Flush();
            if (_config.ApplicationInsightsKey != Guid.Empty)
            {
                TelemetryConfiguration.Active.InstrumentationKey = _config.ApplicationInsightsKey.ToString();
                _telemetryClient = new TelemetryClient();
                _telemetryClient.TrackEvent(new EventTelemetry("Startup"));

                Console.WriteLine($"Application Insights telemetry configured! {_telemetryClient.IsEnabled()}");
            }
            else
            {
                Console.WriteLine("Application Insights telemetry id unavailable, disabling...");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }

            Application = await _client.GetApplicationInfoAsync();

            _client.MessageReceived += ProcessComand_MessageRecieve;
            _client.Ready += _client_Ready;

            _client.GuildAvailable += _client_GuildAvailable;
            _client.GuildUnavailable += _client_GuildUnavailable;
            _client.JoinedGuild += _client_JoinedGuild;
            _client.LeftGuild += _client_LeftGuild;

            Commands.AddRange(new StockCommands().GetCommands());
            Commands.AddRange(new APICommands().GetCommands());
            Commands.AddRange(new MusicCommands().GetCommands());
            Commands.AddRange(new WamCashCommands().GetCommands());
            Commands.AddRange(new CryptoCommands().GetCommands());
            Commands.AddRange(new MarkovCommands().GetCommands());
            Commands.AddRange(new ImageCommands().GetCommands());
            Commands.AddRange(new ModerationCommands().GetCommands());
            ParamConverters.AddRange(
                new IParamConverter[] {
                    new DiscordChannelParse(),
                    new DiscordUserParse(),
                    new DiscordRoleParse(),
                    new DiscordGuildParse(),
                    new ByteArrayConverter(),
                    new ImageConverter(),
                    new ColourConverter(),
                    new FontConverter()
                });

            Console.WriteLine($"{Commands.Count} commands and {ParamConverters.Count} converters ready and waiting!");


            using (WamBotContext ctx = new WamBotContext())
            {
                await ctx.Database.MigrateAsync();
            }

            var saveTimer = Tools.CreateTimer(TimeSpan.FromMinutes(5), (s, e) =>
            {
                File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));
                File.WriteAllText("markov.json", JsonConvert.SerializeObject(MarkovCommands.MarkovList));
            });

            var happinessTickdown = Tools.CreateTimer(TimeSpan.FromHours(12), HappinessTickdown);

            await Task.Delay(-1);
        }

        private static async Task _client_Ready()
        {
            await SetStatusAsync();

            var statusUpdateTimer = Tools.CreateTimer(TimeSpan.FromMinutes(15), async (s, e) =>
            {
                await SetStatusAsync();
            });
        }

        private static async Task _client_GuildAvailable(SocketGuild arg)
        {
            Console.WriteLine($"Guild {arg.Name} is now available.");

            if (_config.DisallowedGuilds.Contains(arg.Id))
            {
                Console.WriteLine($"Leaving disallowed guild {arg.Name}.");

                var c = Tools.GetFirstChannel(arg);
                await c.SendMessageAsync(":middle_finger:");
                await arg.LeaveAsync();
            }
            else if (!_config.SeenGuilds.Contains(arg.Id))
            {
                await Tools.SendWelcomeMessage(arg);

                File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));
            }
        }

        private static Task _client_GuildUnavailable(SocketGuild arg)
        {
            Console.WriteLine($"Guild {arg.Name} is now unavailable");
            return Task.CompletedTask;
        }

        private static async Task _client_JoinedGuild(SocketGuild arg)
        {
            Console.WriteLine($"Joined guild {arg.Name}!");

            if (_config.DisallowedGuilds.Contains(arg.Id))
            {
                Console.WriteLine($"Leaving disallowed guild {arg.Name}.");

                var c = Tools.GetFirstChannel(arg);
                await c.SendMessageAsync(":middle_finger:");
                await arg.LeaveAsync();
            }
            else if (!_config.SeenGuilds.Contains(arg.Id))
            {
                await Tools.SendWelcomeMessage(arg);

                File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));
            }
        }

        private static Task _client_LeftGuild(SocketGuild arg)
        {
            Console.WriteLine($"Guild {arg.Name} has been removed (kick/ban)");
            return Task.CompletedTask;
        }

        public static void HappinessTickdown(object sender, ElapsedEventArgs e)
        {
            using (WamBotContext ctx = new WamBotContext())
            {
                foreach (User data in ctx.Users)
                {
                    if (Tools.GetHappinessLevel(data.Happiness) == HappinessLevel.Hate)
                    {
                        data.Happiness = (sbyte)(((int)data.Happiness) + 1)
                            .Clamp(sbyte.MinValue, sbyte.MaxValue);
                    }
                    else
                    {
                        data.Happiness = (sbyte)(((int)data.Happiness) - 1)
                            .Clamp(sbyte.MinValue, sbyte.MaxValue);
                    }
                }
            }
        }

        private static async Task SetStatusAsync()
        {
            var st = Statuses.ElementAt(_random.Next(Statuses.Count));
            Console.WriteLine($"Setting status to: {st.Key} {st.Value}");
            if (st.Key == ActivityType.Streaming)
            {
                string str = st.Value.Substring(0, st.Value.IndexOf("|"));
                string link = st.Value.Substring(st.Value.IndexOf("|") + 1);
                await _client.SetGameAsync(str, link, st.Key);
            }
            else
            {
                await _client.SetGameAsync(st.Value, type: st.Key);
            }
        }

        private static async Task InitialiseDiscordClients()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                LogLevel = LogSeverity.Debug,
                MessageCacheSize = 200
            });

            _client.Log += DiscordClient_Log;
            await _client.LoginAsync(TokenType.Bot, _config.Token);
        }

        private static async Task ProcessComand_MessageRecieve(SocketMessage arg)
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
                if (!command.RequiresGuild || channel is IGuildChannel)
                {
                    if (Tools.CheckPermissions(Client, (message.Channel is IGuildChannel c ? (IUser)(await c.Guild?.GetCurrentUserAsync()) : (IUser)Client.CurrentUser), channel, command))
                    {
                        if (Tools.CheckPermissions(Client, author, channel, command))
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
                }
                else
                {
                    Console.WriteLine($"[{(message.Channel is IGuildChannel ch ? ch.Guild?.Name : message.Channel.Name)}] Attempt to run command requiring guild within non-guild channel.");
                    await Tools.SendTemporaryMessage(message, channel, "This command requires a server to run! Sorry!");
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

                using (WamBotContext db = new WamBotContext())
                {
                    CommandContext context = new CommandContext(commandSegments.ToArray(), message, _client, db);

                    context.UserData = await db.Users.GetOrCreateAsync(db, (long)author.Id, () => new User(author));
                    if (channel is IGuildChannel gc)
                    {
                        context.GuildData = await db.Guilds.GetOrCreateAsync(db, (long)gc.GuildId, () => new Guild(gc.Guild));
                        context.ChannelData = await db.Channels.GetOrCreateAsync(db, (long)channel.Id, () => new Channel(gc));
                    }

                    string[] cmdsegarr = commandSegments.ToArray();
                    await command.Run(cmdsegarr, context);

                    context.UserData.Happiness += 1;
                    context.UserData.CommandsRun += 1;

                    await db.SaveChangesAsync();

                    RequestTelemetry request = Tools.GetRequestTelemetry(author, channel, command, start, "200", true);
                    _telemetryClient?.TrackRequest(request);
                }
            }
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
                ManageException(message, channel, ex, command);
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
            if (!(ex is TaskCanceledException) && !(ex is OperationCanceledException))
            {
                Console.WriteLine($"\n --- Something's fucked up! --- \n{ex.ToString()}\n");
                _telemetryClient?.TrackException(ex, new Dictionary<string, string> { { "command", command.GetType().Name } });

                ex = ex.InnerException ?? ex;

                new Task(async () =>
                {
                    try
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                            .WithAuthor($"Error - WamBot {Assembly.GetEntryAssembly().GetName().Version.ToString(3)}", Application?.IconUrl)
                            .WithDescription($"Something's gone very wrong executing that command, and an {ex.GetType().Name} occured.")
                            .WithFooter("This message will be deleted in 10 seconds")
                            .WithTimestamp(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10))
                            .WithColor(255, 0, 0);
                        builder.AddField("Message", $"```{ex.Message.Truncate(1016)}```");
#if DEBUG
                        builder.AddField("Stack Trace", $"```{ex.StackTrace.Truncate(1016)}```");
#endif

                        IUserMessage msg = await channel.SendMessageAsync("", embed: builder.Build());

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
