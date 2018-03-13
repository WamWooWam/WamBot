using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace WamBotRewrite.Api
{
    /// <summary>
    /// A class that wraps methods allowing them to be called easier
    /// </summary>
    internal class CommandRunner
    {
        private static ConcurrentDictionary<Type, MethodInfo[]> methodCache = new ConcurrentDictionary<Type, MethodInfo[]>();
        private static ConcurrentDictionary<MethodInfo, ParameterInfo[]> parameterCache = new ConcurrentDictionary<MethodInfo, ParameterInfo[]>();
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


        internal MethodInfo _method;
        private CommandAttribute _commandAttribute;
        private PermissionsAttribute _permissionsAttribute;
        private bool _ownerOnly;
        private bool _ignoreArguments;
        private bool _requiresGuild;

        public CommandRunner(MethodInfo method, CommandCategory category)
        {
            _method = method;
            Category = category;


            var attributes = method.GetCustomAttributes(true);

            _commandAttribute = (CommandAttribute)attributes.FirstOrDefault(a => a is CommandAttribute);
            if (_commandAttribute == null)
            {
                throw new ArgumentException("Method is not a command method");
            }

            _permissionsAttribute = (PermissionsAttribute)attributes.FirstOrDefault(a => a is PermissionsAttribute);
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
            MethodInfo[] methods = GetMethods();
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
                                thing.Add(await ParseParameter(s, param.ParameterType.GetElementType(), ctx));
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
                            obj = await ParseParameter(args.Any() ? args[position] : null, param.ParameterType, ctx);
                            args = ctx.Arguments;
                            parameters.Add(obj);
                        }
                    }
                    else
                    {
                        if (!param.IsOptional)
                        {
                            throw new CommandException($"Missing argument! {param.Name}");
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

        #region Tools

        private async Task<object> ParseParameter(string arg, Type t, CommandContext ctx)
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
                        throw new Exception($"Unable to parse \"{t}\"!");
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
                    throw new Exception($"Unable to parse \"{t}\"!");
                }
            }

            return obj;
        }

        private static ParameterInfo[] GetMethodParameters(MethodInfo method)
        {
            if (parameterCache.TryGetValue(method, out var m))
            {
                return m;
            }
            else
            {
                var methods = method.GetParameters().Where(p => p.ParameterType != typeof(CommandContext)).ToArray();
                parameterCache[method] = methods;
                return methods;
            }
        }

        private MethodInfo[] GetMethods()
        {
            if (methodCache.TryGetValue(GetType(), out var info))
            {
                return info;
            }
            else
            {
                Type t = GetType();
                var methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(m => m.ReturnParameter.ParameterType == typeof(Task) || m.ReturnParameter.ParameterType == typeof(void))
                    .OrderByDescending(m => m.GetParameters().Count())
                    .ToArray();

                methodCache[t] = methods;
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
