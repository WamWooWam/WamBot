using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Effects
{
    class SharpenCommand : ModernDiscordCommand
    {
        public override string Name => "Sharpen";

        public override string Description => "Sharpens an image";

        public override string[] Aliases => new[] { "sharpen" };

        public Task<CommandResult> Run([Implicit] Image<Rgba32> image, float delta = 5)
        {
            image.Mutate(m => m.GaussianSharpen(delta));
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
