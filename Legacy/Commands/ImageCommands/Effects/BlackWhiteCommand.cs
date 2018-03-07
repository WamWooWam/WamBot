using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands
{
    class BlackWhiteCommand : DiscordCommand
    {
        public override string Name => "Black & White";

        public override string Description => "It don't matter if you're black or white. Especially if you use this command, then you can be both!";

        public override string[] Aliases => new[] { "bw", "blackwhite" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image)
        {
            image.Mutate(m => m.BlackWhite());
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
