using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands
{
    class SaturatonCommand : DiscordCommand
    {
        public override string Name => "Saturation";

        public override string Description => "Adjusts the saturation of an image";

        public override string[] Aliases => new[] { "sat", "saturation" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, int sat = 5)
        {
            image.Mutate(m => m.Saturation(sat));
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
