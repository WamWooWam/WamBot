using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    class PetCommand : ModernDiscordCommand
    {
        public override string Name => "Pet";

        public override string Description => "Pets WamBot!";

        public override string[] Aliases => new[] { "pet" };

        private static Random _random = new Random();

        public CommandResult Run()
        {
            string[] sadResponses = new string[] { "No.", "I'm not in the mood for this.", "Better than `hit` i suppose...", "I can't be bothered.", "Fuck off", $"You can fuck right off." };
            string[] dislikeResponses = new string[] { "Do I have to?", "Why now??", "You're not great at this.", "I can't be bothered.", $"Thanks I guess.", "Ugh", "I don't wanna know where that hand's been." };
            string[] regularResponses = new string[] { "Mew.", @"\*purrs\*", "<3", "You're getting good at this!", $"Thank you, {Context.Message.Author.Username}!" };
            string[] happyResponses = new string[] { "Mew!", @"\*purrs happily\*", $"I <3 {Context.Message.Author.Username}", "You're the best!", $"Thanks a ton, {Context.Message.Author.Username}!" };

            switch (Context.HappinessLevel)
            {
                case HappinessLevel.Hate:
                    Context.Happiness += 2;
                    return (sadResponses[_random.Next(sadResponses.Length)]);
                case HappinessLevel.Dislike:
                    Context.Happiness += 3;
                    return (dislikeResponses[_random.Next(dislikeResponses.Length)]);
                case HappinessLevel.Indifferent:
                case HappinessLevel.Like:
                    Context.Happiness += 4;
                    return (regularResponses[_random.Next(regularResponses.Length)]);
                case HappinessLevel.Adore:
                    Context.Happiness += 4;
                    return (happyResponses[_random.Next(happyResponses.Length)]);
            }

            return ("Mew.");
        }
    }
}
