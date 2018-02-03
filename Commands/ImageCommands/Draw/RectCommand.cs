using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

using SixLabors.ImageSharp.Drawing.Pens;
using SixLabors.Primitives;

namespace ImageCommands.Draw
{
    class RectCommand : ModernDiscordCommand
    {
        public override string Name => "Rectangle";

        public override string Description => "Draws a rectangle on the current image";

        public override string[] Aliases => new[] { "rect" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, float x, float y, float width, float height, Rgba32? colour = null)
        {
            colour =  colour?? new Rgba32(0, 0, 0);
            if (x >= 0 && y >= 0 && width > 0 && height > 0)
            {
                RectangleF rectangle = new RectangleF(x, y, width, height);
                image.Mutate(i => i.Fill(colour.Value, rectangle));
                return Task.FromResult(image.ToResult(Context));
            }
            else
            {
                throw new ArgumentException("", "Thats not gonna work fuckwit!");
            }
        }
    }
}
