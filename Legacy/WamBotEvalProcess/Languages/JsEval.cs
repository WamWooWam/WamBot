using DSharpPlus.Entities;
using Microsoft.Scripting.JavaScript;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using WamBotEval.Languages.Globals;

namespace WamBotEval.Languages
{
    class JsEval
    {
        static HttpClient _client = new HttpClient();

        internal static void RunEval(string code, PipeStream pipeStream, StreamWriter writer)
        {
            using (JavaScriptRuntime runtime = new JavaScriptRuntime())
            using (JavaScriptEngine engine = runtime.CreateEngine())
            {
                try
                {
                    using (engine.AcquireContext())
                    {
                        engine.AddTypeToGlobal<JSConsole>();
                        engine.AddTypeToGlobal<XMLHttpRequest>();
                        engine.SetGlobalVariable("tools", engine.Converter.FromObject(new Tools()));
                        engine.SetGlobalVariable("console", engine.Converter.FromObject(new JSConsole()));

                        engine.SetGlobalFunction("get", JsGet);
                        engine.SetGlobalFunction("post", JsPost);
                        engine.SetGlobalFunction("atob", JsAtob);
                        engine.SetGlobalFunction("btoa", JsBtoa);

                        try
                        {
                            var fn = engine.EvaluateScriptText($@"(function() {{ {code} }})();");
                            var v = fn.Invoke(Enumerable.Empty<JavaScriptValue>());

                            if (engine.HasException)
                            {
                                var e = engine.GetAndClearException();
                                writer.WriteLine(JsonConvert.SerializeObject(engine.Converter.ToObject(e)));
                            }
                            else
                            {
                                string o = engine.Converter.ToString(v);
                                writer.WriteLine(JsonConvert.SerializeObject(o));
                            }
                        }
                        catch (Exception ex)
                        {

                            if (engine.HasException)
                            {
                                var e = engine.GetAndClearException();
                                writer.WriteLine(JsonConvert.SerializeObject(engine.Converter.ToObject(e)));
                            }
                            else
                            {
                                writer.WriteLine(JsonConvert.SerializeObject(ex));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    writer.WriteLine(JsonConvert.SerializeObject(ex));
                }
            }
        }

        private static JavaScriptValue JsAtob(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            return callingEngine.Converter.FromString(Encoding.UTF8.GetString(Convert.FromBase64String(callingEngine.Converter.ToString(arguments.First()))));
        }

        private static JavaScriptValue JsBtoa(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            return callingEngine.Converter.FromString(Convert.ToBase64String(Encoding.UTF8.GetBytes(callingEngine.Converter.ToString(arguments.First()))));
        }

        private static JavaScriptValue JsGet(JavaScriptEngine a, bool b, JavaScriptValue c, IEnumerable<JavaScriptValue> d)
        {
            return a.Converter.FromObject(_client.GetStringAsync(a.Converter.ToString(d.First())).GetAwaiter().GetResult());
        }

        private static JavaScriptValue JsPost(JavaScriptEngine a, bool b, JavaScriptValue c, IEnumerable<JavaScriptValue> d)
        {
            var post = _client.PostAsync(a.Converter.ToString(d.First()), new StringContent(a.Converter.ToString(d.ElementAt(1)))).GetAwaiter().GetResult();
            return a.Converter.FromObject(post.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }
    }
}
