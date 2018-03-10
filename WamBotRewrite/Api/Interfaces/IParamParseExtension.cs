using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api
{
    public interface IParamConverter
    {
        Type[] AcceptedTypes { get; }

        Task<object> Convert(string arg, Type to, CommandContext context);
    }
}
