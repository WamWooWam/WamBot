using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WamBot.Api;

namespace Variables
{
    class VariableParseExtension : IParseExtension
    {
        public string Name => "Variables";
        public string Description => "Enables variables witin arguments.";

        private static Regex _regex = new Regex(@"%(\w*\d*?)%");

        public IEnumerable<string> Parse(IEnumerable<string> args, DiscordChannel c)
        {
            if (!c.IsPrivate)
            {
                for (int i = 0; i < args.Count(); i++)
                {
                    yield return _regex.Replace(args.ElementAt(i), m =>
                    {
                        if (Variables.GetVariable(c.GuildId + m.Groups[1].Value, out object variable))
                        {
                            return variable.ToString();
                        }

                        return m.Value;
                    });
                }
            }
            else
            {
                foreach (var item in args)
                {
                    yield return item;
                }
            }
        }
    }
}
