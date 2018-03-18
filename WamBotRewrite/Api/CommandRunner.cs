using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO.Pipes;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using WamBotRewrite.Api.Pipes;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace WamBotRewrite.Api
{
    /// <summary>
    /// A class that wraps methods allowing them to be called easier
    /// </summary>
    internal class CommandRunner
    {
        private static ConcurrentDictionary<MethodInfo, ParameterInfo[]> _parameterCache = new ConcurrentDictionary<MethodInfo, ParameterInfo[]>();
        private static Dictionary<Type, string> _typeKeywords = new Dictionary<Type, string>()
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(object), "object" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };

        private static JSchemaGenerator _schemaGenerator = new JSchemaGenerator();
        private static JSchema _exceptionSchema = _schemaGenerator.Generate(typeof(Exception));

        internal MethodInfo _method;
        private CommandAttribute _commandAttribute;
        private PermissionsAttribute _permissionsAttribute;
        private RunOutOfProcessAttribute _outOfProcessAttribute;
        private bool _ownerOnly;
        private bool _ignoreArguments;
        private bool _requiresGuild;

        public CommandRunner(MethodInfo method, CommandCategory category)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
            Category = category ?? throw new ArgumentNullException(nameof(category));

            var attributes = method.GetCustomAttributes(true).ToList();
            attributes.AddRange(method.DeclaringType.GetCustomAttributes(true));

            _commandAttribute = (CommandAttribute)attributes.FirstOrDefault(a => a is CommandAttribute);
            if (_commandAttribute == null)
            {
                throw new ArgumentException("Method is not a command method");
            }

            _permissionsAttribute = (PermissionsAttribute)attributes.FirstOrDefault(a => a is PermissionsAttribute);
            _outOfProcessAttribute = (RunOutOfProcessAttribute)attributes.FirstOrDefault(a => a is RunOutOfProcessAttribute);
            _ownerOnly = attributes.Any(a => a is OwnerOnlyAttribute);
            _ignoreArguments = attributes.Any(a => a is IgnoreArgumentsAttribute);
            _requiresGuild = attributes.Any(a => a is RequiresGuildAttribute);
        }

        public string Name => _commandAttribute.Name;
        public string Description => _commandAttribute.Description;
        public string ExtendedDescription => _commandAttribute.ExtendedDescription;
        public string[] Aliases => _commandAttribute.Aliases;
        public GuildPermission BotPermissions => _permissionsAttribute?.BotPermissions ?? GuildPermission.SendMessages;
        public GuildPermission UserPermissions => _permissionsAttribute?.UserPermissions ?? GuildPermission.SendMessages;
        public bool OwnerOnly => _ownerOnly;
        public bool RequiresGuild => _requiresGuild;

        public CommandCategory Category { get; private set; }

        public string Usage
        {
            get
            {
                var param = GetMethodParameters(_method);
                StringBuilder b = new StringBuilder();

                foreach (ParameterInfo p in param)
                {
                    if (!p.IsImplicit())
                    {
                        AppendParameter(param, b, p);
                    }
                }

                return b.ToString();
            }
        }

        public async Task Run(string[] args, CommandContext ctx)
        {
            if (_outOfProcessAttribute != null && !Program.RunningOutOfProcess)
            {
                Guid pipeGuid = Guid.NewGuid();
                PipeContext context = new PipeContext(ctx);
                using (NamedPipeServerStream pipeStream = new NamedPipeServerStream(pipeGuid.ToString(), PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                {
                    Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
                        {
                            WorkingDirectory = Directory.GetCurrentDirectory(),
                            UseShellExecute = false,
                            Arguments = $@"--type=command --m={_method.Name} --t={_method.DeclaringType.FullName} --p={pipeGuid} --r={_outOfProcessAttribute.RequiresDiscord}"
                        }
                    };

                    process.Start();
                    await pipeStream.WaitForConnectionAsync();

                    using (StreamReader reader = new StreamReader(pipeStream))
                    using (StreamWriter writer = new StreamWriter(pipeStream) { AutoFlush = true })
                    {
                        await writer.WriteLineAsync(JsonConvert.SerializeObject(Program.Config));
                        await writer.WriteLineAsync(JsonConvert.SerializeObject(context));

                        string str = await reader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            var obj = JToken.Parse(str);
                            if (obj.IsValid(_exceptionSchema))
                            {
                                Exception ex = JsonConvert.DeserializeObject<Exception>(str);
                                throw ex;
                            }
                        }
                    }
                }
            }
            else
            {
                List<object> parameters = new List<object> { ctx };
                int position = 0;

                if (_method.GetCustomAttribute<IgnoreArgumentsAttribute>() == null)
                {
                    foreach (ParameterInfo param in GetMethodParameters(_method))
                    {
                        object obj = null;
                        if (param.IsImplicit() || position < args.Length)
                        {
                            if (param.IsParams())
                            {
                                List<object> thing = new List<object>();
                                foreach (string s in args.Skip(position))
                                {
                                    thing.Add(await ParseParameter(s, param.ParameterType.GetElementType(), param, ctx));
                                }

                                Type type = param.ParameterType.GetElementType();

                                if (type == typeof(string))
                                {
                                    parameters.Add(thing.Cast<string>().ToArray());
                                }
                                else
                                {
                                    throw new NotSupportedException("Parameter argument type unsupported.");
                                }
                            }
                            else
                            {
                                obj = await ParseParameter(args.Any() ? args[position] : null, param.ParameterType, param, ctx);
                                args = ctx.Arguments;
                                parameters.Add(obj);
                            }
                        }
                        else
                        {
                            if (!param.IsOptional)
                            {
                                throw new CommandException($"Hey! You'll need to specify something for \"{param.Name}\"!");
                            }
                            else
                            {
                                parameters.Add(param.DefaultValue);
                            }
                        }

                        if (!param.IsImplicit())
                        {
                            position += 1;
                        }
                    }
                }

                try
                {
                    object obj = _method.Invoke(Category, parameters.ToArray());
                    if (obj is Task t)
                    {
                        await t;
                    }
                }
                catch (Exception ex)
                {
                    throw ex.InnerException ?? ex;
                }
            }
        }

        #region Tools

        private async Task<object> ParseParameter(string arg, Type t, ParameterInfo info, CommandContext ctx)
        {
            object obj;
            try
            {
                TypeConverter converter = TypeDescriptor.GetConverter(t);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    obj = converter.ConvertFromInvariantString(arg);
                }
                else
                {
                    IParamConverter parseExtension = Program.ParamConverters.FirstOrDefault(p => p.AcceptedTypes.Contains(t));
                    if (parseExtension != null)
                    {
                        obj = await parseExtension.Convert(arg, t, ctx);
                    }
                    else
                    {
                        throw new CommandException($"Sorry! I couldn't parse what you specified for \"{info.Name}\". Expected {PrettyTypeName(t)}.");
                    }
                }
            }
            catch
            {
                IParamConverter parseExtension = Program.ParamConverters.FirstOrDefault(p => p.AcceptedTypes.Contains(t));
                if (parseExtension != null)
                {
                    obj = await parseExtension.Convert(arg, t, ctx);
                }
                else
                {
                    throw new CommandException($"Sorry! I couldn't parse what you specified for \"{info.Name}\". Expected {PrettyTypeName(t)}.");
                }
            }

            var validationAttributes = info.GetCustomAttributes<ValidationAttribute>();
            foreach(var attribute in validationAttributes.Where(a => !a.RequiresValidationContext))
            {
                if (!attribute.IsValid(obj))
                {
                    throw new CommandException(
                        $"That doesn't seem right! Check what you've specified for {info.Name}!{(!string.IsNullOrWhiteSpace(attribute.ErrorMessage) ? $" ({attribute.ErrorMessage})" : null)}");
                }
            }

            return obj;
        }

        private static ParameterInfo[] GetMethodParameters(MethodInfo method)
        {
            if (_parameterCache.TryGetValue(method, out var m))
            {
                return m;
            }
            else
            {
                var methods = method.GetParameters().Where(p => p.ParameterType != typeof(CommandContext)).ToArray();
                _parameterCache[method] = methods;
                return methods;
            }
        }

        internal static void AppendParameter(ParameterInfo[] param, StringBuilder b, ParameterInfo p, bool usage = true)
        {
            if (p.IsParams())
            {
                b.Append("params ");
            }

            b.Append($"{PrettyTypeName(p.ParameterType)} {p.Name}");

            if (p.IsOptional)
            {
                b.Append($" = {p.DefaultValue ?? "null"}");
            }

            if (p.Position != (usage ? param.Length : param.Length - 1))
            {
                b.Append(", ");
            }
        }


        internal static string PrettyTypeName(Type t)
        {
            if (_typeKeywords.ContainsKey(t))
            {
                return _typeKeywords[t];
            }

            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() != typeof(Nullable<>))
                {
                    return string.Format(
                        "{0}<{1}>",
                        t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                        string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));
                }
                else
                {
                    return $"{PrettyTypeName(t.GetGenericArguments().First())}?";
                }
            }

            return t.Name;
        }
        #endregion
    }
}
