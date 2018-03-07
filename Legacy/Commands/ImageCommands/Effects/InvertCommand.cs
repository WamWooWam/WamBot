using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ImageCommands.Internals;
using SixLabors.ImageSharp;
using WamBot.Api;

namespace ImageCommands.Effects
{
    class InvertCommand : DiscordCommand
    {
        public override string Name => "Invert";

        public override string Description => "Inverts an image";

        public override string[] Aliases => new[] { "invert" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image)
        {
            image.Mutate(m => m.Invert());
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
