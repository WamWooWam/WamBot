using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;
using WamBot.Api;

namespace MathsCommands
{
    class OperatorParse : IParseExtension
    {
        public string Name => "Operators";

        public string Description => "Enables the use of operators within WamBot.";

        private static string[] acceptedOperators = new string[] { "+", "-", "*", "/", "^", "^^", "<<", ">>", "|", "&", "%" };

        public IEnumerable<string> Parse(IEnumerable<string> args, DiscordChannel c)
        {
            string[] parseArgs = args.Skip(1).ToArray();
            if (parseArgs.Any(a => acceptedOperators.Contains(a)) && parseArgs.Length >= 3)
            {
                var temp = parseArgs.Take(3);
                if (temp.Count() == 3 && 
                    acceptedOperators.Contains(temp.ElementAt(1)) &&
                    double.TryParse(temp.ElementAt(0), out var no1) &&
                    double.TryParse(temp.ElementAt(2), out var no2))
                {
                    double fin = double.NaN;

                    switch (temp.ElementAt(1))
                    {
                        case "+":
                            fin = no1 + no2;
                            break;
                        case "-":
                            fin = no1 - no2;
                            break;
                        case "*":
                            fin = no1 * no2;
                            break;
                        case "/":
                            fin = no1 /no2;
                            break;
                        case "^":
                            fin = Math.Pow(no1, no2);
                            break;
                        case "^^":
                            fin = (ulong)no1 ^ (ulong)no2;
                            break;
                        case "<<":
                            fin = (ulong)no1 << (int)no2;
                            break;
                        case ">>":
                            fin = (ulong)no1 >> (int)no2;
                            break;
                        case "|":
                            fin = (long)no1 | (long)no2;
                            break;
                        case "&":
                            fin = (long)no1 & (long)no2;
                            break;
                        case "%":
                            fin = no1 % no2;
                            break;
                    }

                    if(fin != double.NaN)
                    {
                        List<string> newArgs = new List<string> {args.First(), fin.ToString() };
                        newArgs.AddRange(parseArgs.Skip(3));
                        return Parse(newArgs, c);
                    }
                }
            }

            return args;
        }
    }
}
