using Microsoft.Scripting.HostBridge;
using Microsoft.Scripting.JavaScript.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Scripting.JavaScript
{
    public sealed class JavaScriptConverter
    {
        public class JavaScriptProjection
        {
            public volatile int RefCount;
            public JavaScriptObject Prototype;
            public JavaScriptFunction Constructor;
            public bool HasStaticEvents;
            public bool HasInstanceEvents;
        }

        private BridgeManager hostBridge_;
        private WeakReference<JavaScriptEngine> engine_;
        private ChakraApi api_;
        private Dictionary<Type, JavaScriptProjection> projectionTypes_;
        private Dictionary<Type, Expression> eventMarshallers_;

        public JavaScriptConverter(JavaScriptEngine engine)
        {
            engine_ = new WeakReference<JavaScriptEngine>(engine);
            api_ = engine.Api;
            projectionTypes_ = new Dictionary<Type, JavaScriptProjection>();
            eventMarshallers_ = new Dictionary<Type, Expression>();

            hostBridge_ = new BridgeManager(engine);
        }

        private JavaScriptEngine GetEngine()
        {
            JavaScriptEngine result;
            if (!engine_.TryGetTarget(out result))
                throw new ObjectDisposedException(nameof(JavaScriptEngine));

            return result;
        }

        private JavaScriptEngine GetEngineAndClaimContext()
        {
            var result = GetEngine();

            return result;
        }

        public bool ToBoolean(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();

            bool result;

            if (value.Type == JavaScriptValueType.Boolean)
            {
                Errors.ThrowIfIs(api_.JsBooleanToBool(value.handle_, out result));
            }
            else
            {
                JavaScriptValueSafeHandle tempBool;
                Errors.ThrowIfIs(api_.JsConvertValueToBoolean(value.handle_, out tempBool));
                using (tempBool)
                {
                    Errors.ThrowIfIs(api_.JsBooleanToBool(tempBool, out result));
                }
            }

            return result;
        }

        public JavaScriptValue FromBoolean(bool value)
        {
            var eng = GetEngine();
            if (value)
                return eng.TrueValue;

            return eng.FalseValue;
        }

        public double ToDouble(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();

            double result;

            if (value.Type == JavaScriptValueType.Number)
            {
                Errors.ThrowIfIs(api_.JsNumberToDouble(value.handle_, out result));
            }
            else
            {
                JavaScriptValueSafeHandle tempVal;
                Errors.ThrowIfIs(api_.JsConvertValueToNumber(value.handle_, out tempVal));
                using (tempVal)
                {
                    Errors.ThrowIfIs(api_.JsNumberToDouble(tempVal, out result));
                }
            }

            return result;
        }

        public JavaScriptValue FromDouble(double value)
        {
            var eng = GetEngineAndClaimContext();

            JavaScriptValueSafeHandle result;
            Errors.ThrowIfIs(api_.JsDoubleToNumber(value, out result));
            return eng.CreateValueFromHandle(result);
        }

        public int ToInt32(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();

            int result;

            if (value.Type == JavaScriptValueType.Number)
            {
                Errors.ThrowIfIs(api_.JsNumberToInt(value.handle_, out result));
            }
            else
            {
                JavaScriptValueSafeHandle tempVal;
                Errors.ThrowIfIs(api_.JsConvertValueToNumber(value.handle_, out tempVal));
                using (tempVal)
                {
                    Errors.ThrowIfIs(api_.JsNumberToInt(tempVal, out result));
                }
            }

            return result;
        }

        public JavaScriptValue FromInt32(int value)
        {
            var eng = GetEngineAndClaimContext();

            JavaScriptValueSafeHandle result;
            Errors.ThrowIfIs(api_.JsIntToNumber(value, out result));

            return eng.CreateValueFromHandle(result);
        }

        public unsafe string ToString(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();
            if (value.Type == JavaScriptValueType.String)
            {
                void* str;
                uint len;
                Errors.ThrowIfIs(api_.JsStringToPointer(value.handle_, out str, out len));
                if (len > int.MaxValue)
                    throw new OutOfMemoryException("Exceeded maximum string length.");

                return Marshal.PtrToStringUni(new IntPtr(str), unchecked((int)len));
            }
            else if (value.Type == JavaScriptValueType.Symbol)
            {
                // Presently, JsRT doesn't have a way for the host to query the description of a Symbol
                // Using JsConvertValueToString resulted in putting the runtime into an exception state
                // Thus, detect the condition and just return a known string.

                return "(Symbol)";
            }
            else
            {
                JavaScriptValueSafeHandle tempStr;
                Errors.ThrowIfIs(api_.JsConvertValueToString(value.handle_, out tempStr));
                using (tempStr)
                {
                    void* str;
                    uint len;
                    Errors.ThrowIfIs(api_.JsStringToPointer(tempStr, out str, out len));
                    if (len > int.MaxValue)
                        throw new OutOfMemoryException("Exceeded maximum string length.");

                    return Marshal.PtrToStringUni(new IntPtr(str), unchecked((int)len));
                }
            }
        }

        public unsafe JavaScriptValue FromString(string value)
        {
            var eng = GetEngineAndClaimContext();

            JavaScriptValueSafeHandle result;
            var encoded = Encoding.Unicode.GetBytes(value);
            fixed (byte* ptr = &encoded[0])
            {
                Errors.ThrowIfIs(api_.JsPointerToString(ptr, value.Length, out result));
            }

            return eng.CreateValueFromHandle(result);
        }

        public JavaScriptArray FromArray(object[] value)
        {
            var eng = GetEngineAndClaimContext();

            JavaScriptValueSafeHandle result;
            api_.JsCreateArray((uint)value.Length, out result);

            var arr = eng.CreateArrayFromHandle(result);

            for (var i = 0; i < value.Length; i++)
            {
                var val = FromObject(value[i]);
                using (var temp = eng.Converter.FromInt32(i))
                    Errors.ThrowIfIs(api_.JsSetIndexedProperty(result, temp.handle_, val.handle_));
            }

            return arr;
        }

        public JavaScriptValue FromObject(object o)
        {
            var eng = GetEngine();
            if (o == null)
            {
                return eng.NullValue;
            }

            if (o is JavaScriptValue jsVal)
                return jsVal;

            var t = o.GetType();
            if (t == typeof(string))
            {
                return FromString((string)o);
            }
            else if (t == typeof(double) || t == typeof(float))
            {
                return FromDouble((double)o);
            }
            else if (t == typeof(int) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte))
            {
                return FromInt32((int)o);
            }
            else if (t == typeof(uint))
            {
                return FromDouble((uint)o);
            }
            else if (t == typeof(long))
            {
                return FromDouble((long)o);
            }
            else if (t == typeof(bool))
            {
                var b = (bool)o;
                return b ? eng.TrueValue : eng.FalseValue;
            }
            else if (t.GetTypeInfo().IsValueType)
            {
                throw new ArgumentException("Non-primitive value types may not be projected to JavaScript directly.  Use a JSON serializer to serialize the value.  For more information, see readme.md.");
            }
            else if (t.IsArray || typeof(IEnumerable).IsAssignableFrom(t))
            {
                var array = (IEnumerable)o;
                return FromArray(array.Cast<object>().ToArray());
            }
            else if (typeof(Task).IsAssignableFrom(t))
            {
                // todo : project as Promise

                return eng.NullValue;
            }
            else if (typeof(Delegate).IsAssignableFrom(t))
            {
                throw new ArgumentException("Use JavaScriptEngine.CreateFunction to marshal a delegate to JavaScript.");
            }
            else
            {
                var result = InitializeProjectionForObject(o);
                return result;
            }
        }

        public JavaScriptValue FromObjectViaNewBridge(object o)
        {
            var eng = GetEngine();
            if (o == null)
            {
                return eng.NullValue;
            }

            if (o is JavaScriptValue jsVal)
                return jsVal;

            var t = o.GetType();
            if (t == typeof(string))
            {
                return FromString((string)o);
            }
            else if (t == typeof(double) || t == typeof(float))
            {
                return FromDouble((double)o);
            }
            else if (t == typeof(int) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte))
            {
                return FromInt32((int)o);
            }
            else if (t == typeof(uint))
            {
                return FromDouble((uint)o);
            }
            else if (t == typeof(long))
            {
                return FromDouble((long)o);
            }
            else if (t == typeof(bool))
            {
                var b = (bool)o;
                return b ? eng.TrueValue : eng.FalseValue;
            }
            else if (t.GetTypeInfo().IsValueType)
            {
                throw new ArgumentException("Non-primitive value types may not be projected to JavaScript directly.  Use a JSON serializer to serialize the value.  For more information, see readme.md.");
            }
            else if (t.IsArray || typeof(IEnumerable).IsAssignableFrom(t))
            {
                var array = (IEnumerable)o;
                return FromArray(array.Cast<object>().ToArray());
            }
            else if (typeof(Task).IsAssignableFrom(t))
            {
                // todo : project as Promise

                return eng.NullValue;
            }
            else if (typeof(Delegate).IsAssignableFrom(t))
            {
                throw new ArgumentException("Use JavaScriptEngine.CreateFunction to marshal a delegate to JavaScript.");
            }
            else
            {
                var cb = hostBridge_.GetBridge(t);
                return cb.ProjectObject(o);
            }
        }

        public JavaScriptFunction FromDelegate(Delegate d, string name)
        {
            var engine = GetEngine();
            var method = engine.CreateFunction((eng, ctor, thisObj, args) =>
            {
                var argsToPass = new List<object>();
                foreach (var item in args)
                {
                    argsToPass.Add(ToObject(item));
                }

                try
                {
                    return FromObject(d.DynamicInvoke(argsToPass.ToArray()));
                }
                catch (Exception ex)
                {
                    eng.SetException(FromObject(ex));
                    return eng.UndefinedValue;
                }
            }, name);

            return method;
        }

        public object ToObject(JavaScriptValue val)
        {
            switch (val.Type)
            {
                case JavaScriptValueType.Boolean:
                    return val.IsTruthy;
                case JavaScriptValueType.Number:
                    return ToDouble(val);
                case JavaScriptValueType.String:
                    return ToString(val);
                case JavaScriptValueType.Undefined:
                    return null;
                case JavaScriptValueType.Array:
                    var arr = val as JavaScriptArray;
                    Debug.Assert(arr != null);

                    var array = arr.Select(v => ToObject(v)).ToArray();
                    if (!array.Any(o => o.GetType() != array[0].GetType()))
                    {
                        var type = array[0].GetType();
                        var newArray = Array.CreateInstance(type, array.Length);
                        Array.Copy(array, newArray, array.Length);

                        return newArray;
                    }

                    return array;
                case JavaScriptValueType.ArrayBuffer:
                    var ab = val as JavaScriptArrayBuffer;
                    Debug.Assert(ab != null);

                    return ab.GetUnderlyingMemory();

                case JavaScriptValueType.DataView:
                    var dv = val as JavaScriptDataView;
                    Debug.Assert(dv != null);

                    return dv.GetUnderlyingMemory();

                case JavaScriptValueType.TypedArray:
                    var ta = val as JavaScriptTypedArray;
                    Debug.Assert(ta != null);

                    return ta.GetUnderlyingMemory();

                case JavaScriptValueType.Object:
                    var obj = val as JavaScriptObject;
                    var external = obj.ExternalObject;
                    return external ?? obj;

                // Unsupported marshaling types
                case JavaScriptValueType.Function:
                case JavaScriptValueType.Date:
                case JavaScriptValueType.Symbol:
                default:
                    throw new NotSupportedException("Unsupported type marshaling value from JavaScript to host: " + val.Type);
            }
        }

        public JavaScriptObject GetProjectionPrototypeForType(Type t)
        {
            JavaScriptProjection baseTypeProjection;

            if (projectionTypes_.TryGetValue(t, out baseTypeProjection))
            {
                Interlocked.Increment(ref baseTypeProjection.RefCount);
            }
            else
            {
                baseTypeProjection = InitializeProjectionForType(t);
                baseTypeProjection.RefCount++;
            }
            projectionTypes_[t] = baseTypeProjection;

            return baseTypeProjection.Prototype;
        }

        public JavaScriptProjection GetProjectionForType(Type t)
        {
            JavaScriptProjection baseTypeProjection;

            if (projectionTypes_.TryGetValue(t, out baseTypeProjection))
            {
                Interlocked.Increment(ref baseTypeProjection.RefCount);
            }
            else
            {
                baseTypeProjection = InitializeProjectionForType(t);
                baseTypeProjection.RefCount++;
            }
            projectionTypes_[t] = baseTypeProjection;

            return baseTypeProjection;
        }

        private JavaScriptProjection InitializeProjectionForType(Type t)
        {
            var eng = GetEngineAndClaimContext();

            var reflector = ObjectReflector.Create(t);
            JavaScriptProjection baseTypeProjection = null;
            if (reflector.HasBaseType)
            {
                var baseType = reflector.GetBaseType();
                if (!projectionTypes_.TryGetValue(baseType, out baseTypeProjection))
                {
                    baseTypeProjection = InitializeProjectionForType(baseType);
                    baseTypeProjection.RefCount += 1;
                    projectionTypes_[baseType] = baseTypeProjection;
                }
            }

            var publicConstructors = reflector.GetConstructors();
            var publicInstanceProperties = reflector.GetProperties(instance: true);
            var publicStaticProperties = reflector.GetProperties(instance: false);
            var publicInstanceMethods = reflector.GetMethods(instance: true);
            var publicStaticMethods = reflector.GetMethods(instance: false);
            var publicInstanceEvents = reflector.GetEvents(instance: true);
            var publicStaticEvents = reflector.GetEvents(instance: false);

            //if (AnyHaveSameArity(publicConstructors, publicInstanceMethods, publicStaticMethods, publicInstanceProperties, publicStaticProperties))
            //    throw new InvalidOperationException("The specified type cannot be marshaled; some publicly accessible members have the same arity.  Projected methods can't differentiate only by type (e.g., Add(int, int) and Add(float, float) would cause this error).");

            JavaScriptFunction ctor;
            if (!t.IsAbstract)
            {
                // e.g. var MyObject = function() { [native code] };
                ctor = eng.CreateFunction((engine, constr, thisObj, args) =>
                {
                    // todo
                    return FromObject(Activator.CreateInstance(t, args.Select(s => ToObject(s)).ToArray()));
                }, t.Name);
            }
            else
            {
                ctor = eng.CreateFunction((engine, constr, thisObj, args) =>
                {
                    return eng.UndefinedValue;
                }, t.Name);
            }

            // MyObject.prototype = Object.create(baseTypeProjection.PrototypeObject);
            var prototypeObj = CreateObjectFor(eng, baseTypeProjection);
            ctor.SetPropertyByName("prototype", prototypeObj);
            // MyObject.prototype.constructor = MyObject;
            prototypeObj.SetPropertyByName("constructor", ctor);

            // MyObject.CreateMyObject = function() { [native code] };
            ProjectMethods(t.Name, ctor, eng, publicStaticMethods);
            // Object.defineProperty(MyObject, 'Foo', { get: function() { [native code] } });
            ProjectProperties(t.Name, ctor, eng, publicStaticProperties);
            // MyObject.addEventListener = function() { [native code] };
            if ((baseTypeProjection?.HasStaticEvents ?? false) || publicStaticEvents.Any())
                ProjectEvents(t.Name, ctor, eng, publicStaticEvents, baseTypeProjection, instance: false);


            // MyObject.prototype.ToString = function() { [native code] };
            ProjectMethods(t.Name + ".prototype", prototypeObj, eng, publicInstanceMethods);
            // Object.defineProperty(MyObject.prototype, 'baz', { get: function() { [native code] }, set: function() { [native code] } });
            ProjectProperties(t.Name + ".prototype", prototypeObj, eng, publicInstanceProperties);
            // MyObject.prototype.addEventListener = function() { [native code] };
            if ((baseTypeProjection?.HasInstanceEvents ?? false) || publicInstanceEvents.Any())
                ProjectEvents(t.Name + ".prototype", prototypeObj, eng, publicInstanceEvents, baseTypeProjection, instance: true);

            prototypeObj.Freeze();

            return new JavaScriptProjection
            {
                RefCount = 0,
                Constructor = ctor,
                Prototype = prototypeObj,
                HasInstanceEvents = (baseTypeProjection?.HasInstanceEvents ?? false) || publicInstanceEvents.Any(),
                HasStaticEvents = (baseTypeProjection?.HasStaticEvents ?? false) || publicStaticEvents.Any(),
            };
        }

        // used by InitializeProjectionForType
        private JavaScriptObject CreateObjectFor(JavaScriptEngine engine, JavaScriptProjection baseTypeProjection)
        {
            if (baseTypeProjection != null)
            {
                // todo: revisit to see if there is a better way to do this
                // Can probably get 
                // ((engine.GlobalObject.GetPropertyByName("Object") as JavaScriptObject).GetPropertyByName("create") as JavaScriptFunction).Invoke(baseTypeProjection.Prototype) 
                // but this is so much clearer
                dynamic global = engine.GlobalObject;
                JavaScriptObject result = global.Object.create(baseTypeProjection.Prototype);
                return result;
            }
            else
            {
                return engine.CreateObject();
            }
        }

        private void ProjectMethods(string owningTypeName, JavaScriptObject target, JavaScriptEngine engine, IEnumerable<MethodInfo> methods)
        {
            var methodsByName = methods.GroupBy(m => m.Name);
            foreach (var group in methodsByName)
            {
                JavaScriptFunction method;
                if (group.Any(g => typeof(Task).IsAssignableFrom(g.ReturnType)))
                {
                    method = engine.CreateFunction(async (eng, thisObj, args) =>
                    {
                        if (!(thisObj is JavaScriptObject obj))
                        {
                            eng.SetException(eng.CreateTypeError("Could not call method '" + group.Key + "' because there was an invalid 'this' context."));
                            return eng.UndefinedValue;
                        }

                        var argsArray = args.ToArray();
                        var candidate = GetBestFitMethod(group, thisObj, argsArray);
                        if (candidate == null)
                        {
                            eng.SetException(eng.CreateReferenceError("Could not find suitable method or not enough arguments to invoke '" + group.Key + "'."));
                            return eng.UndefinedValue;
                        }

                        var argsToPass = new List<object>();
                        for (var i = 0; i < candidate.GetParameters().Length; i++)
                        {
                            argsToPass.Add(ToObject(argsArray.ElementAtOrDefault(i) ?? eng.NullValue));
                        }

                        try
                        {
                            var result = candidate.Invoke(obj.ExternalObject, argsToPass.ToArray());
                            if (result is Task task)
                            {
                                await task;
                                var resultProperty = task.GetType().GetProperty("Result");
                                if (resultProperty != null)
                                {
                                    return FromObject(resultProperty.GetValue(task));
                                }
                            }

                            return FromObject(result);
                        }
                        catch (Exception ex)
                        {
                            eng.SetException(FromObject(ex));
                            return eng.UndefinedValue;
                        }
                    }, AsyncHostFunctionKind.Promise);
                }
                else
                {
                    method = engine.CreateFunction((eng, ctor, thisObj, args) =>
                    {
                        if (!(thisObj is JavaScriptObject obj))
                        {
                            eng.SetException(eng.CreateTypeError("Could not call method '" + group.Key + "' because there was an invalid 'this' context."));
                            return eng.UndefinedValue;
                        }

                        var argsArray = args.ToArray();
                        var candidate = GetBestFitMethod(group, thisObj, argsArray);
                        if (candidate == null)
                        {
                            eng.SetException(eng.CreateReferenceError("Could not find suitable method or not enough arguments to invoke '" + group.Key + "'."));
                            return eng.UndefinedValue;
                        }

                        var argsToPass = new List<object>();
                        for (var i = 0; i < candidate.GetParameters().Length; i++)
                        {
                            argsToPass.Add(ToObject(argsArray.ElementAtOrDefault(i) ?? eng.NullValue));
                        }

                        try
                        {
                            return FromObject(candidate.Invoke(obj.ExternalObject, argsToPass.ToArray()));
                        }
                        catch (Exception ex)
                        {
                            eng.SetException(FromObject(ex));
                            return eng.UndefinedValue;
                        }
                    }, owningTypeName + "." + group.Key);
                }

                //var propDescriptor = engine.CreateObject();
                //propDescriptor.SetPropertyByName("configurable", engine.TrueValue);
                //propDescriptor.SetPropertyByName("enumerable", engine.TrueValue);
                //propDescriptor.SetPropertyByName("value", method);
                //target.DefineProperty(group.Key, propDescriptor);
                target.SetPropertyByName(group.Key, method);
            }
        }

        // todo: replace with a dynamic method thunk
        private MethodInfo GetBestFitMethod(IEnumerable<MethodInfo> methodCandidates, JavaScriptValue thisObj, JavaScriptValue[] argsArray)
        {
            if (!(thisObj is JavaScriptObject @this))
                return null;

            var external = @this.ExternalObject;
            if (external == null)
                return null;

            MethodInfo most = null;
            var arity = -1;
            foreach (var candidate in methodCandidates)
            {
                //if (candidate.DeclaringType != external.GetType())
                //    continue;

                var paramCount = candidate.GetParameters().Length;
                if (argsArray.Length == paramCount)
                {
                    return candidate;
                }
                else if (argsArray.Length < paramCount)
                {
                    if (paramCount > arity)
                    {
                        arity = paramCount;
                        most = candidate;
                    }
                }
            }

            return most;
        }

        private void ProjectProperties(string owningTypeName, JavaScriptObject target, JavaScriptEngine engine, IEnumerable<PropertyInfo> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                JavaScriptFunction jsGet = null, jsSet = null;
                if (prop.GetMethod != null)
                {
                    jsGet = engine.CreateFunction((eng, ctor, thisObj, args) =>
                    {
                        if (!(thisObj is JavaScriptObject @this))
                        {
                            eng.SetException(eng.CreateTypeError("Could not retrieve property '" + prop.Name + "' because there was an invalid 'this' context."));
                            return eng.UndefinedValue;
                        }

                        try
                        {
                            return FromObject(prop.GetValue(@this.ExternalObject));
                        }
                        catch (Exception ex)
                        {
                            eng.SetException(FromObject(ex));
                            return eng.UndefinedValue;
                        }
                    }, owningTypeName + "." + prop.Name + ".get");
                }
                if (prop.SetMethod != null)
                {
                    jsSet = engine.CreateFunction((eng, ctor, thisObj, args) =>
                    {
                        if (!(thisObj is JavaScriptObject @this))
                        {
                            eng.SetException(eng.CreateTypeError("Could not retrieve property '" + prop.Name + "' because there was an invalid 'this' context."));
                            return eng.UndefinedValue;
                        }

                        try
                        {
                            var val = ToObject(args.First());
                            if (prop.PropertyType == typeof(int))
                            {
                                val = (int)(double)val;
                            }
                            prop.SetValue(@this.ExternalObject, val);
                            return eng.UndefinedValue;
                        }
                        catch (Exception ex)
                        {
                            eng.SetException(FromObject(ex));
                            return eng.UndefinedValue;
                        }
                    }, owningTypeName + "." + prop.Name + ".set");
                }

                var descriptor = engine.CreateObject();
                if (jsGet != null)
                    descriptor.SetPropertyByName("get", jsGet);
                if (jsSet != null)
                    descriptor.SetPropertyByName("set", jsSet);
                descriptor.SetPropertyByName("enumerable", engine.TrueValue);
                target.DefineProperty(prop.Name, descriptor);
            }
        }

        private static bool AnyHaveSameArity(params IEnumerable<MemberInfo>[] members)
        {
            foreach (var memberset in members)
            {
                var arities = new HashSet<int>();

                if (memberset is IEnumerable<ConstructorInfo> ctors)
                {
                    foreach (var ctor in ctors)
                    {
                        var arity = ctor.GetParameters().Length;
                        if (arities.Contains(arity))
                            return true;
                        arities.Add(arity);
                    }
                }
                else if (memberset is IEnumerable<MethodInfo> methods)
                {
                    foreach (var methodGroup in methods.GroupBy(m => m.Name))
                    {
                        arities.Clear();
                        foreach (var method in methodGroup)
                        {
                            var arity = method.GetParameters().Length;
                            if (arities.Contains(arity))
                                return true;
                            arities.Add(arity);
                        }
                    }
                }
                else if (memberset is IEnumerable<PropertyInfo> props)
                {
                    //foreach (var prop in props)
                    //{
                    //    int arity = prop.GetIndexParameters().Length;
                    //    if (arities.Contains(arity))
                    //        return true;
                    //    arities.Add(arity);
                    //}
                }
                else
                {
                    throw new InvalidOperationException("Unrecognized member type");
                }
            }

            return false;
        }

        private void ProjectEvents(string owningTypeName, JavaScriptObject target, JavaScriptEngine engine, IEnumerable<EventInfo> events, JavaScriptProjection baseTypeProjection, bool instance)
        {
            var eventsArray = events.ToArray();
            var eventsLookup = eventsArray.ToDictionary(ei => ei.Name.ToLower());
            // General approach here
            // if there is a base thing, invoke that
            // for each event, register a delegate that marshals it back to JavaScript
            var add = engine.CreateFunction((eng, ctor, thisObj, args) =>
            {
                var callBase = instance && (baseTypeProjection?.HasInstanceEvents ?? false);
                if (!(thisObj is JavaScriptObject @this))
                    return eng.UndefinedValue;

                if (callBase)
                {
                    var baseObj = baseTypeProjection.Prototype;
                    if (baseObj.GetPropertyByName("addEventListener") is JavaScriptFunction baseFn)
                    {
                        baseFn.Call(@this, args);
                    }
                }

                var argsArray = args.ToArray();
                if (argsArray.Length < 2)
                    return eng.UndefinedValue;

                var eventName = argsArray[0].ToString();
                if (!(argsArray[1] is JavaScriptFunction callbackFunction))
                    return eng.UndefinedValue;

                EventInfo curEvent;
                if (!eventsLookup.TryGetValue(eventName, out curEvent))
                    return eng.UndefinedValue;

                var targetMethod = curEvent.EventHandlerType.GetMethod("Invoke");

                var paramsExpr = targetMethod.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
                var cookie = EventMarshaler.RegisterDelegate(callbackFunction, SynchronizationContext.Current);

                var marshaler = Expression.Lambda(curEvent.EventHandlerType, Expression.Block(
                    Expression.Call(
                        typeof(EventMarshaler).GetMethod(nameof(EventMarshaler.InvokeJavaScriptCallback)),
                        Expression.Constant(cookie),
                        Expression.NewArrayInit(typeof(string), targetMethod.GetParameters().Select(p => Expression.Constant(p.Name))),
                        Expression.NewArrayInit(typeof(object), paramsExpr))
                ), paramsExpr);

                curEvent.AddMethod.Invoke(@this.ExternalObject, new object[] { marshaler.Compile() });

                return eng.UndefinedValue;
            }, owningTypeName + ".addEventListener");
            target.SetPropertyByName("addEventListener", add);
        }

        private JavaScriptObject InitializeProjectionForObject(object target)
        {
            var t = target.GetType();
            JavaScriptProjection projection;
            if (!projectionTypes_.TryGetValue(t, out projection))
            {
                projection = InitializeProjectionForType(t);
            }

            Interlocked.Increment(ref projection.RefCount);
            // Avoid race condition in which projectionTypes_ has had projection removed
            projectionTypes_[t] = projection;

            var eng = GetEngineAndClaimContext();
            var result = eng.CreateExternalObject(target, externalData =>
            {
                if (Interlocked.Decrement(ref projection.RefCount) <= 0)
                {
                    projectionTypes_.Remove(t);
                }
            });
            result.Prototype = projection.Prototype;

            return result;
        }

        private abstract class ObjectReflector
        {
            public abstract IEnumerable<MethodInfo> GetMethods(bool instance);
            public abstract IEnumerable<PropertyInfo> GetProperties(bool instance);
            public abstract IEnumerable<ConstructorInfo> GetConstructors();
            public abstract IEnumerable<EventInfo> GetEvents(bool instance);
            public abstract IEnumerable<FieldInfo> GetFields(bool instance);

            public static ObjectReflector Create(Type t)
            {
                return (ObjectReflector)Activator.CreateInstance(typeof(ObjectReflector<>).MakeGenericType(t));
            }

            public abstract bool HasBaseType
            {
                get;
            }

            public abstract Type GetBaseType();

            public abstract bool IsDelegateType
            {
                get;
            }
        }

        private class ObjectReflector<T>
            : ObjectReflector
        {
            private static readonly Type Type_ = typeof(T);
            private static readonly TypeInfo TypeInfo_ = Type_.GetTypeInfo();

            public override IEnumerable<ConstructorInfo> GetConstructors()
            {
                return Type_.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            }

            public override IEnumerable<EventInfo> GetEvents(bool instance)
            {
                return TypeInfo_.GetEvents(BindingFlags.Public | (instance ? BindingFlags.Instance : BindingFlags.Static));
            }

            public override IEnumerable<FieldInfo> GetFields(bool instance)
            {
                // todo
                yield break;
            }

            public override IEnumerable<MethodInfo> GetMethods(bool instance)
            {
                return TypeInfo_.GetMethods(BindingFlags.Public | (instance ? BindingFlags.Instance : BindingFlags.Static))
                    .Where(m => !m.Attributes.HasFlag(MethodAttributes.SpecialName));
            }

            public override IEnumerable<PropertyInfo> GetProperties(bool instance)
            {
                return TypeInfo_.GetProperties(BindingFlags.Public | (instance ? BindingFlags.Instance : BindingFlags.Static));
            }

            public override bool HasBaseType
            {
                get
                {
                    return TypeInfo_.BaseType != null;
                }
            }

            public override Type GetBaseType()
            {
                return TypeInfo_.BaseType;
            }

            public override bool IsDelegateType
            {
                get
                {
                    return typeof(Delegate).IsAssignableFrom(Type_);
                }
            }
        }
    }

    internal static class EventMarshaler
    {
        private static Dictionary<int, Tuple<JavaScriptFunction, SynchronizationContext>> eventRegistrations = new Dictionary<int, Tuple<JavaScriptFunction, SynchronizationContext>>();
        private static volatile int registrationTokens = 0;

        public static int RegisterDelegate(JavaScriptFunction callback, SynchronizationContext syncContext)
        {
            var cookie = Interlocked.Increment(ref registrationTokens);
            eventRegistrations[cookie] = Tuple.Create(callback, syncContext);
            return cookie;
        }

        public static void RemoveDelegate(int cookie)
        {
            eventRegistrations.Remove(cookie);
        }

        public static void InvokeJavaScriptCallback(int cookie, string[] names, object[] values)
        {
            Tuple<JavaScriptFunction, SynchronizationContext> registration;

            Debug.Assert(names != null);
            Debug.Assert(values != null);
            Debug.Assert(names.Length == values.Length);

            if (!eventRegistrations.TryGetValue(cookie, out registration))
                return;
            if (registration.Item2 == null) // synchronization context
            {
                var eng = registration.Item1.GetEngine();
                using (var context = eng.AcquireContext())
                {
                    var jsObj = eng.CreateObject();
                    for (var i = 0; i < names.Length; i++)
                    {
                        jsObj.SetPropertyByName(names[i], eng.Converter.FromObject(values[i]));
                    }

                    registration.Item1.Invoke(new[] { jsObj });
                }
            }
            else
            {
                registration.Item2.Post((s) =>
                {
                    var eng = registration.Item1.GetEngine();
                    using (var context = eng.AcquireContext())
                    {
                        var jsObj = eng.CreateObject();
                        for (var i = 0; i < names.Length; i++)
                        {
                            jsObj.SetPropertyByName(names[i], eng.Converter.FromObject(values[i]));
                        }

                        registration.Item1.Invoke(new[] { jsObj });
                    }
                }, null);
            }

        }
    }
}
