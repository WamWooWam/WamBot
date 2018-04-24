using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace WamBotRewrite.Api
{
    public abstract class CommandCategory
    {
        private static HttpClient _httpClient = new HttpClient();

        public static HttpClient HttpClient => _httpClient;

        public abstract string Name { get; }
        public abstract string Description { get; }

        internal IEnumerable<CommandRunner> GetCommands()
        {
            var methods = GetType()
                .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(CommandAttribute)))
                .Where(m => m.ReturnType == typeof(Task) || m.ReturnType == typeof(void))
                .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(CommandContext)))
                .Select(m => new CommandRunner(m, this));

            return methods;
        }
    }
}
