using ConsoleDraw;
using ConsoleDraw.UI;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WamBot.Api;
using WamBot.Cli.Models;
using WamWooWam.Core;
using WamWooWam.Core.Serialisation;

namespace WamBot.Cli
{
    partial class Program
    {
        internal static Config Config { get; set; }

        private static FrameBuffer buffer;
        private static DiscordClient Client { get; set; }
        internal static TelemetryClient TelemetryClient { get; set; }

        private static HttpClient _httpClient;
        private static TextArea _dSharpPlusLogArea;
        private static TextArea _appLogArea;
        private static Button _statusButton;
        private static Button _pingArea;
        private static Timer _saveTimer;
        private static Timer _statusTimer;
        private static Dictionary<ActivityType, string> _activityTypes = new Dictionary<ActivityType, string>()
        {
            { ActivityType.ListeningTo, "Listening to" },
            { ActivityType.Playing, "Playing" },
            { ActivityType.Streaming, "Streaming" },
            { ActivityType.Watching, "Watching" }
        };

        internal static Dictionary<ulong, sbyte> HappinessData => Config.Happiness;
        internal static List<DiscordCommand> Commands { get; set; } = new List<DiscordCommand>();
        internal static List<IParseExtension> ParseExtensions { get; set; } = new List<IParseExtension>();
        internal static Dictionary<ICommandsAssembly, List<DiscordCommand>> AssemblyCommands { get; set; } = new Dictionary<ICommandsAssembly, List<DiscordCommand>>();

        static async Task Main(string[] args)
        {
            _ = new ArgumentException(); // fuckin sdk bugs reeeeeeee

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            buffer = new FrameBuffer(Console.WindowWidth, Console.WindowHeight);

            FrameBufferGraphics graphics = new FrameBufferGraphics(buffer);
            UIExtension ext = new UIExtension(buffer);
            buffer.AddDrawExtension(ext);

            SetupUI(buffer, ext);

            buffer.Run();
            await Run(buffer);

            await Task.Yield();
            ext.BeginEventLoop();
        }

        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {

        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                buffer?.Stop();
                Console.ResetColor();
                Console.WriteLine(e.ExceptionObject);
            }

            File.WriteAllText($"wambot-{(e.IsTerminating ? "crash" : "exception")}-{DateTime.Now.Ticks}.json", e.ExceptionObject.ToJson());
        }

        private static async Task Run(FrameBuffer buffer)
        {
            _appLogArea.WriteLine("Loading config...");
            ReloadConfig();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("user-agent", $"Mozilla/5.0 ({Environment.OSVersion.ToString()}; {Environment.OSVersion.Platform}; {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}) WamBot/3.0.0 (fuck you)");

            Client = new DiscordClient(new DiscordConfiguration()
            {
                Token = Config.Token,
                LogLevel = LogLevel.Debug
            });
            Client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
            Client.Ready += Client_Ready;
            Client.MessageCreated += Client_MessageCreated;
            Client.MessageUpdated += Client_MessageUpdated;
            Client.GuildCreated += Client_GuildCreated;
            Client.GuildDeleted += Client_GuildDeleted;
            Client.GuildAvailable += Client_GuildAvailable;
            Client.GuildUnavailable += Client_GuildUnavailable;
            Client.Heartbeated += Client_Heartbeated;
            Client.ClientErrored += Client_ClientErrored;
            Client.SocketErrored += Client_SocketErrored;

            await LoadPluginsAsync();

            _appLogArea.Text += ("Connecting to Discord... ");

            await Client.ConnectAsync();
        }

        private static void ReloadConfig()
        {
            if (File.Exists("config.json"))
            {
                try
                {
                    string str = File.ReadAllText("config.json");
                    Config = JsonConvert.DeserializeObject<Config>(str);

                    if (Config == null)
                    {
                        ConfigLoadFailure(buffer, new NullReferenceException());
                    }

                }
                catch (Exception ex)
                {
                    ConfigLoadFailure(buffer, ex);
                }
            }
            else
            {
                Config = new Config();
                File.WriteAllText("config.json", JsonConvert.SerializeObject(Config));
            }

            _saveTimer?.Stop();
            _saveTimer = new Timer(30_000) { AutoReset = true };
            _saveTimer.Elapsed += SaveTimer_Elapsed;
            _saveTimer.Start();

            _statusTimer?.Stop();
            _statusTimer = new Timer(Config.StatusUpdateInterval.TotalMilliseconds) { AutoReset = true };
            _statusTimer.Elapsed += UpdateStatusTimer;
            _statusTimer.Start();

            TelemetryClient?.Flush();
            if (Config.ApplicationInsightsKey != Guid.Empty)
            {
                TelemetryConfiguration.Active.InstrumentationKey = Config.ApplicationInsightsKey.ToString();
                TelemetryClient = new TelemetryClient();
                TelemetryClient.TrackEvent(new EventTelemetry("Startup"));

                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                _appLogArea.WriteLine($"Application Insights telemetry configured! {TelemetryClient.IsEnabled()}");
            }
            else
            {
                _appLogArea.WriteLine("Application Insights telemetry id unavailable, disabling...");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }
        }

        private static void SaveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                File.WriteAllText("config.json", JsonConvert.SerializeObject(Config, Formatting.Indented));
                TelemetryClient?.Flush();
            }
            catch { }
        }

        static Random random = new Random();

        private static async void UpdateStatusTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Client != null)
            {
                await UpdateStatusAsync();
            }
        }

        private static async Task UpdateStatusAsync()
        {
            try
            {
                StatusModel model = Config.StatusMessages.ElementAt(random.Next(Config.StatusMessages.Count));
                await Client.UpdateStatusAsync(new DiscordActivity(model.Status, model.Type));
                _appLogArea?.WriteLine($"Status set to {model.Type} {model.Status}");
            }
            catch (Exception ex)
            {
                _appLogArea?.WriteLine($"An {ex.GetType().Name} occured updating status.");
            }
        }

        private static void ConfigLoadFailure(FrameBuffer buffer, Exception ex)
        {
            buffer.Stop();
            Console.ResetColor();
            Console.Clear();
            ConsolePlus.WriteSubHeading("Unable to load config", $"A {ex.GetType().Name} occured while loading your configuration file! WamBot will now exit.", colour: ConsoleColor.Red);
            Environment.Exit(0);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            TelemetryClient?.TrackEvent(new EventTelemetry("Exit"));
            TelemetryClient?.Flush();
        }

        private static void SetupUI(FrameBuffer buffer, UIExtension ext)
        {
            Button heading = new Button()
            {
                Text = "WamBot 3.1.2",
                Height = 4,
                BackgroundColour = ConsoleColor.Red,
                BorderColor = ConsoleColor.Gray,
                ForegroundColour = ConsoleColor.White,
                Dock = DockStyle.Top,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1, 1, 0, 1),
                Enabled = false
            };

            _appLogArea = new TextArea()
            {
                Width = buffer.Width / 2 + 7,
                Y = 4,
                Height = buffer.Height - 4 - 2,
                ForegroundColour = ConsoleColor.White,
                BorderColor = ConsoleColor.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                Enabled = true,
                ReadOnly = true
            };

            _dSharpPlusLogArea = new TextArea()
            {
                Width = buffer.Width / 2 - 7,
                X = buffer.Width / 2 + 7,
                Y = 4,
                Height = buffer.Height - 4 - 2,
                ForegroundColour = ConsoleColor.White,
                BorderColor = ConsoleColor.Gray,
                BorderThickness = new Thickness(1),
                Enabled = true,
                ReadOnly = true
            };

            Panel panel = new Panel()
            {
                Width = buffer.Width,
                Height = 3,
                Y = buffer.Height - 3
            };

            _statusButton = new Button()
            {
                Width = buffer.Width / 4,
                Height = 3,
                Dock = DockStyle.Left,
                BorderColor = ConsoleColor.Gray,
                ForegroundColour = ConsoleColor.White,
                BackgroundColour = ConsoleColor.DarkGray,
                Text = "Loading...",
                Padding = new Thickness(0),
                Enabled = false
            };

            _pingArea = new Button()
            {
                Width = buffer.Width / 4,
                Height = 3,
                X = buffer.Width - _statusButton.Width,
                BorderColor = ConsoleColor.Gray,
                ForegroundColour = ConsoleColor.White,
                BackgroundColour = ConsoleColor.DarkGray,
                Text = "Ping: 999ms, Chk: 0",
                Padding = new Thickness(0),
                Enabled = false
            };

            TextArea commandArea = new TextArea()
            {
                Width = (buffer.Width / 4) * 2,
                Height = 3,
                X = buffer.Width / 4,
                BorderColor = ConsoleColor.Gray,
                ForegroundColour = ConsoleColor.White,
                BackgroundColour = ConsoleColor.DarkGray,
                Padding = new Thickness(0)
            };

            commandArea.Activated += CommandArea_Activated;

            panel.Controls.Add(_pingArea);
            panel.Controls.Add(_statusButton);
            panel.Controls.Add(commandArea);

            ext.BasePanel.Controls.Add(heading);
            ext.BasePanel.Controls.Add(panel);

            ext.BasePanel.Controls.Add(_appLogArea);
            ext.BasePanel.Controls.Add(_dSharpPlusLogArea);
        }

        internal static async Task LoadPluginsAsync()
        {
            _appLogArea.WriteLine("");
            List<string> dlls = Directory.EnumerateFiles("Plugins").Where(f => Path.GetExtension(f) == ".dll").ToList();
            dlls.Insert(0, Assembly.GetExecutingAssembly().Location);
            foreach (string str in Config.AdditionalPluginDirectories)
            {
                dlls.AddRange(Directory.EnumerateFiles(str).Where(f => Path.GetExtension(f) == ".dll"));
            }

            _appLogArea.WriteLine($"{(Commands.Any() ? "Reloading" : "Loading")} {dlls.Count()} plugins...");

            Commands.Clear();
            AssemblyCommands.Clear();
            ParseExtensions.Clear();

            foreach (string dllPath in dlls)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(dllPath);
                    if (assembly != null && assembly.DefinedTypes.Any(t => t.GetInterfaces()?.Contains(typeof(ICommandsAssembly)) == true))
                    {
                        _appLogArea.WriteLine($"Searching {Path.GetFileName(dllPath)} for commands.");
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
                                ParseExtensions.Add((IParseExtension)Activator.CreateInstance(type));
                            }

                            if (type.GetInterfaces().Contains(typeof(IParamConverter)))
                            {
                                Tools.RegisterPatameterParseExtension((IParamConverter)Activator.CreateInstance(type));
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
                            _appLogArea.WriteLine($"Loaded {asmCommands.Count()} commands from {Path.GetFileName(dllPath)} without command assembly manifest. Categorised help will be unavailable for these commands.");
                            _appLogArea.WriteLine("Consider adding an \"ICommandAssembly\" class as soon as possible.");
                        }
                        else
                        {
                            if (asm is IBotStartup s)
                            {
                                await s.Startup(Client);
                            }

                            _appLogArea.WriteLine($"Loaded {asmCommands.Count()} plugins from {Path.GetFileName(dllPath)}!");
                        }

                        Commands.AddRange(asmCommands);
                        if (asm != null)
                        {
                            AssemblyCommands.Add(asm, asmCommands);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _appLogArea.WriteLine($"An {ex.GetType().Name} occured while loading from {Path.GetFileName(dllPath)}\n{ex.Message}");
                }
            }

            _appLogArea.WriteLine("");
            _appLogArea.WriteLine($"Plugins loaded!");
            _appLogArea.WriteLine($"{Commands.Count} commands available: {string.Join(", ", Commands.Select(c => c.Name))}");
            _appLogArea.WriteLine($"{ParseExtensions.Count} parse extensions available: {string.Join(", ", ParseExtensions.Select(c => c.Name))}");
            _appLogArea.WriteLine("");
        }

        private static async void CommandArea_Activated(object sender, EventArgs e)
        {
            TextArea area = sender as TextArea;
            string txt = area.Text.Trim();
            area.Text = string.Empty;

            IEnumerable<string> segments = Strings.SplitCommandLine(txt);
            string command = segments.FirstOrDefault();
            if (command != null)
            {
                _appLogArea.WriteLine($">{txt}");
                switch (segments.FirstOrDefault())
                {
                    case "list":
                        ListCommand(segments);
                        break;
                    case "leave":
                        await LeaveCommand(segments);
                        break;
                    case "blacklist":
                        await BlacklistCommand(segments);
                        break;
                    case "name":
                        await NameCommand(segments);
                        break;
                    case "exit":
                        await Client.DisconnectAsync();
                        Environment.Exit(0);
                        break;
                    case "disconnect":
                        _appLogArea.WriteLine("Disconnecting...");
                        await Client.DisconnectAsync();
                        _appLogArea.WriteLine("Disconnected.");
                        break;
                    case "connect":
                        _appLogArea.WriteLine("Connecting...");
                        await Client.ConnectAsync();
                        break;
                    case "unload":
                        await UnloadCommand(segments);
                        break;
                    case "reload":
                        ReloadConfig();
                        await LoadPluginsAsync();
                        break;
                    case "clear":
                        _appLogArea.Text = "";
                        break;
                    default:
                        _appLogArea.WriteLine($"{command}: Bad command or filename.");
                        break;
                }
            }
        }

        private static async Task UnloadCommand(IEnumerable<string> segments)
        {
            if (segments.Count() == 2)
            {
                if (File.Exists(Path.Combine("Plugins", segments.ElementAt(1))))
                {
                    File.Move(Path.Combine("Plugins", segments.ElementAt(1)), Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(segments.ElementAt(1)) + Path.GetFileName(Path.GetTempFileName())));
                    await LoadPluginsAsync();
                }
            }
        }

        private static async Task NameCommand(IEnumerable<string> segments)
        {
            if (segments.Count() == 2)
            {
                string name = segments.ElementAt(1);
                await Client.UpdateCurrentUserAsync(name);
            }
        }

        private static async Task BlacklistCommand(IEnumerable<string> segments)
        {
            if (segments.Count() == 2 && ulong.TryParse(segments.ElementAt(1), out ulong i))
            {
                DiscordGuild guild = Client.Guilds.Values.FirstOrDefault(g => g.Id == i);
                if (guild != null)
                {
                    await guild.LeaveAsync();
                    Config.DisallowedGuilds.Add(guild.Id);
                    _appLogArea.WriteLine($"Left {guild.Name}.");
                }
            }
            else
            {
                _appLogArea.WriteLine("Please specify a guild id!");
            }
        }

        private static async Task LeaveCommand(IEnumerable<string> segments)
        {
            if (segments.Count() == 2 && ulong.TryParse(segments.ElementAt(1), out ulong id))
            {
                DiscordGuild guild = Client.Guilds.Values.FirstOrDefault(g => g.Id == id);
                if (guild != null)
                {
                    await guild.LeaveAsync();
                    _appLogArea.WriteLine($"Left {guild.Name}.");
                }
            }
            else
            {
                _appLogArea.WriteLine("Please specify a guild id!");
            }
        }

        private static void ListCommand(IEnumerable<string> segments)
        {
            if (segments.Count() == 2)
            {
                switch (segments.ElementAt(1))
                {
                    case "members":
                    case "users":
                        IEnumerable<DiscordMember> members = Client.Guilds.Values.SelectMany(g => g.Members).Distinct(new MemberEquality());
                        _appLogArea.WriteLine($" -- Listing {members.Count()} Members -- ");
                        foreach (DiscordUser memb in members)
                        {
                            _appLogArea.WriteLine($"{memb.Username}#{memb.Discriminator} ({memb.Id})");
                        }
                        break;
                    case "guilds":
                        _appLogArea.WriteLine($" -- Listing {Client.Guilds.Count} Guilds -- ");
                        foreach (DiscordGuild guild in Client.Guilds.Values)
                        {
                            _appLogArea.WriteLine($"{guild.Name} ({guild.Id}): {guild.MemberCount} members.");
                        }
                        break;
                    default:
                        _appLogArea.WriteLine($"Specify guilds/users");
                        break;
                }
            }
            else
            {
                _appLogArea.WriteLine($"Specify guilds/users");
            }
        }
    }
}
