using DSharpPlus.Entities;
using Microsoft.Scripting.JavaScript;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using WhatTheActualFuckCommands.JSEval.Globals;

namespace WhatTheActualFuckCommands.JSEval
{
    [HttpClient]
    class JSEvalCommand : ModernDiscordCommand
    {
        static HttpClient _client;

        public JSEvalCommand(HttpClient client)
        {
            if (_client == null)
            {
                _client = client;
            }
        }

        public override string Name => "JS Eval";

        public override string Description => "Evaluates JavaScript code";

        public override string[] Aliases => new[] { "js", "eval" };

        public async Task<CommandResult> RunAsync(params string[] rawcode)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string code = Context.Message.Content
                .Substring(Context.Message.Content.IndexOf(" "))
                .Trim()
                .Trim('`');

            DiscordEmbedBuilder builder = Context.GetEmbedBuilder("Evaluation Result");
            builder.AddField("Code", $"```js\r\n{Static.ShrinkToEmbed(code)}\r\n```");

            using (JavaScriptRuntime runtime = new JavaScriptRuntime(new JavaScriptRuntimeSettings()
            {
                AllowScriptInterrupt = true,
                DisableBackgroundWork = false,
                EnableIdle = true
            }))
            using (JavaScriptEngine engine = runtime.CreateEngine())
            {
                Task t = Task.Run(() =>
                {
                    try
                    {
                        using (engine.AcquireContext())
                        {
                            engine.AddTypeToGlobal<JSConsole>();
                            engine.AddTypeToGlobal<XMLHttpRequest>();
                            engine.SetGlobalVariable("tools", engine.Converter.FromObject(new Globals.Tools()));
                            engine.SetGlobalVariable("console", engine.Converter.FromObject(new JSConsole(Context)));

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
                                    builder.WithColor(new DiscordColor(255, 0, 0));
                                    HandleJsException(builder, engine, e);
                                }
                                else
                                {
                                    string str = engine.Converter.ToString(v);
                                    builder.AddField("Result", $"```\r\n{(str.Length > 1010 ? str.Substring(0, 1000) + "..." : str)}\r\n```");
                                }
                            }
                            catch (Exception ex)
                            {
                                builder.WithColor(new DiscordColor(255, 0, 0));

                                if (engine.HasException)
                                {
                                    var e = engine.GetAndClearException();
                                    HandleJsException(builder, engine, e);
                                }
                                else
                                {
                                    if (ex.Message == "JsErrorInDisabledState")
                                    {
                                        builder.AddField("Result", $"Eval Failed (Likely execution timeout.)");
                                    }
                                    else
                                    {
                                        builder.AddField("Result", $"Eval Failed ({ex.Message})");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        builder.WithColor(new DiscordColor(255, 0, 0));
                        builder.AddField("Result", $"Eval Failed ({ex.Message})");
                    }
                });
                t.Wait(10000);
                if (!t.IsCompleted)
                {
                    engine.Runtime.DisableExecution();
                }
            }
            builder.AddField("Evaluation Time (ms)", stopwatch.ElapsedMilliseconds.ToString());
            return builder.Build();
        }



        private static void HandleJsException(DiscordEmbedBuilder builder, JavaScriptEngine engine, JavaScriptValue e)
        {
            builder.AddField("Result", $"Eval Failed ({engine.Converter.ToString(e)})");
            try
            {
                string str = engine.Converter.ToString(((dynamic)e).stack);
                builder.AddField("JS Stack Trace", $"```\r\n{Static.ShrinkToEmbed(string.IsNullOrWhiteSpace(str) ? "Unavailable" : str)}\r\n```", false);
            }
            catch { }
        }

        private JavaScriptValue JsAtob(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            return callingEngine.Converter.FromString(Encoding.UTF8.GetString(Convert.FromBase64String(callingEngine.Converter.ToString(arguments.First()))));
        }

        private JavaScriptValue JsBtoa(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            return callingEngine.Converter.FromString(Convert.ToBase64String(Encoding.UTF8.GetBytes(callingEngine.Converter.ToString(arguments.First()))));
        }

        private JavaScriptValue JsGet(JavaScriptEngine a, bool b, JavaScriptValue c, IEnumerable<JavaScriptValue> d)
        {
            return a.Converter.FromObject(_client.GetStringAsync(a.Converter.ToString(d.First())).GetAwaiter().GetResult());
        }

        private JavaScriptValue JsPost(JavaScriptEngine a, bool b, JavaScriptValue c, IEnumerable<JavaScriptValue> d)
        {
            var post = _client.PostAsync(a.Converter.ToString(d.First()), new StringContent(a.Converter.ToString(d.ElementAt(1)))).GetAwaiter().GetResult();
            return a.Converter.FromObject(post.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }
    }
}
