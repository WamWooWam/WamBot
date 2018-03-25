using WamBotRewrite.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tweet = Tweetinvi.TweetAsync;
using Messages = Tweetinvi.Message;
using TwitterMessages = Tweetinvi.MessageAsync;
using TwitterUser = Tweetinvi.UserAsync;
using Discord;
using System.Threading;
using WamBotRewrite.Data;

namespace WamBotRewrite.Commands
{
    class TwitterCommands : CommandCategory
    {
        public override string Name => "Twitter";

        public override string Description => "Commands that allow me to interface with Twitter!";

        internal Dictionary<ulong, long> _awaitingMessages = new Dictionary<ulong, long>();
        

        [Command("Link Account", "Allows you to link your twitter account to your user!", new[] { "twitter-link", "add-twitter", "link-twitter" })]
        public async Task AssociateTwitter(CommandContext ctx, string username)
        {
            var tu = await TwitterUser.GetUserFromScreenName(username);

            if (tu != null)
            {
                EmbedBuilder b = ctx.GetEmbedBuilder("Link Twitter account?")
                    .WithThumbnailUrl(tu.ProfileImageUrlHttps)
                    .WithDescription("Are you sure you want to link this Twitter account? (Y/N)");

                b.AddField("Username", tu.ScreenName);
                b.AddField("Tweets", tu.StatusesCount.ToString(), true);
                b.AddField("Followers", tu.FollowersCount.ToString(), true);

                try
                {
                    var m = await ctx.ReplyAndAwaitResponseAsync(string.Empty, embed: b.Build());

                    if (m.Content.ToLowerInvariant().StartsWith("y"))
                    {
                        int code = Program.Random.Next(1, 999999);
                        await TwitterMessages.PublishMessage(
                            $"Hello! This is an automated message to confirm linking your Twitter account to WamBot!\n\n" +
                            $"If you didn't request this, you can safely ignore this message, if you did, here's the code you need: {code:D6}", tu.Id);

                        _awaitingMessages[ctx.Author.Id] = tu.Id;
                        m = await ctx.ReplyAndAwaitResponseAsync("A direct message has been sent to this Twitter user! Respond here with your code to finish linking your account.", timeout: 120_000);
                        if (m.Content.Trim() == code.ToString())
                        {
                            ctx.UserData.TwitterId = tu.Id;
                            await ctx.ReplyAsync("Twitter account linked!");
                        }
                    }
                    else
                    {
                        await ctx.ReplyAsync("Cancelled linking Twitter account.");
                    }
                }
                catch
                {
                    await ctx.ReplyAsync("Cancelled linking Twitter account.");
                }
            }
            else
            {
                await ctx.ReplyAsync($"Unable to find @{username} on Twitter!");
            }
        }
    }
}
