using ImageCommands.Internals;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Meme
{
    class ClippyCommand : DiscordCommand
    {
        static Rgba32 colour = new Rgba32(255, 255, 204); // clippy background colour
        static Image<Rgba32> clippyTop;
        static Image<Rgba32> clippyBottom;
        static Font font;
        static string[] characters = new[] { "dot", "hoverbot", "nature", "office", "powerpup", "scribble", "wizard" };

        public ClippyCommand()
        {
            clippyTop = Image.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "clippytop.png"));
            clippyBottom = Image.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "clippybottom.png"));

            font = SystemFonts.Find("Comic Sans MS").CreateFont(14);
        }

        public override string Name => "Clippy";

        public override string Description => "Generates an image of clippy asking a question.";

        public override string[] Aliases => new[] { "clippy", "clip", "annoyingcunt" };

        public Task<CommandResult> RunImageCommand(params string[] args)
        {
            Image<Rgba32> baseImage = Image.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", $"clippy.png"));
            try
            {
                IEnumerable<string> txtBase = args;
                if (characters.Contains(args[0].ToLowerInvariant()))
                {
                    baseImage = ChangeImage(ref txtBase);
                }

                string txt = string.Join("\r\n\r\n", txtBase);

                RectangleF size = TextMeasurer.MeasureBounds(txt, new RendererOptions(font) { WrappingWidth = clippyTop.Width - 20 });
                using (Image<Rgba32> textImage = new Image<Rgba32>(clippyTop.Width, (int)Math.Ceiling(size.Height + 5)))
                {
                    textImage.Mutate(m => m
                        .Fill(colour)
                        .DrawLines(Rgba32.Black, 2, new PointF[] { new PointF(0, 0), new PointF(0, textImage.Height) })
                        .DrawLines(Rgba32.Black, 2, new PointF[] { new PointF(textImage.Width, 0), new PointF(textImage.Width, textImage.Height) })
                        .DrawText(txt, font, Rgba32.Black, new PointF(10, 0), new TextGraphicsOptions() { WrapTextWidth = clippyTop.Width - 20 }));

                    Image<Rgba32> returnImage = new Image<Rgba32>(clippyTop.Width, clippyTop.Height + textImage.Height + clippyBottom.Height + baseImage.Height);

                    returnImage.Mutate(m => m
                        .Fill(Rgba32.Transparent)
                        .DrawImage(clippyTop, 1, new Size(clippyTop.Width, clippyTop.Height), new Point(0, 0))
                        .DrawImage(textImage, 1, new Size(textImage.Width, textImage.Height), new Point(0, clippyTop.Height))
                        .DrawImage(clippyBottom, 1, new Size(clippyBottom.Width, clippyBottom.Height), new Point(0, clippyTop.Height + textImage.Height))
                        .DrawImage(baseImage, 1, new Size(baseImage.Width, baseImage.Height), new Point((clippyTop.Width - baseImage.Width) / 2, clippyTop.Height + textImage.Height + clippyBottom.Height)));

                    return Task.FromResult(returnImage.ToResult(Context));
                }
            }
            finally
            {
                baseImage?.Dispose();
            }
        }

        private static Image<Rgba32> ChangeImage(ref IEnumerable<string> txtBase)
        {
            Image<Rgba32> baseImage = Image.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", $"{txtBase.ElementAt(0)}.png"));
            txtBase = txtBase.Skip(1);
            return baseImage;
        }
    }
}
