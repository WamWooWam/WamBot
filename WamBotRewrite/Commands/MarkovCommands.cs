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

        internal static Dictionary<ulong, Markov<string>> Markovs { get; private set; } = new Dictionary<ulong, Markov<string>>();
        internal static Dictionary<ulong, List<string>> MarkovList { get; private set; } = new Dictionary<ulong, List<string>>();

        public override string Name => "Markov Chain";

        public override string Description => "Training Markov chains off discord chatter? What could possibly go wrong?\n\n" +
            "If your markov chain randomly vanishes, there was more than likely an issue while saving chains before exit, something only done because people wouldn't shut up about \"datamining\", if there's anyone to blame, it's them.";

        public MarkovCommands()
        {
            if (File.Exists("markov.json"))
            {
                MarkovList = JsonConvert.DeserializeObject<Dictionary<ulong, List<string>>>(File.ReadAllText("markov.json"));
                //File.Delete("markov.json");

                foreach (var pair in MarkovList)
                {
                    Markov<string> markov = new Markov<string>(".");
                    markov.Train(pair.Value, 2);
                    Markovs[pair.Key] = markov;
                }
            }

#if RELEASE
            var tweetTimer = Tools.CreateTimer(TimeSpan.FromMinutes(60), async (o, e) =>
            {
                using (WamBotContext ctx = new WamBotContext())
                {
                    bool tweeted = false;
                    while (!tweeted)
                    {
                        var m = Markovs.ElementAt(Program.Random.Next(Markovs.Count));
                        var u = ctx.Users.Find((long)m.Key);

                        await Program.LogMessage("MARKOV", $"Attempting to tweet with markov of {u?.UserId}");

                        if (u?.MarkovTwitterEnabled == true)
                        {
                            var twitter = u.TwitterId != 0 ? TwitterUser.GetUserFromId(u.TwitterId) : null;
                            var discord = Program.Client.GetUser((ulong)u.UserId);
                            var mention = twitter != null ? $"@{twitter.ScreenName}" : $"{discord.Username}#{discord.Discriminator}";
                            string content = m.Value.Generate(Program.Random.Next(10, 30), " ", true);
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
#endif
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
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
            if (Markovs.TryGetValue(ctx.Author.Id, out var m))
            {
                m.Train(strings.ToList(), 2);
                MarkovList[ctx.Author.Id].AddRange(strings);

                await ctx.ReplyAsync("Added to your markov chain!");
            }
            else
            {
                Markov<string> markov = new Markov<string>(".");
                markov.Train(strings.ToList(), 2);

                var markovList = new List<string>();
                markovList.AddRange(strings);

                MarkovList[ctx.Author.Id] = markovList;
                Markovs[ctx.Author.Id] = markov;
            }
        }

        [Command("Markov Clear", "Clears and resets your markov chain.", new[] { "markov-reset" })]
        public async Task MarkovClear(CommandContext ctx)
        {
            if (Markovs.TryGetValue(ctx.Author.Id, out var m))
            {
                MarkovList[ctx.Author.Id].Clear();
                Markovs[ctx.Author.Id] = new Markov<string>(".");
                await ctx.ReplyAsync("Reset your markov chain!");
            }
            else
            {
                await ctx.ReplyAsync("You don't have a chain to reset!");
            }
        }

        [Command("Markov", "Generates a string based on Markov chains", new[] { "markov", "mark" })]
        public async Task Markov(CommandContext ctx, [Range(1, 1000)] int length = 10, IUser user = null)
        {
            if (Markovs.TryGetValue(user?.Id ?? ctx.Author.Id, out var markov))
            {
                var output = markov.Generate(length, " ", true);
                string str = string.Join(" ", output);
                await ctx.ReplyAsync($"\"{str}\"");
            }
            else
            {
                await ctx.ReplyAsync((((user?.Id ?? ctx.Author.Id) == ctx.Author.Id) ? "You don't" : "This user doesn't") + $" have a markov chain!\n" +
                    $"Enable automatic markov training with `{Program.Config.Prefix}markov-in` or use `{Program.Config.Prefix}train` to add some training data!");
            }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            File.WriteAllText("markov.json", JsonConvert.SerializeObject(MarkovList));
        }

        private Task Markov_MessageRecieved(Discord.WebSocket.SocketMessage arg)
        {
            lock (markovContext)
            {
                if (!string.IsNullOrWhiteSpace(arg.Content) && !arg.Author.IsCurrent() && !arg.Content.ToLowerInvariant().Trim().StartsWith(Program.Config.Prefix))
                {
                    var strings = arg.Content
                        .Split(c => char.IsSeparator(c) || char.IsWhiteSpace(c))
                        .Where(s => s.Any() && !s.StartsWith(Program.Config.Prefix) && !char.IsSymbol(s.First()) && s.All(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c)));

                    User data = markovContext.Users.GetOrCreate(markovContext, (long)arg.Author.Id, () => new User(arg.Author));
                    Channel channel = null;

                    if (arg.Channel is IGuildChannel gc)
                    {
                        markovContext.Guilds.GetOrCreate(markovContext, (long)gc.GuildId, () => new Guild(gc.Guild));
                        channel = markovContext.Channels.GetOrCreate(markovContext, (long)gc.Id, () => new Channel(gc));
                    }

                    if (strings.Count() > 2)
                    {
                        if (data.MarkovEnabled)
                        {
                            if (channel?.MarkovEnabled != false)
                            {
                                if (Markovs.TryGetValue(arg.Author.Id, out Markov<string> m))
                                {
                                    m.Train(strings.ToList(), 2);
                                    MarkovList[arg.Author.Id].AddRange(strings);
                                }
                                else
                                {
                                    Markov<string> markov = new Markov<string>(".");
                                    markov.Train(strings.ToList(), 2);

                                    var markovList = new List<string>();
                                    markovList.AddRange(strings);

                                    MarkovList[arg.Author.Id] = markovList;
                                    Markovs[arg.Author.Id] = markov;
                                }

                                return Program.LogMessage("MARKOV", $"Added {JsonConvert.SerializeObject(strings)} to Markov for {arg.Author.Username}#{arg.Author.Discriminator}");
                            }
                            else
                            {
                                return Program.LogMessage("MARKOV", $"Channel #{arg.Channel.Name} has automatic markov training disabled.");
                            }
                        }
                        else
                        {
                            return Program.LogMessage("MARKOV", $"User {arg.Author.Username}#{arg.Author.Discriminator} has automatic markov training disabled.");
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
