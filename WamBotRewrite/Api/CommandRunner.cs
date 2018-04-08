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
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

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
        private bool _isNsfw;

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
            _isNsfw = attributes.Any(a => a is NsfwAttribute);
        }

        public string Name => _commandAttribute.Name;
        public string Description => _commandAttribute.Description;
        public string ExtendedDescription => _commandAttribute.ExtendedDescription;
        public string[] Aliases => _commandAttribute.Aliases;
        public GuildPermission BotPermissions => _permissionsAttribute?.BotPermissions ?? GuildPermission.SendMessages;
        public GuildPermission UserPermissions => _permissionsAttribute?.UserPermissions ?? GuildPermission.SendMessages;
        public bool OwnerOnly => _ownerOnly;
        public bool RequiresGuild => _requiresGuild;
        public bool IsNsfw => _isNsfw;

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
                using (NamedPipeServerStream pipeStream = new NamedPipeServerStream(pipeGuid.ToString(), PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo("dotnet")
                        {
                            WorkingDirectory = Directory.GetCurrentDirectory(),
                            UseShellExecute = false,
                            Arguments = $@"{Assembly.GetEntryAssembly().Location} --type=command --m={_method.Name} --t={_method.DeclaringType.FullName} --p={pipeGuid} --r={_outOfProcessAttribute.RequiresDiscord}"
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
                var parameters = new List<object> { ctx };
                int position = 0;

                if (_method.GetCustomAttribute<IgnoreArgumentsAttribute>() == null)
                {
                    foreach (var param in GetMethodParameters(_method))
                    {
                        object obj = null;
                        if (param.IsImplicit() || position < args.Length)
                        {
                            if (param.IsParams())
                            {
                                var thing = new List<object>();
                                foreach (string s in args.Skip(position))
                                {
                                    thing.Add(await ParseParameter(s, param.ParameterType.GetElementType(), param, ctx));
                                }

                                var type = param.ParameterType.GetElementType();

                                MethodInfo[] methods = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static);
                                object enumerable = methods.FirstOrDefault(c => c.Name == "Cast").MakeGenericMethod(type).Invoke(null, new[] { thing });
                                object array = methods.FirstOrDefault(c => c.Name == "ToArray").MakeGenericMethod(type).Invoke(null, new[] { enumerable });
                                parameters.Add(array);
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
                var converter = TypeDescriptor.GetConverter(t);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    obj = converter.ConvertFromInvariantString(arg);
                }
                else
                {
                    var parseExtension = Program.ParamConverters.FirstOrDefault(p => p.AcceptedTypes.Contains(t));
                    if (parseExtension != null)
                    {
                        obj = await parseExtension.Convert(arg, info, ctx);
                    }
                    else
                    {
                        throw new CommandException($"Sorry! I couldn't parse what you specified for \"{info.Name}\". `{PrettyTypeName(t)}` expected.");
                    }
                }
            }
            catch
            {
                var parseExtension = Program.ParamConverters.FirstOrDefault(p => p.AcceptedTypes.Contains(t));
                if (parseExtension != null)
                {
                    obj = await parseExtension.Convert(arg, info, ctx);
                }
                else
                {
                    throw new CommandException($"Sorry! I couldn't parse what you specified for \"{info.Name}\". Expected {PrettyTypeName(t)}.");
                }
            }

            if (obj != null)
            {
                var context = new ValidationContext(obj) { MemberName = info.Name, DisplayName = info.Name };
                var validationAttributes = info.GetCustomAttributes<ValidationAttribute>();
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateValue(obj, context, results, validationAttributes))
                {
                    StringBuilder builder = new StringBuilder($"That doesn't seem right! Check what you've specified for {info.Name}!");
                    if (results.Any(r => !string.IsNullOrWhiteSpace(r.ErrorMessage)))
                    {
                        builder.AppendLine("```");

                        foreach (var result in results)
                        {
                            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                            {
                                builder.AppendLine(result.ErrorMessage);
                            }
                        }

                        builder.AppendLine("```");
                    }

                    throw new CommandException(builder.ToString());
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
            foreach (var a in p.CustomAttributes)
            {
                Type at = a.AttributeType;
                AppendAttribute(b, usage, a, at);
            }

            if (p.IsParams())
            {
                b.Append("params ");
            }

            b.Append($"{PrettyTypeName(p.ParameterType)} {p.Name}");

            if (p.IsOptional)
            {
                b.Append($" = {PrettyValue(p.DefaultValue)}");
            }

            if (p.Position != (usage ? param.Length : param.Length - 1))
            {
                b.Append(", ");
            }
        }

        internal static void AppendAttribute(StringBuilder b, bool usage, CustomAttributeData a, Type at)
        {
            if (!at.Namespace.StartsWith("System.Runtime") && at != typeof(DebuggerStepThroughAttribute) && at != typeof(ParamArrayAttribute))
            {
                b.Append("[");

                string name = at.Name;
                b.Append(name.Substring(0, name.Length - 9));

                if (a.ConstructorArguments.Any() || a.NamedArguments.Any())
                {
                    b.Append("(");

                    var cps = a.Constructor.GetParameters();
                    for (int i = 0; i < cps.Length; i++)
                    {
                        var ap = a.ConstructorArguments.ElementAt(i);
                        var cp = cps.ElementAt(i);

                        if (i != 0)
                        {
                            b.Append(", ");
                        }

                        if (usage)
                            b.Append($"{cp.Name}: {PrettyValue(ap.Value)}");
                        else
                            b.Append(PrettyValue(ap.Value));
                    }

                    for (int i = 0; i < a.NamedArguments.Count; i++)
                    {
                        if (i != 0 || a.ConstructorArguments.Any())
                        {
                            b.Append(", ");
                        }

                        var op = a.NamedArguments.ElementAt(i);
                        b.Append($"{op.MemberName} = {PrettyValue(op.TypedValue.Value)}");
                    }

                    b.Append(")");
                }

                b.Append("] ");

                if (!usage)
                    b.AppendLine();
            }
        }

        private static string PrettyValue(object value)
        {
            StringBuilder b = new StringBuilder();

            if (value != null)
            {

                if (value is Array a)
                {
                    b.Append("new[] { ");
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (i != 0)
                        {
                            b.Append(", ");
                        }

                        var o = a.Cast<object>().ElementAt(i);
                        b.Append(PrettyValue(o));
                    }
                    b.Append(" }");
                }

                if (value is ReadOnlyCollection<CustomAttributeTypedArgument> c)
                {
                    b.Append("new[] { ");
                    for (int i = 0; i < c.Count; i++)
                    {
                        if (i != 0)
                        {
                            b.Append(", ");
                        }

                        var o = c.ElementAt(i);
                        b.Append(PrettyValue(o));
                    }
                    b.Append(" }");
                }

                if (value is string s)
                {
                    b.Append($"\"{s}\"");
                }

                if (value is Enum e)
                {
                    b.Append(e.GetType().Name);
                    b.Append(".");
                    b.Append(value);
                }
            }
            else
            {
                return "null";
            }

            return b.Length > 0 ? b.ToString() : value.ToString();
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
