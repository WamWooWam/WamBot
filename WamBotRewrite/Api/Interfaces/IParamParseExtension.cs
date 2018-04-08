using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api
{
    public interface IParamConverter
    {
        Type[] AcceptedTypes { get; }

        Task<object> Convert(string arg, ParameterInfo param, CommandContext context);
    }
}
