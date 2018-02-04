using DSharpPlus;
using DSharpPlus.Entities;
using ModerationCommands.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ModerationCommands
{
    [RequiresGuild]
    class HackBanCommand : ModernDiscordCommand
    {
        public override string Name => "Hack Ban";

        public override string Description => "Bans a user not currently in the server";

        public override string[] Aliases => new[] { "hackban" };

        public override Permissions? UserPermissions => Permissions.BanMembers;

        public override Permissions? BotPermissions => Permissions.BanMembers;

        public async Task<CommandResult> RunAsync()
        {
            GuildData guild = this.GetData<GuildData>(Context.Guild.Id.ToString());
            if (guild == null)
            {
                guild = new GuildData();
            }

            DiscordEmbedBuilder embedBuilder = Context.GetEmbedBuilder("Hackbans");
            foreach (Hackban b in guild.Hackbans)
            {
                DiscordUser user = await Context.Client.GetUserAsync(b.User);
                DiscordUser issuer = await Context.Guild.GetMemberAsync(b.Issuer) ?? await Context.Client.GetUserAsync(b.Issuer);
                embedBuilder.AddField($"{user.Username}#{user.Discriminator} ({user.Id})", $"Issued by: {(issuer is DiscordMember m ? m.Mention : issuer.Username)} at {b.Timestamp}");
            }

            return embedBuilder.Build();
        }

        public async Task<CommandResult> Run(string action, DiscordUser user)
        {
            GuildData guild = this.GetData<GuildData>(Context.Guild.Id.ToString());
            if (guild == null)
            {
                guild = new GuildData();
            }

            if (action == "add")
            {
                guild.Hackbans.Add(new Hackban(user.Id, Context.Author.Id));
                this.SetData(Context.Guild.Id.ToString(), guild);

                DiscordMember member;
                if ((member = Context.Guild.Members.FirstOrDefault(m => m.Id == user.Id)) != null)
                {
                    await member.BanAsync(reason: $"Hackban by {Context.Author.Username}#{Context.Author.Discriminator} via WamBot");
                }
                return $"Hackbanned {user.Mention} ({user.Username}#{user.Discriminator})";
            }
            else if (action == "remove")
            {
                guild.Hackbans.RemoveAll(g => g.User == user.Id);
                this.SetData(Context.Guild.Id.ToString(), guild);

                DiscordBan ban = (await Context.Guild.GetBansAsync()).FirstOrDefault(b => b.User.Id == user.Id);
                if (ban != null)
                {
                    await ban.User.UnbanAsync(Context.Guild, $"Revoked hackban by {Context.Author.Username}#{Context.Author.Discriminator} via WamBot");
                }

                return $"Removed hackban for {user.Mention} ({user.Username}#{user.Discriminator})";
            }
            else
            {
                return "Available actions: add, remove.";
            }
        }
    }
}
