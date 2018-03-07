using DSharpPlus;
using DSharpPlus.Entities;
using ModerationCommands.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ModerationCommands
{
    [HttpClient]
    [RequiresGuild]
    class SetupPinsCommand : DiscordCommand
    {
        static HttpClient _client = null;

        public SetupPinsCommand(HttpClient client)
        {
            if (_client == null)
            {
                _client = client;
            }
        }

        public override string Name => "Setup Pins";

        public override string Description => "Sets up a pins webhook to get around the 50 pin limit.";

        public override string[] Aliases => new[] { "pins" };

        public override Permissions? UserPermissions => base.UserPermissions | Permissions.ManageWebhooks;

        public async Task<CommandResult> RunAsync()
        {
            GuildData data = this.GetData<GuildData>(Context.Guild.Id.ToString());
            if (data.Webhook == null)
            {
                return "Pins have not been configured for this server. Run this command and specify a channel to enable pin redirection.";
            }

            await data.Webhook.DeleteAsync();
            data.Webhook = null;
            data.HasUpdatedPins = false;

            this.SetData(Context.Guild.Id.ToString(), data);
            return "Pin redirection disabled!";
        }

        public async Task<CommandResult> RunAsync(DiscordChannel channel)
        {
            if (channel.Guild.Id == Context.Guild.Id)
            {
                GuildData data = this.GetData<GuildData>(channel.Guild.Id.ToString());
                if (data.Webhook != null)
                {
                    return "Pins have already been configured for this server. Run this command without arguments to disable pin redirection.";
                }

                var webhooks = await channel.GetWebhooksAsync();

                using (MemoryStream str = new MemoryStream())
                using (Stream sourceStr = await _client.GetStreamAsync(Context.Client.CurrentUser.AvatarUrl))
                {
                    sourceStr.CopyTo(str);
                    str.Seek(0, SeekOrigin.Begin);

                    DiscordWebhook webhook = await channel.CreateWebhookAsync("WamBot Pins Webhook", str, $"Pins setup by {Context.Author.Username}#{Context.Author.Discriminator}");
                    Meta.WebhookClient.AddWebhook(webhook);
                    await webhook.ExecuteAsync("Webhook setup!");

                    data.Webhook = webhook;

                    DiscordMessage msg = await Context.ReplyAsync("Updating pins, this may take a while...");
                    await Meta.UpdatePinsAsync(DateTimeOffset.Now, Context.Channel, data, Context.Client);
                    await msg.DeleteAsync();

                    this.SetData(channel.Guild.Id.ToString(), data);
                    return "Pins channel configured and updated!";
                }
            }
            else
            {
                return "Oi! You cannae do that!";
            }
        }
    }
}
