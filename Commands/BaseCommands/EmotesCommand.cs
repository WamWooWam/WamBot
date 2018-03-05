using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    [Owner]
    class EmotesCommand : DiscordCommand
    {
        public override string Name => "Emotes";

        public override string Description => "Lists every emote WamBot can see/use.";

        public override string[] Aliases => new[] { "emotes", "emoji" };

        public CommandResult Run()
        {
            StringBuilder emojis = new StringBuilder();
            foreach (DiscordEmoji emoji in Context.Client.Guilds.Values.SelectMany(g => g.Emojis))
            {
                emojis.Append($"{emoji} ");
            }

            return $"Available emotes: {emojis}";
        }
    }
}
