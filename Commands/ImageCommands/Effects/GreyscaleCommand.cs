using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands
{
    class GreyscaleCommand : ModernDiscordCommand
    {
        public override string Name => "Greyscale";

        public override string Description => "Makes an image greyscale";

        public override string[] Aliases => new[] { "grey", "gray", "greyscale", "grayscale" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image)
        {
            image.Mutate(m => m.Grayscale());

            return Task.FromResult(image.ToResult(Context));
        }
    }
}
