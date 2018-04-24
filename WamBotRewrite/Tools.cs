using Discord;
using Discord.WebSocket;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WamBotRewrite.Api;
using WamBotRewrite.Data;
using WamWooWam.Core;

namespace WamBotRewrite
{
    internal static class Tools
    {
        internal static bool IsImplicit(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ImplicitAttribute), false);
        }

        internal static bool IsParams(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ParamArrayAttribute), false);
        }

        internal static bool IsAprilFools(this DateTime time)
        {
            return time.Day == 1 && time.Month == 4 && time.Hour < 12;
        }

        internal static async Task SendTemporaryMessage(IMessage m, IMessageChannel c, string mess, int timeout = 5_000)
        {
            await Task.Yield();
            new Task(async () =>
            {
                IMessage message = await c.SendMessageAsync(mess);
                await Task.Delay(timeout);
                await message.DeleteAsync();
            }).Start();
        }

        internal static bool CheckPermissions(DiscordSocketClient client, IUser author, IMessageChannel channel, CommandRunner command)
        {
            bool go = true;

            if (command.OwnerOnly)
            {
                if (author.Id == Program.Application.Owner.Id || author.Id == client.CurrentUser.Id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (author is IGuildUser memb)
            {
                ChannelPermissions p = memb.GetPermissions(channel as IGuildChannel);
                GuildPermissions g = memb.GuildPermissions;
                go = g.Has(memb.Id != client.CurrentUser.Id ? command.UserPermissions : command.BotPermissions) || g.Has(GuildPermission.Administrator);
            }
            else
            {
                go = true;
            }

            return go;
        }

        public static async Task<T> GetOrCreateAsync<T>(this DbSet<T> db, DbContext ctx, object key, IFactory<T> factory) where T : class
        {
            T value = await db.FindAsync(key);
            if (value == null)
            {
                value = await factory.CreateAsync(key);
                await db.AddAsync(value);
                await ctx.SaveChangesAsync();
            }

            db.Attach(value);

            return value;
        }

        public static T GetOrCreate<T>(this DbSet<T> db, DbContext ctx, object key, IFactory<T> factory) where T : class
        {
            T value = db.Find(key);
            if (value == null)
            {
                value = factory.Create(key);
                db.Add(value);
                ctx.SaveChanges();
            }

            db.Attach(value);

            return value;
        }

        public static Timer CreateTimer(TimeSpan interval, ElapsedEventHandler action)
        {
            Timer timer = new Timer { Interval = interval.TotalMilliseconds };
            timer.Elapsed += action;
            timer.Start();

            return timer;
        }

        public static string GetMethodDeclaration(MethodInfo method)
        {
            StringBuilder builder = new StringBuilder();
            
            foreach(var attr in method.DeclaringType.CustomAttributes)
            {
                CommandRunner.AppendAttribute(builder, false, attr, attr.AttributeType);
            }

            foreach(var attr in method.CustomAttributes)
            {
                CommandRunner.AppendAttribute(builder, false, attr, attr.AttributeType);
            }

            if (method.IsPublic)
                builder.Append("public ");
            else if (method.IsPrivate)
                builder.Append("private ");

            if (method.IsStatic)
                builder.Append("static ");

            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                builder.Append("async ");

            builder.Append(CommandRunner.PrettyTypeName(method.ReturnType));
            builder.Append(" ");

            builder.Append(method.Name);
            builder.Append("(");

            var parameters = method.GetParameters().ToArray();
            foreach (var p in parameters)
            {
                CommandRunner.AppendParameter(parameters, builder, p, false);
            }
            builder.Append(");");

            return builder.ToString();
        }

        internal static Color GetUserColor(IGuildUser m)
        {
            return m.RoleIds.Select(s => m.Guild.Roles.First(r => r.Id == s)).OrderByDescending(r => r.Position).FirstOrDefault(c => c.Color.RawValue != Color.Default.RawValue)?.Color ?? new Color(0, 137, 255);
        }

        internal static bool IsCurrent(this IUser user) =>
            user.Id == Program.Client.CurrentUser.Id;

        internal static async Task SendChunkedMessageAsync(this IMessageChannel channel, string content, bool tts, Channel data = null)
        {
            List<string> chunks = new List<string>();
            var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToArray();

            foreach (string line in lines)
            {
                if (line.Length < 1993)
                {
                    chunks.Add(line + Environment.NewLine);
                }
                else
                {
                    chunks.AddRange(line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w + " "));
                }
            }

            bool finished = false;
            int i = 0;
            while (!finished)
            {
                int l = 0;
                StringBuilder builder = new StringBuilder();
                if (content.Trim().StartsWith("```"))
                {
                    builder.Append("```");
                    l += 3;
                }

                for (; i < chunks.Count; i++)
                {
                    string str = chunks.ElementAt(i);
                    if (l + str.Length < 1996)
                    {
                        l += str.Length;
                        builder.Append(str);
                    }
                    else
                    {
                        break;
                    }
                }

                if (content.Trim().EndsWith("```"))
                {
                    builder.Append("```");
                    l += 3;
                }

                if (!(i < chunks.Count))
                {
                    finished = true;
                }

                if (data != null)
                    data.MessagesSent += 1;

                await channel.SendMessageAsync(builder.ToString(), tts);
                await Task.Delay(2000);
            }
        }

        internal static string ProtectedReadLine()
        {
            string ret = "";
            while (true)
            {
                ConsoleKeyInfo inf = Console.ReadKey(true);
                if (inf.Key == ConsoleKey.Backspace)
                {
                    if (ret.Length > 0)
                        ret = ret.Substring(0, ret.Length - 1);
                }
                else if (inf.Key == ConsoleKey.Enter)
                {
                    return ret;
                }
                else if (char.IsLetterOrDigit(inf.KeyChar) || char.IsPunctuation(inf.KeyChar))
                {
                    ret += inf.KeyChar;
                }
                else
                {
                    Console.Write("\b");
                }
            }
        }

        public static HappinessLevel GetHappinessLevel(sbyte h)
        {
            if (h >= 102)
            {
                return HappinessLevel.Adore;
            }

            if (h < 102 && h >= 51)
            {
                return HappinessLevel.Like;
            }

            if (h < 51 && h >= -51)
            {
                return HappinessLevel.Indifferent;
            }

            if (h < -51 && h >= -102)
            {
                return HappinessLevel.Dislike;
            }

            if (h < -102)
            {
                return HappinessLevel.Hate;
            }

            return HappinessLevel.Indifferent;
        }

        internal static DateTime GetAssemblyDate(AssemblyName mainAssembly)
        {
            return new DateTime(2000, 1, 1).AddDays(mainAssembly.Version.Build).AddSeconds(mainAssembly.Version.MinorRevision * 2);
        }

        internal static RequestTelemetry GetRequestTelemetry(IUser author, IMessageChannel channel, CommandRunner command, DateTimeOffset start, string code, bool success)
        {
            RequestTelemetry tel = new RequestTelemetry(command?.GetType().Name ?? "N/A", start, DateTimeOffset.Now - start, code, success);
            tel.Properties.Add("invoker", author.Id.ToString());
            tel.Properties.Add("channel", channel.Id.ToString());
            tel.Properties.Add("guild", (channel is IGuildChannel g ? g.Guild.Id.ToString() : "N/A"));


            return tel;
        }

        internal static async Task SendWelcomeMessage(SocketGuild arg)
        {
            var channel = GetFirstChannel(arg);

            if (channel != null)
            {
                await channel.SendMessageAsync("Welcome to WamBot!");
                await channel.SendMessageAsync("Thank you for adding WamBot to your server! Here's a few quick tips to get you started.");
                await channel.SendMessageAsync($"Use `{Program.Config.Bot.Prefix}help` for a list of available commands. To get info on when commands are added or removed, set an announements channel with `{Program.Config.Bot.Prefix}announce`.");
                await channel.SendMessageAsync($"For more information, visit http://wamwoowam.co.uk/wambot/.");
            }
        }

        internal static ISocketMessageChannel GetFirstChannel(SocketGuild arg)
        {
            var channels = arg.TextChannels
               .OrderBy(c => c.Position);

            var channel =
                channels.FirstOrDefault(c => c.Name.ToLowerInvariant().Contains("general") && CanSendMessages(arg, c)) ??
                channels.FirstOrDefault(c => CanSendMessages(arg, c));
            return channel;
        }

        internal static bool CanSendMessages(SocketGuild g, IGuildChannel c)
        {
            return g.CurrentUser.GetPermissions(c).Has(ChannelPermission.SendMessages);
        }

        internal static void ManageException(IMessage message, IMessageChannel channel, Exception ex, CommandRunner command)
        {
            if (!(ex is TaskCanceledException) && !(ex is OperationCanceledException))
            {
                Program.TelemetryClient?.TrackException(ex, new Dictionary<string, string> { { "command", command.GetType().Name } });

                ex = ex.InnerException ?? ex;

#if UI
                App.Current.Dispatcher.Invoke(() =>
                {
                    ((MainWindow)App.Current.MainWindow).recentExceptions.Items.Add(ex);
                });
#endif

                new Task(async () =>
                {
                    try
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                            .WithAuthor($"Error - WamBot {Assembly.GetEntryAssembly().GetName().Version.ToString(3)}", Program.Application?.IconUrl)
                            .WithDescription(DateTime.Now.IsAprilFools() ? "OOPSIE WOOPSIE!! Uwu We made a fucky wucky!! A wittle fucko boingo! The code monkeys at our headquarters are working VEWY HAWD to fix this!" : $"Something's gone very wrong executing that command, and an {ex.GetType().Name} occured.")
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

        /// <summary>
        /// Compute the distance between two strings.
        /// From: https://www.dotnetperls.com/levenshtein
        /// </summary>
        public static int StringDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
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
