using ImageCommands.Internals;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Draw
{
    class MeasureTextCommand : BaseDiscordCommand
    {
        public override string Name => "Measure Text";

        public override string Description => "Measures the size of text";

        public override string[] Aliases => new[] { "mestext" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x >= 1 && x <= 3;

        public override string Usage => "string text, [float size, string font]";

        public override Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            string text = args[0];
            string fontName = "Segoe UI";
            float size = 12;           

            if (args.Length >= 2)
            {
                if (!float.TryParse(args[1], out size))
                {
                    return Task.FromResult<CommandResult>($"Failed to parse size.");
                }
            }

            if (args.Length == 3)
            {
                fontName = args[2];
            }

            if (SystemFonts.TryFind(fontName, out FontFamily family))
            {
                Font font = family.CreateFont(size, FontStyle.Regular);
                SizeF bounds = TextMeasurer.Measure(text, new RendererOptions(font));

                return Task.FromResult<CommandResult>($"Width: {bounds.Width}, Height: {bounds.Height}");
            }
            else
            {
                return Task.FromResult<CommandResult>($"Unable to find font {fontName}");
            }
        }
    }
}
