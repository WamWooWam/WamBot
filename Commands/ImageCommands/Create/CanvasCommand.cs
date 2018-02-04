using ImageCommands.Internals;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamCash.Intergration;

namespace ImageCommands.Create
{
    class CanvasCommand : ModernDiscordCommand
    {
        public override string Name => "Canvas";

        public override string Description => "Creates a new blank image";

        public override string[] Aliases => new[] { "create", "new" };

        public async Task<CommandResult> RunCommand(int width = 300, int height = 300, Rgba32? color = null)
        {
            if (width <= 4096 && height <= 4096)
            {
                //if (width > 8192 && height > 8192)
                //{
                //    decimal thing = (((decimal)width + (decimal)height) / 2M) / ushort.MaxValue;
                //    await AccountsIntergration.EnsureBallanceAsync(Context, 4 * thing);
                //}

                if (ImageCommandResult.ImageCache.TryGetValue(Context.Guild.Id, out var oldImage))
                {
                    oldImage.Dispose();
                }

                Image<Rgba32> newImage = new Image<Rgba32>(width, height);
                newImage.Mutate(m => m.Fill(color ?? new Rgba32(255, 255, 255)));

                return newImage.ToResult(Context);
            }
            else
            {
                throw new CommandException("That image is way too big! Calm your tits there!");
            }
        }
    }
}
