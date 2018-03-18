using Discord;
using MarkovChains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;
using WamWooWam.Core;

namespace WamBotRewrite.Commands
{
    class MarkovCommands : CommandCategory
    {
        public override string Name => "Markov Chain";

        public override string Description => "Training Markov chains off discord chatter? What could possibly go wrong?\n\n" +
            "If your markov chain randomly vanishes, there was more than likely an issue while saving chains before exit, something only done because people wouldn't shut up about \"datamining\", if there's anyone to blame, it's them.";

        [Command("Markov Opt-in",
            "This command allows you to opt in/out of automatic markov training.",
            new[] { "markovin", "markovout" },
            ExtendedDescription = "You can train your markov chain regardless by using the Markov Train command.")]
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
                        ctx.UserData.MarkovEnabled = false;
                        await ctx.ReplyAsync("Opted out of automatic markov training.");
                    }
                }
                catch { }
            }
            else
            {
                ctx.UserData.MarkovEnabled = true;
                await ctx.ReplyAsync("Opted in to automatic markov training!");
            }
        }

        [Command("Markov Train", "Train your markov chain with a given string", new[] { "train" })]
        public async Task MarkovTrain(CommandContext ctx, params string[] strings)
        {
            if (Program.Markovs.TryGetValue(ctx.Author.Id, out var m))
            {
                m.Train(strings.ToList(), 2);
                Program.MarkovList[ctx.Author.Id].AddRange(strings);

                await ctx.ReplyAsync("Added to your markov chain!");
            }
            else
            {
                Markov<string> markov = new Markov<string>(".");
                markov.Train(strings.ToList(), 2);

                var markovList = new List<string>();
                markovList.AddRange(strings);

                Program.MarkovList[ctx.Author.Id] = markovList;
                Program.Markovs[ctx.Author.Id] = markov;
            }
        }

        [Command("Markov Clear", "Clears and resets your markov chain.", new[] { "clear" })]
        public async Task MarkovClear(CommandContext ctx)
        {
            if (Program.Markovs.TryGetValue(ctx.Author.Id, out var m))
            {
                Program.MarkovList[ctx.Author.Id].Clear();
                Program.Markovs[ctx.Author.Id] = new Markov<string>(".");
                await ctx.ReplyAsync("Reset your markov chain!");
            }
            else
            {
                await ctx.ReplyAsync("You don't have a chain to reset!");
            }
        }

        [Command("Markov", "Generates a string based on Markov chains", new[] { "markov", "mark" })]
        public async Task Markov(CommandContext ctx, int length = 10, IUser user = null)
        {
            if (Program.Markovs.TryGetValue(user?.Id ?? ctx.Author.Id, out var markov))
            {
                var output = markov.Generate(length, " ", true);
                string str = string.Join(" ", output);
                await ctx.ReplyAsync($"\"{str.Truncate(1996)}\"");
            }
            else
            {
                await ctx.ReplyAsync($"You don't have a markov chain!\n" +
                    $"Enable automatic markov training with {Program.Config.Prefix}markovin or use {Program.Config.Prefix}train to add some training data!");
            }
        }        
    }
}
