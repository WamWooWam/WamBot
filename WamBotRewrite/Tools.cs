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

namespace WamBotRewrite
{
    static class Tools
    {
        internal static bool IsImplicit(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ImplicitAttribute), false);
        }

        internal static bool IsParams(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ParamArrayAttribute), false);
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

        internal static bool CheckPermissions(DiscordSocketClient client, IUser author, ISocketMessageChannel channel, CommandRunner command)
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

        public static async Task<T> GetOrCreateAsync<T>(this DbSet<T> db, DbContext ctx, object key, Func<T> createPrecidate) where T : class
        {
            T value = await db.FindAsync(key);
            if (value == null)
            {
                value = createPrecidate();
                await db.AddAsync(value);
                await ctx.SaveChangesAsync();
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

        internal static RequestTelemetry GetRequestTelemetry(SocketUser author, ISocketMessageChannel channel, CommandRunner command, DateTimeOffset start, string code, bool success)
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
                await channel.SendMessageAsync($"Use `{Program.Config.Prefix}help` for a list of available commands. To get info on when commands are added or removed, set an announements channel with `{Program.Config.Prefix}announce`.");
                await channel.SendMessageAsync($"For more information, visit http://wamwoowam.co.uk/wambot/.");
            }

            Program.Config.SeenGuilds.Add(arg.Id);
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
