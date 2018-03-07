using DSharpPlus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Api
{
    public abstract class DiscordCommand : BaseDiscordCommand
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
        };

        protected CommandContext Context { get; private set; }

        public override sealed Func<int, bool> ArgumentCountPrecidate => x =>
        {
            var methods = GetMethods();
            if (methods.Any())
            {
                return methods.Any(c =>
                {
                    var thing = GetMethodParameters(c);
                    return thing.Count() >= x || thing.Any(p => p.IsParams());
                });
            }
            else
            {
                return false;
            }
        };

        public override string Usage
        {
            get
            {
                MethodInfo method = GetDiscordCommandMethod();
                if (method != null)
                {
                    StringBuilder b = new StringBuilder();
                    foreach (ParameterInfo p in GetMethodParameters(method))
                    {
                        if (!p.IsImplicit())
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

                            if (p.Position != GetMethodParameters(method).Length - 1)
                            {
                                b.Append(", ");
                            }
                        }
                    }

                    return b.ToString();
                }
                else
                {
                    return "Invalid command.";
                }
            }
        }

        public override async Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            MethodInfo[] methods = GetMethods();
            if (methods.Any())
            {
                Tuple<ParameterInfo, Exception> lastException = null;
                foreach (MethodInfo method in methods)
                {
                    List<object> parameters = new List<object>();
                    Context = context;
                    int position = 0;
                    bool skip = false;

                    foreach (ParameterInfo param in GetMethodParameters(method))
                    {
                        try
                        {
                            object obj = null;
                            if (param.IsImplicit() || position < args.Length)
                            {
                                if (param.IsParams())
                                {
                                    List<object> thing = new List<object>();
                                    foreach (string s in args.Skip(position))
                                    {
                                        thing.Add(await ParseParameter(s, param.ParameterType.GetElementType()));
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
                                    obj = await ParseParameter(args.Any() ? args[position] : null, param.ParameterType);
                                    args = context.Arguments;
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
                        catch (Exception ex)
                        {
                            lastException = new Tuple<ParameterInfo, Exception>(param, ex);
                            skip = true;
                            break;
                        }
                    }

                    if (skip)
                    {
                        continue;
                    }

                    try
                    {
                        object obj = method.Invoke(this, parameters.ToArray());
                        if (obj is CommandResult c)
                        {
                            return c ?? CommandResult.Empty;
                        }
                        else if (obj is Task<CommandResult> t)
                        {
                            return await t;
                        }
                        else
                        {
                            return "This should really never happen...";
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex.InnerException ?? ex;
                    }
                }

                if (lastException != null)
                {
                    throw new BadArgumentsException();
                }
                else
                {
                    return "Unable to find applicable command method for given arguments.";
                }
            }
            else
            {
                return "Unable to find appropriate command method.";
            }
        }

        private async Task<object> ParseParameter(string arg, Type t)
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
                    IParamConverter parseExtension = Tools.ParameterParseHelpers.FirstOrDefault(p => p.AcceptedTypes.Contains(t));
                    if (parseExtension != null)
                    {
                        obj = await parseExtension.Convert(arg, t, Context);
                    }
                    else
                    {
                        throw new Exception($"Unable to parse \"{t}\"!");
                    }
                }
            }
            catch
            {
                IParamConverter parseExtension = Tools.ParameterParseHelpers.FirstOrDefault(p => p.AcceptedTypes.Contains(t));
                if (parseExtension != null)
                {
                    obj = await parseExtension.Convert(arg, t, Context);
                }
                else
                {
                    throw new Exception($"Unable to parse \"{t}\"!");
                }
            }

            return obj;
        }

        private MethodInfo GetDiscordCommandMethod()
        {
            return GetMethods().FirstOrDefault();
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
                var methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(m => m.ReturnParameter.ParameterType == typeof(Task<CommandResult>) || m.ReturnParameter.ParameterType == typeof(CommandResult)).OrderByDescending(m => m.GetParameters().Count()).ToArray();

                methodCache[t] = methods;
                return methods;
            }
        }

        private static string PrettyTypeName(Type t)
        {
            if (_typeKeywords.ContainsKey(t))
            {
                return _typeKeywords[t];
            }

            if (t.IsGenericType)
            {
                return string.Format(
                    "{0}<{1}>",
                    t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                    string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));
            }

            return t.Name;
        }

    }
}
