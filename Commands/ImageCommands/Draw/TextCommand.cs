using ImageCommands.Internals;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Draw
{
    class TextCommand : ModernDiscordCommand
    {
        public override string Name => "Text";

        public override string Description => "Draws text onto an image";

        public override string[] Aliases => new[] { "text", "txt" };

        public Task<CommandResult> RunImageCommand([Implicit]Image<Rgba32> image, string text, Rgba32? color = null, float fontSize = 14, FontFamily family = null, FontStyle style = FontStyle.Regular, float? x = null, float? y = null)
        {
            color = color ?? new Rgba32(0, 0, 0);
            family = family ?? SystemFonts.Find("Segoe UI");
            Font font = family.CreateFont(fontSize, FontStyle.Regular);
            SizeF bounds = TextMeasurer.Measure(text, new RendererOptions(font) { ApplyKerning = true, WrappingWidth = image.Width - 20 });

            x = x ?? (image.Width - bounds.Width) / 2;
            y = y ?? (image.Height - bounds.Height) / 2;

            TextGraphicsOptions options = new TextGraphicsOptions(true)
            {
                ApplyKerning = true,
                WrapTextWidth = image.Width - 20,
                Antialias = true,
                AntialiasSubpixelDepth = 4
            };

            image.Mutate(m => m.DrawText(text, font, color.Value, new PointF(x.Value, y.Value), options));

            return Task.FromResult(image.ToResult(Context));
        }
    }
}
