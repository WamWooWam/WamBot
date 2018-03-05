using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands
{
    public class ResizeImageCommand : DiscordCommand
    {

        public override string Name => "Resize Image";

        public override string Description => "Resizes an image to a specified width and height, or by a percentage.";

        public override string[] Aliases => new[] { "resize" };
        
        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, double percentage)
        {
            int width = 0;
            int height = 0;

            double percent = percentage / 100d;
            width = (int)(image.Width * percent);
            height = (int)(image.Height * percent);

            if (width <= 8192 && height <= 8192)
            {
                image.Mutate(i =>
                {
                    i.Resize(width, height);
                });

                return Task.FromResult(image.ToResult(Context));
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image, int width, int height)
        {
            if (width <= 8192 && height <= 8192)
            {
                image.Mutate(i =>
                {
                    i.Resize(width, height);
                });

                return Task.FromResult(image.ToResult(Context));
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
