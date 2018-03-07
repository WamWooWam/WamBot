using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    [TriggersTyping]
    class DiceCommand : DiscordCommand
    {
        private Random _random;

        public DiceCommand()
        {
            _random = new Random();
        }

        public override string Name => "Dice";

        public override string Description => "Take a risk, roll the dice.";

        public override string[] Aliases => new[] { "dice", "d", "risk", "roll" };

        public CommandResult Run(int count, int max)
        {
            return RunDice(Context, $"{count}d{max}", count, max);
        }

        public CommandResult Run(string str)
        {
            string d = str;
            string[] splitd = d.Split('d');

            if (splitd.Length == 2 && int.TryParse(splitd[0], out int count) && int.TryParse(splitd[1], out int max))
            {
                return RunDice(Context, d, count, max);
            }

            return "Hey! Something's very wrong with your input! Try again.";
        }

        private string RunDice(CommandContext context, string d, int count, int max)
        {
            context.Happiness += 2;
            if (count > 0 && max > 0)
            {
                if (count <= 4096)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append($"{context.Author.Username} rolled {d} and got: ");

                    for (int i = 0; i < count - 1; i++)
                    {
                        builder.Append($"{_random.Next(max)}, ");
                    }

                    builder.Append(_random.Next(max));
                    builder.Append("!");
                    return builder.ToString();
                }
                else
                {
                    return "Yeah more than 4096 dice is probably not a great idea let's be honest.";
                }
            }
            else
            {
                return "I can't generate a negative number of dice you twat.";
            }
        }
    }
}
