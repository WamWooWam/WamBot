using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Utilities
{
    class OpacityCommand : ModernDiscordCommand
    {
        public override string Name => "Opacity";

        public override string Description => "Adjusts an image's overall opacity.";

        public override string[] Aliases => new[] { "opacity", "alpha" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, float opacity)
        {
            image.Mutate(i => i.Alpha(opacity));
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
