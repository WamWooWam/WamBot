using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Effects
{
    class OilPaintCommand : DiscordCommand
    {
        public override string Name => "Oil Paint";

        public override string Description => "Applies an oil paint effect to an image.";

        public override string[] Aliases => new[] { "oil", "oilpaint" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image,int levels = 30, int size = 10)
        {
            image.Mutate(i => i.OilPaint(levels, size));
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
