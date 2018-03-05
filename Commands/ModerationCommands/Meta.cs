using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ModerationCommands.Data;
using WamBot.Api;
using WamWooWam.Core.Serialisation;

namespace ModerationCommands
{
    public class Meta : ICommandsAssembly, IBotStartup
    {
        internal static List<ulong> LockedGuilds = new List<ulong>();
        internal static DiscordWebhookClient WebhookClient = new DiscordWebhookClient();

        public string Name => "Moderation";

        public string Description => "Provides a basic set of moderation tools to do many things";

        public Version Version => new Version(1, 0, 0, 1);

        public Task Startup(DiscordClient client)
        {
            client.MessageCreated += Client_MessageCreated;
            client.GuildMemberAdded += Client_GuildMemberAdded;
            client.ChannelPinsUpdated += Client_ChannelPinsUpdated;

            return Task.CompletedTask;
        }

        private async Task Client_ChannelPinsUpdated(ChannelPinsUpdateEventArgs e)
        {
            if (e.Channel.Guild != null)
            {
                GuildData d = new HackBanCommand().GetData<GuildData>(e.Channel.GuildId.ToString());

                await UpdatePinsAsync(e.LastPinTimestamp, e.Channel, d, e.Client);

                new HackBanCommand().SetData(e.Channel.GuildId.ToString(), d);
            }
        }

        internal static async Task UpdatePinsAsync(DateTimeOffset lastPinTimestamp, DiscordChannel channel, GuildData d, DiscordClient client)
        {
            if (d.Webhook != null)
            {
                if (!WebhookClient.Webhooks.Any(w => w.Id == d.Webhook.Id))
                {
                    d.Webhook = await client.GetWebhookWithTokenAsync(d.Webhook.Id, d.Webhook.Token);
                    WebhookClient.AddWebhook(d.Webhook);
                }

                DiscordChannel pinsChannel = await client.GetChannelAsync(d.Webhook.ChannelId);

                if (lastPinTimestamp > d.LastPinTimestamp)
                {
                    List<DiscordMessage> pins = new List<DiscordMessage>();
                    DiscordMember currentMember = channel.Guild.CurrentMember;

                    if (!d.HasUpdatedPins)
                    {
                        var channels = channel.Guild.Channels.Where(c => c.Type == ChannelType.Text && !c.IsPrivate && c.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels));

                        if (!pinsChannel.IsNSFW)
                            channels = channels.Where(c => !c.IsNSFW);

                        foreach (DiscordChannel c in channels)
                        {
                            pins.AddRange(await c.GetPinnedMessagesAsync());
                            await Task.Delay(6000);
                        }
                        d.HasUpdatedPins = true;
                    }
                    else
                    {
                        pins.AddRange(await channel.GetPinnedMessagesAsync());
                    }

                    foreach (var pin in pins.OrderBy(p => p.CreationTimestamp).Where(p => p.Id > d.LastPinnedMessage))
                    {
                        try
                        {
                            var embeds = new List<DiscordEmbed>();

                            foreach (var attachment in pin.Attachments)
                            {
                                var builder = new DiscordEmbedBuilder()
                                    .WithAuthor(pin.Author?.Username ?? "Okay what the fuck?", null, pin.Author?.AvatarUrl)
                                    .WithTitle(attachment.FileName)
                                    .WithFooter($"In #{pin.Channel.Name}")
                                    .WithTimestamp(pin.CreationTimestamp);

                                if (attachment.Width != 0)
                                {
                                    builder = builder.WithImageUrl(attachment.Url);
                                }
                                else
                                {
                                    builder = builder.WithUrl(attachment.Url);
                                    builder.AddField("Attachment URL", attachment.Url);
                                }

                                embeds.Add(builder.Build());

                            }

                            string content = pin.Content
                                .Replace("@everyone", "@ everyone")
                                .Replace("@here", "@ here");

                            foreach (var user in pin.MentionedUsers)
                            {
                                content = content.Replace($"<@{user.Id}>", $"@{user.Username}")
                                    .Replace($"<@!{user.Id}>", $"@{user.Username}");
                            }

                            foreach (var role in pin.MentionedRoles)
                            {
                                content = content.Replace(role.Mention, $"@{role.Name}");
                            }

                            if (embeds.Any() || !string.IsNullOrWhiteSpace(content))
                            {
                                await d.Webhook.ExecuteAsync(content, $"{(pin.Author?.Username ?? "Okay what the fuck?")} - in #{pin.Channel.Name}", pin.Author?.AvatarUrl ?? currentMember.DefaultAvatarUrl, false, embeds);
                                await Task.Delay(1000);
                                d.LastPinnedMessage = pin.Id;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                }

                d.LastPinTimestamp = lastPinTimestamp;
            }
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            GuildData d = new HackBanCommand().GetData<GuildData>(e.Guild.Id.ToString());
            if (d?.Hackbans.Any(b => b.User == e.Member.Id) == true)
            {
                await e.Member.BanAsync(reason: "Hackban by WamBot");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (LockedGuilds.Any(g => g == e.Guild?.Id) && (e.Author as DiscordMember)?.PermissionsIn(e.Channel).HasPermission(Permissions.Administrator) != true)
            {
                try
                {
                    await e.Message.DeleteAsync();
                }
                catch { }
            }
        }
    }
}
