using ImageCommands.Internals;
using SixLabors.ImageSharp;
using SixLabors.Shapes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Draw
{
    class EllipseCommand : ModernDiscordCommand
    {
        public override string Name => "Ellipse";

        public override string Description => "Draws an ellipse on the current image.";

        public override string[] Aliases => new[] { "ellipse" };
        
        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, float x, float y, float radius, Rgba32? colour = null)
        {
            if (x >= 0 && y >= 0 && radius > 0)
            {
                EllipsePolygon ellipse = new EllipsePolygon(x, y, radius);
                image.Mutate(i => i.Fill(colour ?? new Rgba32(0, 0, 0), ellipse));
                return Task.FromResult(image.ToResult(Context));
            }
            else
            {
                throw new CommandException("Something doesn't look right with that input! Check and try again!");
            }
        }

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, float x, float y, float width, float height, Rgba32? colour = null)
        {
            if (x >= 0 && y >= 0 && width > 0 && height > 0)
            {
                EllipsePolygon ellipse = new EllipsePolygon(x, y, width, height);
                image.Mutate(i => i.Fill(colour ?? new Rgba32(0, 0, 0), ellipse));
                return Task.FromResult(image.ToResult(Context));
            }
            else
            {
                throw new CommandException("Something doesn't look right with that input! Check and try again!");
            }
        }
    }

}
