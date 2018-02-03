using System;
using System.Threading.Tasks;
using ImageCommands.Internals;
using SixLabors.ImageSharp;
using WamBot.Api;

namespace ImageCommands
{
    class SepiaCommand : ModernDiscordCommand
    {
        public override string Name => "Sepia";

        public override string Description => "Makes an image sepia";

        public override string[] Aliases => new[] { "sepia" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image)
        {
            image.Mutate(i => i.Sepia());
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
