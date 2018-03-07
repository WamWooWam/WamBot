using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ImageCommands.Internals;
using SixLabors.ImageSharp;
using WamBot.Api;

namespace ImageCommands.Effects
{
    class PixelateCommand : DiscordCommand
    {
        public override string Name => "Pixelate";

        public override string Description => "Pixelates an image";

        public override string[] Aliases => new[] { "pixel", "pixelate" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, int size = 5)
        {
            image.Mutate(m => m.Pixelate(size));
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
