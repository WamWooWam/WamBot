using Discord;
using Discord.WebSocket;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

        internal static async Task<bool> CheckPermissions(DiscordSocketClient client, IUser author, ISocketMessageChannel channel, CommandRunner command)
        {
            bool go = true;

            if (command.OwnerOnly)
            {
                if (author.Id == (await client.GetApplicationInfoAsync()).Owner.Id || author.Id == client.CurrentUser.Id)
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

        internal static RequestTelemetry GetRequestTelemetry(SocketUser author, ISocketMessageChannel channel, CommandRunner command, DateTimeOffset start, string code, bool success)
        {
            RequestTelemetry tel = new RequestTelemetry(command?.GetType().Name ?? "N/A", start, DateTimeOffset.Now - start, code, success);
            tel.Properties.Add("invoker", author.Id.ToString());
            tel.Properties.Add("channel", channel.Id.ToString());
            tel.Properties.Add("guild", (channel is IGuildChannel g ? g.Guild.Id.ToString() : "N/A"));

            return tel;
        }
    }
}
