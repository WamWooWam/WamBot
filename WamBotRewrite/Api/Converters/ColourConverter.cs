using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api.Converters
{
    class ColourConverter : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(Rgba32), typeof(Rgba32?) };

        public Task<object> Convert(string arg, Type to, CommandContext context)
        {
            string str = arg.TrimStart('#');
            if (uint.TryParse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint n))
            {
                return Task.FromResult<object>(Rgba32.FromHex(str));
            }
            else
            {
                if (arg.ToLowerInvariant() == "accent")
                {
                    return Task.FromResult<object>(new Rgba32(Program.AccentColour.R, Program.AccentColour.G, Program.AccentColour.B));
                }
                else
                {
                    var f = typeof(Rgba32).GetFields().FirstOrDefault(re => re.Name.ToLowerInvariant() == arg.ToLowerInvariant());
                    if (f != null)
                    {
                        return Task.FromResult(f.GetValue(null));
                    }
                    else
                    {
                        throw new CommandException("That's not a colour you numpty!");
                    }
                }
            }
        }
    }
}
