using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api.Converters
{
    class ByteArrayConverter : IParamConverter
    {
        public Type[] AcceptedTypes => new[] { typeof(byte[]) };

        public Task<object> Convert(string arg, ParameterInfo to, CommandContext context)
        {
            return Task.FromResult<object>(arg != null ? System.Convert.FromBase64String(arg) : null);
        }
    }
}
