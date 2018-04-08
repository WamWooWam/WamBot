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
                Reddit r = new Reddit();
                var s = await r.GetSubredditAsync(subName);

                var ps = await s.GetPosts(Subreddit.Sort.Hot, 200).ToList();
                var p = ps.ElementAt(Program.Random.Next(0, ps.Count));

                if (s.NSFW != true || ctx.Channel is IDMChannel || (ctx.Channel is ITextChannel c && c.IsNsfw))
                {
                    EmbedBuilder builder = new EmbedBuilder()
                        .WithAuthor($"{p.Title} - /r/{subName}")
                        .WithColor(Program.AccentColour)
                        .WithTimestamp(new DateTimeOffset(p.Created))
                        .WithFooter($"By /u/{p.AuthorName}");

                    builder.Author.Url = p.Shortlink.ToString();

                    if (!string.IsNullOrWhiteSpace(p.SelfText))
                    {
                        builder.WithDescription(HttpUtility.HtmlDecode(p.SelfText));
                    }

                    if (p.Thumbnail != null && (p.Thumbnail.IsAbsoluteUri || p.Url.IsAbsoluteUri))
                    {
                        var resp = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, p.Url));
                        if (!resp.Content.Headers.ContentType.MediaType.StartsWith("text"))
                        {
                            builder.WithImageUrl(p.Url.ToString());
                        }
                        else if (p.Thumbnail.IsAbsoluteUri)
                        {
                            builder.WithThumbnailUrl(p.Thumbnail.ToString());
                            builder.AddField("Url", p.Url.ToString());
                        }
                        else
                        {
                            await ctx.ReplyAsync(p.Shortlink.ToString());
                            return;
                        }
                    }
                    else
                    {
                        builder.AddField("Url", p.Url.ToString());
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
