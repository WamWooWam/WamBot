using Discord;
using Newtonsoft.Json.Linq;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WamBotRewrite.Api;

namespace WamBotRewrite.Commands
{
    class APICommands : CommandCategory
    {
        public override string Name => "API Commands";

        public override string Description => "Commands that pull data from external APIs";

        [Command("Reddit", "Pulls a random post from a specified subreddit", new[] { "reddit" })]
        public async Task Reddit(CommandContext ctx, string subName = "random")
        {
            try
            {
                var reddit = new Reddit();
                var sub = await reddit.GetSubredditAsync(subName);

                var posts = await sub.GetPosts(Subreddit.Sort.Hot, 200).ToList();
                var post = posts.ElementAt(Program.Random.Next(0, posts.Count));

                if (sub.NSFW != true || ctx.Channel is IDMChannel || (ctx.Channel is ITextChannel c && c.IsNsfw))
                {
                    var builder = new EmbedBuilder()
                        .WithAuthor($"{post.Title} - /r/{subName}")
                        .WithColor(Program.AccentColour)
                        .WithTimestamp(new DateTimeOffset(post.Created.ToUniversalTime()))
                        .WithFooter($"By /u/{post.AuthorName}");

                    builder.Author.Url = post.Shortlink.ToString();

                    if (!string.IsNullOrWhiteSpace(post.SelfText))
                    {
                        builder.WithDescription(HttpUtility.HtmlDecode(post.SelfText));
                    }

                    if (post.Thumbnail != null && (post.Thumbnail.IsAbsoluteUri || post.Url.IsAbsoluteUri))
                    {
                        var resp = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, post.Url));
                        if (!resp.Content.Headers.ContentType.MediaType.StartsWith("text"))
                        {
                            builder.WithImageUrl(post.Url.ToString());
                        }
                        else if (post.Thumbnail.IsAbsoluteUri)
                        {
                            builder.WithThumbnailUrl(post.Thumbnail.ToString());
                            builder.AddField("Url", post.Url.ToString());
                        }
                        else
                        {
                            await ctx.ReplyAsync(post.Shortlink.ToString());
                            return;
                        }
                    }
                    else
                    {
                        builder.AddField("Url", post.Url.ToString());
                    }

                    await ctx.ReplyAsync(emb: builder.Build());
                }
                else
                {
                    await ctx.ReplyAsync("Hey! I can't show this here, this channel is SFW!");
                }
            }
            catch(Exception)
            {
                await ctx.ReplyAsync("Sorry! That didn't work! Please try again!");
            }
        }
    }
}
