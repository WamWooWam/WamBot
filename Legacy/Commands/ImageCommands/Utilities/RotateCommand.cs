using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ImageCommands.Internals;
using SixLabors.ImageSharp;
using WamBot.Api;

namespace ImageCommands.Utilities
{
    class RotateCommand : DiscordCommand
    {
        public override string Name => "Rotate";

        public override string Description => "Rotates an image";

        public override string[] Aliases => new[] { "rotate", "rot" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, float degrees = 90)
        {
            image.Mutate(m => m.Rotate(degrees));
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
