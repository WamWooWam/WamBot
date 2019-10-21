using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace WamWooWam.Core.Reflection
{
    public static class Extensions
    {
#if !NETSTANDARD1_6
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

        public static bool IsParams(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ParamArrayAttribute), false);
        }

        public static string GetDeclaration(this MethodInfo method)
        {
            var builder = new StringBuilder();

            foreach (var attr in method.GetCustomAttributesData())
            {
                AppendAttribute(builder, attr, attr.AttributeType);
            }

            if (method.IsPublic)
                builder.Append("public ");
            else if (method.IsPrivate)
                builder.Append("private ");

            if (method.IsStatic)
                builder.Append("static ");

            // thx uwx bb
            var basedef = method.GetBaseDefinition();
            if (basedef != method)
            {
                builder.Append("override ");
            }

            if (method.Attributes.HasFlag(MethodAttributes.Final))
            {
                builder.Append("sealed ");
            }

            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                builder.Append("async ");

            builder.Append(PrettyTypeName(method.ReturnType));
            builder.Append(" ");

            builder.PrettyMethodName(method);
            builder.Append("(");

            var parameters = method.GetParameters();
            foreach (var p in parameters)
            {
                AppendParameter(parameters, builder, p);
            }

            builder.Append(");");

            return builder.ToString();
        }

        private static void PrettyMethodName(this StringBuilder builder, MethodInfo method)
        {
            if (method.IsGenericMethod)
            {
                if (method.Name.Contains("`"))
                    builder.Append(method.Name.Substring(0, method.Name.LastIndexOf("`", StringComparison.InvariantCulture)));
                else
                    builder.Append(method.Name);

                builder.Append("<");
                builder.Append(string.Join(", ", method.GetGenericArguments().Select(a => PrettyTypeName(a))));
                builder.Append(">");
            }
            else
            {
                builder.Append(method.Name);
            }
        }

        internal static void AppendParameter(ParameterInfo[] param, StringBuilder b, ParameterInfo p)
        {
            foreach (var a in p.CustomAttributes)
            {
                var at = a.AttributeType;
                AppendAttribute(b, a, at);
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

            if (p.Position != param.Length - 1)
            {
                b.Append(", ");
            }
        }

        internal static void AppendAttribute(StringBuilder b, CustomAttributeData a, Type at)
        {
            if (!at.Name.StartsWith("_") && at.Namespace?.StartsWith("System.Runtime") != true && at != typeof(DebuggerStepThroughAttribute) && at != typeof(ParamArrayAttribute))
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
            }
        }

        private static string PrettyValue(object value)
        {
            var b = new StringBuilder();

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

            if (t.IsGenericType && t.Name.Contains("`"))
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
#endif
    }
}
