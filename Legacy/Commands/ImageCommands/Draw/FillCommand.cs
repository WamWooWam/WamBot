using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Draw
{
    class FillCommand : DiscordCommand
    {
        public override string Name => "Fill";

        public override string Description => "Fills and clears the canvas with a specified colour";

        public override string[] Aliases => new[] { "fill", "clear" };

        [Command]
        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, Rgba32 colour)
        {
            image.Mutate(m => m.Fill(colour));
            return Task.FromResult(image.ToResult(Context));
        }
    }
}
