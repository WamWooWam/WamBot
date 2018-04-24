using Discord;
using MarkovChains;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;
using WamBotRewrite.Data;
using WamWooWam.Core;
using Tweet = Tweetinvi.Tweet;
using TwitterUser = Tweetinvi.User;

namespace WamBotRewrite.Commands
{
    [RequiresGuild]
    class MarkovCommands : CommandCategory
    {
        WamBotContext markovContext = new WamBotContext();
        IStringGenerator _stringGenerator = new MarkovGenerator();

        public override string Name => "Markov Chain";

        public override string Description => "Training Markov chains off discord chatter? What could possibly go wrong?";

        const double quater = (1d / 4d);
        const double twoquaters = quater * 2;
        const double threequaters = quater * 3;

        public MarkovCommands()
        {
            if (Program.Config.Twitter.MarkovTweets)
            {
                var tweetTimer = Tools.CreateTimer(TimeSpan.FromMinutes(60), async (o, e) =>
                {
                    using (WamBotContext ctx = new WamBotContext())
                    {
                        bool tweeted = false;
                        while (!tweeted)
                        {
                            var u = ctx.Users.ElementAt(Program.Random.Next(0, ctx.Users.Count()));
                            string content = _stringGenerator.Generate(u, Program.Random.Next(10, 30));

                            await Program.LogMessage("MARKOV", $"Attempting to tweet with markov of {u?.UserId}");
                            if (u?.MarkovTwitterEnabled == true && !string.IsNullOrWhiteSpace(content))
                            {
                                var twitter = u.TwitterId != 0 ? TwitterUser.GetUserFromId(u.TwitterId) : null;
                                var discord = Program.Client.GetUser((ulong)u.UserId);
                                var mention = twitter != null ? $"@{twitter.ScreenName}" : $"{discord.Username}#{discord.Discriminator}";
                                if (DateTime.Now.IsAprilFools())
                                {
                                    content = content.Replace("n", "ny").Replace("r", "w");
                                }
                                var output = $"\"{content}\" - {mention}";
                                Tweet.PublishTweet(output);
                                tweeted = true;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                });
            }

            Program.Client.MessageReceived += Markov_MessageRecieved;
        }

        [Command("Markov Opt-in",
            "This command allows you to opt in/out of automatic markov training.",
            new[] { "markov-in", "markov-out" },
            ExtendedDescription = "You can train your markov chain regardless by using the Markov Train command!")]
        public async Task MarkovOptIn(CommandContext ctx)
        {
            if (ctx.UserData.MarkovEnabled)
            {
                try
                {
                    var message = await ctx.ReplyAndAwaitResponseAsync(
                        "Are you sure you want to opt-out of markov chain training?\n" +
                        "Your current markov chain data will not be deleted. (Y/N)");
                    if (message.Content.ToLowerInvariant().StartsWith("y"))
                    {
                        lock (markovContext)
                        {
                            ctx.UserData.MarkovEnabled = false;
                            ctx.DbContext.SaveChanges();
                            ResetContext();
                        }
                        await ctx.ReplyAsync("Opted out of automatic markov training.");
                    }
                }
                catch { }
            }
            else
            {
                lock (markovContext)
                {
                    ctx.UserData.MarkovEnabled = true;
                    ctx.DbContext.SaveChanges();
                    ResetContext();
                }

                await ctx.ReplyAsync("Opted in to automatic markov training!");
            }
        }


        [Command("Markov Tweets Opt-In",
            "This command allows you to opt in or out of hourly markov tweets.",
            new[] { "markov-tweet" },
            ExtendedDescription = "You can tweet from your markov chain regardless by using the Markov Tweet command!")]
        public async Task MarkovTwitterOptIn(CommandContext ctx)
        {
            if (ctx.UserData.MarkovTwitterEnabled)
            {
                try
                {
                    var message = await ctx.ReplyAndAwaitResponseAsync(
                        "Are you sure you want to opt-out of markov tweets?\n" +
                        "Existing tweets will not be deleted. (Y/N)");
                    if (message.Content.ToLowerInvariant().StartsWith("y"))
                    {
                        lock (markovContext)
                        {
                            ctx.UserData.MarkovTwitterEnabled = false;
                            ctx.DbContext.SaveChanges();
                            ResetContext();
                        }

                        await ctx.ReplyAsync("Opted out of markov tweets.");
                    }
                }
                catch { return; }
            }
            else
            {
                lock (markovContext)
                {
                    ctx.UserData.MarkovTwitterEnabled = true;
                    ctx.DbContext.SaveChanges();
                    ResetContext();
                }
                await ctx.ReplyAsync("Opted in to markov tweets!");
            }
        }

        [Permissions(UserPermissions = GuildPermission.ManageMessages)]
        [Command("Markov Channel Opt-out", "Allows you to disable/enable automatic markov training for the current channel.", new[] { "markov-disable", "markov-enable" })]
        public async Task MarkovChannelOut(CommandContext ctx)
        {
            lock (markovContext)
            {
                if (ctx.ChannelData.MarkovEnabled)
                {
                    ctx.ChannelData.MarkovEnabled = false;
                    ctx.DbContext.SaveChanges();
                    ResetContext();
                }
                else
                {
                    ctx.ChannelData.MarkovEnabled = true;
                    ctx.DbContext.SaveChanges();
                    ResetContext();
                }
            }

            if (!ctx.ChannelData.MarkovEnabled)
            {
                await ctx.ReplyAsync("Automatic markov training has been disabled for this channel.");
            }
            else
            {
                await ctx.ReplyAsync("Automatic markov training has been enabled for this channel!");

            }
        }

        private void ResetContext()
        {
            markovContext.Dispose();
            markovContext = new WamBotContext();
        }

        [Command("Markov Train", "Train your markov chain with a given string", new[] { "markov-train", "train" })]
        public async Task MarkovTrain(CommandContext ctx, params string[] strings)
        {
            try
            {
                _stringGenerator.Train(ctx.UserData, strings);
                await ctx.ReplyAsync("Trained!");
            }
            catch
            {
                await ctx.ReplyAsync("Hey! Something went wrong there! Try again with more words.");
            }
        }

        [Command("Markov Clear", "Clears and resets your markov chain.", new[] { "markov-reset" })]
        public async Task MarkovClear(CommandContext ctx)
        {
            _stringGenerator.Reset(ctx.UserData);
            await ctx.ReplyAsync("Reset!");
        }

        [Command("Markov", "Generates a string based on Markov chains", new[] { "markov", "mark" })]
        public async Task Markov(CommandContext ctx, [Range(1, 1000)] int length = 10, IUser user = null)
        {
            var output = _stringGenerator.Generate(user != null ? await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, UserFactory.Instance) : ctx.UserData, length);


            if (!string.IsNullOrWhiteSpace(output))
            {
                await ctx.ReplyAsync($"\"{output}\"");
            }
            else
            {
                await ctx.ReplyAsync((((user?.Id ?? ctx.Author.Id) == ctx.Author.Id) ? "You don't" : "This user doesn't") + $" have a markov chain!\n" +
                    $"Enable automatic markov training with `{Program.Config.Bot.Prefix}markov-in` or use `{Program.Config.Bot.Prefix}train` to add some training data!");
            }
        }

        [Command("Markov Arrow", "Generates a random meme arrow from a markov chain.", new[] { "arrow", ">" })]
        public async Task Markov(CommandContext ctx, IUser user = null)
        {
            var output = _stringGenerator.Generate(user != null ? await ctx.DbContext.Users.GetOrCreateAsync(ctx.DbContext, (long)user.Id, UserFactory.Instance) : ctx.UserData, Program.Random.Next(1, 4));

            if (!string.IsNullOrWhiteSpace(output))
            {
                var prefixes = new[] { "implying", "using", "mfw", "saying", "tfw" };
                var suffixes = new[] { $"in {DateTime.Now.Year}", "in current year", "at all", "on discord", $"in {Program.Random.Next(DateTime.Now.Year + 10, DateTime.Now.Year + 1000)}" };

                var suffix = suffixes[Program.Random.Next(0, suffixes.Length)];
                var prefix = prefixes[Program.Random.Next(0, prefixes.Length)];
                var rand = Program.Random.NextDouble();

                if (rand < quater)
                {
                    await ctx.ReplyAsync($">{output}");
                }
                else if (rand > quater && rand < twoquaters)
                {
                    await ctx.ReplyAsync($">{prefix} {output}");
                }
                else if (rand > twoquaters && rand < threequaters)
                {
                    await ctx.ReplyAsync($">{prefix} {output} {suffix}");
                }
                else
                {
                    await ctx.ReplyAsync($">{output} {suffix}");
                }
            }
            else
            {
                await ctx.ReplyAsync((((user?.Id ?? ctx.Author.Id) == ctx.Author.Id) ? "You don't" : "This user doesn't") + $" have a markov chain!\n" +
                    $"Enable automatic markov training with `{Program.Config.Bot.Prefix}markov-in` or use `{Program.Config.Bot.Prefix}train` to add some training data!");
            }
        }


        private Task Markov_MessageRecieved(Discord.WebSocket.SocketMessage arg)
        {
            lock (markovContext)
            {
                if (!string.IsNullOrWhiteSpace(arg.Content) && !arg.Author.IsCurrent() && !arg.Content.ToLowerInvariant().Trim().StartsWith(Program.Config.Bot.Prefix))
                {
                    var strings = arg.Content
                        .Split(c => char.IsSeparator(c) || char.IsWhiteSpace(c))
                        .Where(s => s.Any() && !s.StartsWith(Program.Config.Bot.Prefix) && !char.IsSymbol(s.First()) && s.All(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c)));

                    User data = markovContext.Users.GetOrCreate(markovContext, (long)arg.Author.Id, UserFactory.Instance);
                    Channel channel = null;

                    if (arg.Channel is IGuildChannel gc)
                    {
                        markovContext.Guilds.GetOrCreate(markovContext, (long)gc.GuildId, GuildFactory.Instance);
                        channel = markovContext.Channels.GetOrCreate(markovContext, (long)gc.Id, ChannelFactory.Instance);
                    }

                    if (strings.Count() > 2)
                    {
                        if (data.MarkovEnabled)
                        {
                            if (channel?.MarkovEnabled != false)
                            {
                                _stringGenerator.Train(data, strings.ToArray());
                                return Program.LogMessage("MARKOV", $"Trained with {JsonConvert.SerializeObject(strings)} for {arg.Author.Username}#{arg.Author.Discriminator}");
                            }
                            else
                            {
                                return Program.LogMessage("MARKOV", $"Channel #{arg.Channel.Name} has automatic training disabled.");
                            }
                        }
                        else
                        {
                            return Program.LogMessage("MARKOV", $"User {arg.Author.Username}#{arg.Author.Discriminator} has automatic training disabled.");
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
