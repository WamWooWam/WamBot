using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Core
{
    internal static class InternalTools
    {
        internal static RequestTelemetry GetRequestTelemetry(DiscordUser author, DiscordChannel channel, BaseDiscordCommand command, DateTimeOffset start, string code, bool success)
        {
            RequestTelemetry tel = new RequestTelemetry(command?.GetType().Name ?? "N/A", start, DateTimeOffset.Now - start, code, success);
            tel.Properties.Add("invoker", author.Id.ToString());
            tel.Properties.Add("channel", channel.Id.ToString());
            tel.Properties.Add("guild", channel.Guild?.Id.ToString());

            return tel;
        }

        internal static bool CheckPermissions(DiscordClient client, DiscordUser author, DiscordChannel channel, BaseDiscordCommand command)
        {
            bool go = true;

            if (command.HasAttribute<OwnerAttribute>())
            {
                if (author.Id == client.CurrentApplication.Owner.Id || author.Id == client.CurrentUser.Id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (command.HasAttribute<RequiresGuildAttribute>() && channel.IsPrivate)
            {
                return false;
            }

            if (author is DiscordMember memb)
            {
                Permissions p = memb.PermissionsIn(channel);
                go = p.HasFlag((memb.Id != client.CurrentUser.Id ? command.UserPermissions : command.BotPermissions) ?? command.RequiredPermissions) || p.HasFlag(Permissions.Administrator);
            }
            else
            {
                go = true;
            }

            return go;
        }

        internal static async Task SendWelcomeMessage(DiscordGuild g, Config config)
        {
            if (!config.SeenGuilds.Contains(g.Id))
            {
                DiscordChannel channel = await GetFirstChannelAsync(g);

                if (channel != null)
                {
                    await channel.SendMessageAsync("Welcome to WamBot!");
                    await channel.SendMessageAsync("Thank you for adding WamBot to your server! Here's a few quick tips to get you started.");
                    await channel.SendMessageAsync($"Use `{config.Prefix}help` for a list of available commands. To get info on when commands are added or removed, set an announements channel with `{config.Prefix}announce`.");
                    await channel.SendMessageAsync($"For more information, visit http://wamwoowam.co.uk/projects/wambot/.");
                }

                config.SeenGuilds.Add(g.Id);
            }
        }

        internal static async Task<DiscordChannel> GetFirstChannelAsync(DiscordGuild g)
        {
            var channels = (await g.GetChannelsAsync())
                .Where(c => c.Type == ChannelType.Text)
                .OrderBy(c => c.Position);

            DiscordChannel channel =
                channels.FirstOrDefault(c => c.Name.ToLowerInvariant().Contains("general") && CanSendMessages(g, c)) ??
                channels.FirstOrDefault(c => CanSendMessages(g, c));
            return channel;
        }

        internal static bool CanSendMessages(DiscordGuild g, DiscordChannel c)
        {
            return c.PermissionsFor(g.CurrentMember).HasPermission(Permissions.SendMessages);
        }

        internal static async Task SendTemporaryMessage(DiscordMessage m, DiscordUser u, DiscordChannel c, string mess, int timeout = 5_000)
        {
            await Task.Yield();
            new Task(async () =>
            {
                DiscordMessage message = await c.SendMessageAsync(mess);
                await Task.Delay(timeout);
                await message.DeleteAsync();
            }).Start();
        }
    }
}
