using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Converters
{
    class ColourConverter : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(Rgba32), typeof(Rgba32?) };

        public Task<object> Convert(string arg, Type to, CommandContext context)
        {
            string str = arg.TrimStart('#');
            if (uint.TryParse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint n))
            {
                var c = System.Drawing.Color.FromArgb((int)n);
                return Task.FromResult<object>(new Rgba32(c.R, c.G, c.B, c.A));
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}
