using WamBotRewrite.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tweet = Tweetinvi.TweetAsync;
using TwitterMessages = Tweetinvi.MessageAsync;
using TwitterUser = Tweetinvi.UserAsync;
using Discord;
using System.Threading;
using WamBotRewrite.Data;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using Tweetinvi.Parameters;

namespace WamBotRewrite.Commands
{
    class TwitterCommands : CommandCategory
    {
        public override string Name => "Twitter";

        public override string Description => "Commands that allow me to interface with Twitter!";

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

        [Command("Tweet", "Allows me to Tweet something you say!", new[] { "tweet" })]
        public async Task PostTweet(CommandContext ctx, [StringLength(280, ErrorMessage = "Hey! That's too long! Under 280 characters please!")] string message, string reply = null)
        {
            if ((ctx.Channel as ITextChannel)?.IsNsfw == true && ctx.Message.Attachments.Any())
            {
                await ctx.ReplyAsync("Hey now... I don't trust that image, this channel's marked NSFW...");
            }

            foreach (var uid in ctx.Message.MentionedUserIds)
            {
                var u = (ctx.Guild as SocketGuild)?.GetUser(uid) ?? ctx.Client.GetUser(uid);
                var ud = await ctx.DbContext.Users.FindAsync((long)uid);

                if (ud.TwitterId != 0)
                {
                    var tu = await TwitterUser.GetUserFromId(ud.TwitterId);
                    message = message.Replace(u.Mention, $"@{tu.ScreenName}");
                }
                else
                {
                    message = message.Replace(u.Mention, $"{u.Username}#{u.Discriminator}");
                }
            }

            string m = $"{ctx.Author.Username}#{ctx.Author.Discriminator}";
            if (ctx.UserData.TwitterId != 0)
            {
                var ta = await TwitterUser.GetUserFromId(ctx.UserData.TwitterId);
                m = $"@{ta.ScreenName}";
            }

            message = $"\"{message}\" - {m}";

            if (message.Length <= 280)
            {
                Tweetinvi.Models.ITweet tweet = null;
                var parameters = new PublishTweetOptionalParameters();

                foreach (var a in ctx.Message.Attachments)
                {
                    parameters.MediaBinaries.Add(await _httpClient.GetByteArrayAsync(a.Url));
                }

                if (reply != null)
                {
                    long id = GetTweetId(reply);
                    if (id != 0)
                    {

                        var replyTweet = await Tweet.GetTweet(id);
                        if (replyTweet != null)
                        {
                            parameters.InReplyToTweet = tweet;
                        }
                        else
                        {
                            await ctx.ReplyAsync("Can't reply to a Tweet that doesn't exist!");
                            return;
                        }
                    }
                    else
                    {
                        await ctx.ReplyAsync("Sorry! I couldn't parse that Tweet!");
                    }
                }
                else
                {
                    await ctx.EnsureBallanceAsync(20 + (ctx.Message.Attachments.Count * 5));
                    tweet = await Tweet.PublishTweet(message, parameters);
                }

                await ctx.ReplyAsync($"Here's your Tweet!\nhttps://twitter.com/WamBot_/status/{tweet.Id}");
            }
            else
            {
                await ctx.ReplyAsync("Hey! That's too long! Under 280 characters please!");
            }
        }

        private static long GetTweetId(string tweet)
        {
            long id = 0;

            if (long.TryParse(tweet, out var i))
            {
                id = i;
            }

            if (id == 0 && Uri.TryCreate(tweet, UriKind.Absolute, out var uri))
            {
                if (uri.Host == "twitter.com")
                {
                    string str = uri.Segments[uri.Segments.Length - 1];
                    long.TryParse(str, out id);
                }
            }

            return id;
        }

        [Command("Retweet", "Make me retweet a Tweet!", new[] { "retweet" })]
        public async Task PostRetweet(CommandContext ctx, string tweet)
        {
            long id = GetTweetId(tweet);

            if (id != 0)
            {
                var t = await Tweet.GetTweet(id);
                if (t != null)
                {
                    if (t.CreatedBy.ScreenName != "WamBot_")
                    {
                        if (!t.Retweeted)
                        {
                            await ctx.EnsureBallanceAsync(10);

                            var rt = await Tweet.PublishRetweet(t);
                            await ctx.ReplyAsync($"Here's your retweet!\nhttps://twitter.com/{t.CreatedBy.ScreenName}/status/{t.Id}");
                        }
                        else
                        {
                            await ctx.ReplyAsync("Hey! I can't retweet that! I already have!");
                        }
                    }
                    else
                    {
                        await ctx.ReplyAsync("Hey! I can't retweet myself! That'd be rude!");
                    }
                }
                else
                {
                    await ctx.ReplyAsync("Sorry! I couldn't find that Tweet!");
                }
            }
            else
            {
                await ctx.ReplyAsync("Sorry! I couldn't parse that Tweet!");
            }
        }
    }
}
