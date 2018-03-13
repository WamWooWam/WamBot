using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api.Converters
{
    class ByteArrayConverter : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(byte[]) };

        public Task<object> Convert(string arg, Type to, CommandContext context)
        {
            return Task.FromResult<object>(System.Convert.FromBase64String(arg));
        }
    }
}
