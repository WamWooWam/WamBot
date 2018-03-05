using ImageCommands.Internals;
using SixLabors.ImageSharp;
using SixLabors.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Draw
{
    class PolygonCommand : DiscordCommand
    {
        public override string Name => "Polygon";

        public override string Description => "Draws a triangle on the current image";

        public override string[] Aliases => new[] { "poly" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, float x, float y, int vertecies, float radius, Rgba32? colour = null)
        {
            colour = colour ?? new Rgba32(0, 0, 0);
            if (x >= 0 && y >= 0 && vertecies > 0 && radius > 0)
            {
                RegularPolygon poly = new RegularPolygon(new SixLabors.Primitives.PointF(x + radius, y + radius), vertecies, radius);
                image.Mutate(i => i.Fill(colour.Value, poly));
                return Task.FromResult(image.ToResult(Context));
            }
            else
            {
                throw new ArgumentException("", "Thats not gonna work fuckwit!");
            }
        }
    }
}
