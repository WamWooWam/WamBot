using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WamCash.Intergration;

namespace ImageCommands
{
    class GaussuanBlurCommand : ModernDiscordCommand
    {
        public override string Name => "Gaussian Blur";

        public override string Description => "Gaussian blurs an image";

        public override string[] Aliases => new[] { "blur", "gaussian" };

        public async Task<CommandResult> RunImageCommandAsync([Implicit] Image<Rgba32> image, int radius = 10, BlurType type = BlurType.Gaussian)
        {
            if (radius <= 250)
            {
                if ((image.Width > 8192 && image.Height > 8192) || radius > 100)
                {
                    decimal thing = (((decimal)(image.Width + image.Height + (radius * 2))) / 2M) / short.MaxValue;
                    await AccountsIntergration.EnsureBallanceAsync(Context, 8 * thing);
                }

                image.Mutate(m =>
                {
                    if (type == BlurType.Box)
                    {
                        m.BoxBlur(radius);
                    }
                    else
                    {
                        m.GaussianBlur(radius);
                    }
                });

                return image.ToResult(Context);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum BlurType
    {
        Gaussian,
        Box
    }
}
