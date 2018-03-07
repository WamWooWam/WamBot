using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Converters
{
    class FontConverter : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(FontFamily) };

        public Task<object> Convert(string arg, Type to, CommandContext context)
        {
            if (SystemFonts.TryFind(arg, out FontFamily family))
            {
                return Task.FromResult<object>(family);
            }
            else
            {
                throw new CommandException($"Font \"{arg}\" does not exist!");
            }
        }
    }
}
