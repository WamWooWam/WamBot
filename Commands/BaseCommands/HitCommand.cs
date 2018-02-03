using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    class HitCommand : ModernDiscordCommand
    {
        static Random rand = new Random();

        public override string Name => "Hit";

        public override string Description => "Hits WamBot";

        public override string[] Aliases => new[] { "hit", "punch", "hurt" };

        public CommandResult Run()
        {
            Context.Happiness -= 10;
            if (Context.HappinessLevel >= HappinessLevel.Like)
            {
                switch (rand.Next(0, 6))
                {
                    case 0:
                        return ("Ouch!");
                    case 1:
                        return ("How could you!?");
                    case 2:
                        return ("What did I do?!");
                    case 3:
                        return ("I'm sorry! I promise I'll do better!");
                    case 4:
                        return ("No way!");
                    default:
                        return ("What?!");
                }
            }
            else
            {
                switch (rand.Next(0, 6))
                {
                    case 0:
                        return ("Ow!");
                    case 1:
                        return ("That hurts!");
                    case 2:
                        return ("Ouch!");
                    case 3:
                        return ("Help me!");
                    case 4:
                        return ("Fuck off!");
                    default:
                        return ("Shit!");
                }
            }
        }
    }
}
