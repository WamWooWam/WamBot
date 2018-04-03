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
#if UI
using WamBotRewrite.UI;
#endif
using WamWooWam.Core;

using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;
using SixLabors.ImageSharp.PixelFormats;

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
        internal static Random Random => _random;
        internal static TelemetryClient TelemetryClient => _telemetryClient;
        internal static Color AccentColour { get; private set; }

        static List<ulong> _processedMessageIds = new List<ulong>();
        static DiscordSocketClient _client;
        static DiscordRestClient _restClient;
        static TelemetryClient _telemetryClient;
        static Config _config;
        static Random _random = new Random();


#pragma warning disable CS4014 // Sometimes this is intended behaviour

        [STAThread]
        static void Main(string[] args)
        {
#if UI
            UI.App.Main();
#else
            MainAsync(args).GetAwaiter().GetResult();
#endif
        }

        public static async Task MainAsync(string[] rawArgs)
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

        internal static async Task LogMessage(string source, string text)
        {
#if UI
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ((MainWindow)App.Current.MainWindow).BotLog.AppendText($"[{DateTime.Now}][{source}]: {text}\r\n");
            });
#else
            Console.WriteLine($"[{DateTime.Now}][{source}]: {text}");
#endif
        }

        internal static async Task LogMessage(IMessage message, string text)
        {
            await LogMessage(message.Channel.Name, text);
        }

        internal static async Task LogMessage(CommandContext ctx, string text) => await LogMessage($"{ctx.Command.Name} - {ctx.Channel.Name}", text);

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

        internal static async Task NormalMain()
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
                    await LogMessage("STARTUP", $"Failed to connect to Discord. Retrying in {5 * failures} seconds");
                    await Task.Delay(5000 * failures);
                }
            }

            _telemetryClient?.Flush();
            if (_config.ApplicationInsightsKey != Guid.Empty)
            {
                TelemetryConfiguration.Active.InstrumentationKey = _config.ApplicationInsightsKey.ToString();
                _telemetryClient = new TelemetryClient();
                _telemetryClient.TrackEvent(new EventTelemetry("Startup"));

                await LogMessage("STARTUP", $"Application Insights telemetry configured! {_telemetryClient.IsEnabled()}");
            }
            else
            {
                await LogMessage("STARTUP", "Application Insights telemetry ID unavailable, disabling...");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }

            if (_config.TwitterCredentials != new Tweetinvi.Models.TwitterCredentials())
            {
                Tweetinvi.Auth.SetCredentials(Config.TwitterCredentials);
                await LogMessage("STARTUP", "Twitter credentials configured!");
            }
            else
            {
                await LogMessage("STARTUP", "Twitter credentials unavailable. Disabling!");
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
            Commands.AddRange(new TwitterCommands().GetCommands());
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

            await LogMessage("STARTUP", $"{Commands.Count} commands and {ParamConverters.Count} converters ready and waiting!");

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


            File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));

            await Task.Delay(-1);
        }

        private static async Task _client_Ready()
        {
            await SetStatusAsync();

            await LogMessage("STARTUP", "Configuring accent colour...");

            using (var str = await CommandCategory.HttpClient.GetStreamAsync(_client.CurrentUser.GetAvatarUrl()))
            using (var img = Image.Load(str))
            {
                Rgba32[] buffer = new Rgba32[1];
                img.Mutate(m => m.Resize(1, 1));
                img.SavePixelData(buffer);

                Rgba32 col = buffer[0];
                AccentColour = new Color(col.R, col.G, col.B);
            }

            await LogMessage("STARTUP", $"Accent colour set to #{AccentColour.RawValue.ToString("X2")}");

            var statusUpdateTimer = Tools.CreateTimer(TimeSpan.FromMinutes(15), async (s, e) =>
            {
                await SetStatusAsync();
            });
        }

        private static async Task _client_GuildAvailable(SocketGuild arg)
        {
            await LogMessage("GUILD", $"Guild {arg.Name} is now available.");

            if (_config.DisallowedGuilds.Contains(arg.Id))
            {
                await LogMessage("GUILD", $"Leaving disallowed guild {arg.Name}.");

                var c = Tools.GetFirstChannel(arg);
                await c.SendMessageAsync(":middle_finger:");
                await arg.LeaveAsync();
            }
            else if (!_config.SeenGuilds.Contains(arg.Id))
            {
                await Tools.SendWelcomeMessage(arg);
                File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));
            }

            using (WamBotContext ctx = new WamBotContext())
            {
                await LogMessage("DATABASE", $"Ensuring guild for {arg}");
                var g = await ctx.Guilds.GetOrCreateAsync(ctx, (long)arg.Id, () => new Guild(arg));
                g.Name = arg.Name;

                //await arg.DownloadUsersAsync();
                foreach (var user in arg.Users)
                {
                    await LogMessage("DATABASE", $"Ensuring user for {user}");
                    var u = await ctx.Users.GetOrCreateAsync(ctx, (long)user.Id, () => new User(user));

                    u.Username = user.Username;
                    u.Discriminator = user.Discriminator;
                }

                foreach (var channel in arg.Channels)
                {
                    await LogMessage("DATABASE", $"Ensuring channel for {channel}");
                    var c = await ctx.Channels.GetOrCreateAsync(ctx, (long)channel.Id, () => new Channel(channel));

                    c.Name = channel.Name;
                    c.Type = channel is ITextChannel ? ChannelType.Text : ChannelType.Voice;
                }

                await ctx.SaveChangesAsync();
            }
        }

        private static async Task _client_GuildUnavailable(SocketGuild arg)
        {
            await LogMessage("GUILD", $"Guild {arg.Name} is now unavailable");
        }

        private static async Task _client_JoinedGuild(SocketGuild arg)
        {
            await LogMessage("GUILD", $"Joined guild {arg.Name}!");

            if (_config.DisallowedGuilds.Contains(arg.Id))
            {
                await LogMessage("GUILD", $"Leaving disallowed guild {arg.Name}.");

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

        private static async Task _client_LeftGuild(SocketGuild arg)
        {
            await LogMessage("GUILD", $"Guild {arg.Name} has been removed (kick/ban)");
        }

        public static void HappinessTickdown(object sender, ElapsedEventArgs e)
        {
            using (WamBotContext ctx = new WamBotContext())
            {
                foreach (User data in ctx.Users.Where(u => u.CommandsRun > 0))
                {
                    if (Tools.GetHappinessLevel(data.Happiness) == HappinessLevel.Hate)
                    {
                        data.Happiness = (sbyte)(((int)data.Happiness) + 2)
                            .Clamp(sbyte.MinValue, sbyte.MaxValue);
                    }
                    else
                    {
                        data.Happiness = (sbyte)(((int)data.Happiness) - 2)
                            .Clamp(sbyte.MinValue, sbyte.MaxValue);
                    }
                }

                ctx.SaveChanges();
            }
        }

        private static async Task SetStatusAsync()
        {
            var st = Statuses.ElementAt(_random.Next(Statuses.Count));
            await LogMessage("STATUS", $"Setting status to: {st.Key} {st.Value}");
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

        private static async Task ProcessMessage(IMessage message, IUser author, ISocketMessageChannel channel)
        {
            try
            {
                DateTimeOffset startTime = DateTimeOffset.Now;
                if (!string.IsNullOrWhiteSpace(message?.Content) && !author.IsBot && !author.IsCurrent() && !_processedMessageIds.Contains(message.Id))
                {
                    if (message.Content.ToLower().StartsWith(_config.Prefix.ToLower()))
                    {
                        await LogMessage(message, $"Found command prefix, parsing...");
                        IEnumerable<string> commandSegments = Strings.SplitCommandLine(message.Content.Substring(_config.Prefix.Length));

                        if (commandSegments.Any())
                        {
                            string commandAlias = commandSegments.First().ToLower();
                            IEnumerable<CommandRunner> foundCommands = Commands.Where(c => c?.Aliases?.Any(a => a.ToLower() == commandAlias) == true);
                            CommandRunner commandToRun = foundCommands.FirstOrDefault();
                            if (commandToRun != null)
                            {
                                if (foundCommands.Count() == 1)
                                {
                                    ExecuteCommand(message, commandSegments.Skip(1), commandToRun, startTime);
                                }
                                else if (commandSegments.Count() >= 2)
                                {
                                    foundCommands = CommandCategories
                                        .FirstOrDefault(c => c.Key.Name.ToLowerInvariant() == commandAlias)
                                        .Where(c => c.Aliases.Contains(commandSegments.ElementAt(1).ToLower()));

                                    if (foundCommands != null && foundCommands.Count() == 1)
                                    {
                                        ExecuteCommand(message, commandSegments.Skip(2), foundCommands.First(), startTime);
                                    }
                                    else
                                    {
                                        if (commandToRun != null)
                                        {
                                            ExecuteCommand(message, commandSegments.Skip(1), foundCommands.First(), startTime);
                                        }
                                        else
                                        {
                                            await LogMessage(message, "Unable to find command with alias \"{commandAlias}\".");
                                            await Tools.SendTemporaryMessage(message, channel, $"```\r\n{commandAlias}: command not found!\r\n```");
                                            _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(author, channel, null, startTime, "404", false));
                                        }
                                    }
                                }
                                else if (commandToRun != null)
                                {
                                    ExecuteCommand(message, commandSegments.Skip(1), foundCommands.First(), startTime);
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

        private static void ExecuteCommand(IMessage message, IEnumerable<string> commandSegments, CommandRunner command, DateTimeOffset start)
        {
            if (DateTime.Now.IsAprilFools() && commandSegments.Count() == 0 && Random.NextDouble() > 0.5)
            {
                var c = Commands.Where(co => co._method.GetParameters().Count() == 1).ToArray();
                command = c.ElementAt(Random.Next(c.Count()));
            }

            var r = new CommandRequest(message, commandSegments.ToList(), command, start);

            new Task(async () =>
            {
                using (WamBotContext db = new WamBotContext())
                {
                    try
                    {
                        await LogMessage(r.Message, $"Found {r.Command.Name} command!");
                        _processedMessageIds.Add(r.Message.Id);

                        if (!r.Command.RequiresGuild || r.Channel is IGuildChannel)
                        {
                            if (Tools.CheckPermissions(Client, (r.Message.Channel is IGuildChannel c ? (IUser)(await c.Guild?.GetCurrentUserAsync()) : (IUser)Client.CurrentUser), r.Channel, r.Command))
                            {
                                if (Tools.CheckPermissions(Client, r.Author, r.Channel, r.Command))
                                {
                                    await LogMessage(r.Message, $"Running command \"{r.Command.Name}\" asynchronously."); await r.Channel.TriggerTypingAsync();

                                    CommandContext context = new CommandContext(r.CommandSegments.ToArray(), r.Message, _client, db)
                                    {
                                        Command = r.Command,
                                        UserData = await db.Users.GetOrCreateAsync(db, (long)r.Author.Id, () => new User(r.Author))
                                    };

                                    if (r.Channel is IGuildChannel gc)
                                    {
                                        context.GuildData = await db.Guilds.GetOrCreateAsync(db, (long)gc.GuildId, () => new Guild(gc.Guild));
                                        context.ChannelData = await db.Channels.GetOrCreateAsync(db, (long)r.Channel.Id, () => new Channel(gc));
                                    }

                                    string[] cmdsegarr = r.CommandSegments.ToArray();
                                    await r.Command.Run(cmdsegarr, context);

                                    context.Happiness += 1;
                                    context.UserData.CommandsRun += 1;

                                    RequestTelemetry request = Tools.GetRequestTelemetry(r.Author, r.Channel, r.Command, r.Start, "200", true);
                                    _telemetryClient?.TrackRequest(request);
                                }
                                else
                                {
                                    await LogMessage(r.Message, "Attempt to run command without correct user permissions.");
                                    await Tools.SendTemporaryMessage(r.Message, r.Channel, $"Oi! You're not allowed to run that command! Fuck off!");
                                    _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(r.Author, r.Channel, r.Command, r.Start, "401", false));
                                }
                            }
                            else
                            {
                                await LogMessage(r.Message, "Attempt to run command without correct bot permissions.");
                                await Tools.SendTemporaryMessage(r.Message, r.Channel, $"Sorry! I don't have permission to run that command in this server! Contact an admin/mod for more info.");
                                _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(r.Author, r.Channel, r.Command, r.Start, "403", false));
                            }
                        }
                        else
                        {
                            await LogMessage(r.Message, "Attempt to run command requiring guild within non-guild channel.");
                            await Tools.SendTemporaryMessage(r.Message, r.Channel, "This command requires a server to run! Sorry!");
                            _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(r.Author, r.Channel, r.Command, r.Start, "403", false));
                        }
                    }
                    catch (CommandException ex)
                    {
                        Console.WriteLine(ex);
                        await r.Channel.SendMessageAsync(ex.Message);
                        _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(r.Author, r.Channel, r.Command, r.Start, "400", false));
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        Console.WriteLine(ex);
                        await r.Channel.SendMessageAsync("Hey there! That's gonna cause some issues, no thanks!!");
                        _telemetryClient?.TrackRequest(Tools.GetRequestTelemetry(r.Author, r.Channel, r.Command, r.Start, "400", false));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Tools.ManageException(r.Message, r.Channel, ex, r.Command);
                    }
                    finally
                    {
                        await db.SaveChangesAsync();
                    }
                }
            }).Start();
        }

        private static async Task DiscordClient_Log(LogMessage arg)
        {
#if UI
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ((MainWindow)App.Current.MainWindow).discordLog.AppendText(arg.ToString() + "\r\n");
            });
#else
            await LogMessage(arg.Source.ToUpper(), arg.Message);
#endif
        }
    }
}
