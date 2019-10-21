using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Scripting.JavaScript;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace WamBot.Commands
{
    [Group("js")]
    [Aliases("javascript")]
    public class JavaScriptCommands : BaseCommandModule
    {
        private static ConcurrentDictionary<string, JavaScriptRuntime> _sharedEngines
            = new ConcurrentDictionary<string, JavaScriptRuntime>();
        private static ConcurrentDictionary<ulong, SemaphoreSlim> _sharedSemaphores
            = new ConcurrentDictionary<ulong, SemaphoreSlim>();

        private HttpClient _client;

        static JavaScriptCommands()
        {

        }

        public JavaScriptCommands(HttpClient client)
        {
            _client = client;
        }

        [Command]
        [Aliases("run")]
        [Description("Executes JavaScript code, probably a really bad idea.")]
        public async Task ExecuteJsAsync(CommandContext ctx, [RemainingText] string js)
        {
            var timeout = ctx.Guild != null ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(1);

            if (!_sharedSemaphores.TryGetValue(ctx.Guild?.Id ?? 0, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _sharedSemaphores[ctx.Guild?.Id ?? 0] = semaphore;
            }

            await semaphore.WaitAsync();
            var builder = ctx.GetEmbedBuilder("JavaScript");

            try
            {
                var runtime = GetSharedJSEngine(ctx, "eval");
                if (!runtime.IsExecutionEnabled)
                    runtime.EnableExecution();

                if (string.IsNullOrWhiteSpace(js))
                {
                    var attach = ctx.Message.Attachments.FirstOrDefault();
                    if (attach != null)
                    {
                        js = await _client.GetStringAsync(attach.Url);
                    }
                    else
                    {
                        await ctx.RespondAsync("No JS found!");
                    }
                }
                else
                {
                    js = js.Trim().Trim('`');
                    if (!js.StartsWith("\n") && js.Contains('\n'))
                    {
                        js = js.Remove(0, js.IndexOf('\n')).Trim();
                    }
                }

                builder.AddField("Code", $"```js\n{js.Truncate(1000)}\n```", false);

                var watch = new Stopwatch();
                JavaScriptExecutionContext context = null;
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                try
                {
                    using (var engine = runtime.CreateEngine())
                    {
                        context = engine.AcquireContext();

                        engine.SetGlobalVariable("global", engine.GlobalObject);
                        engine.SetGlobalVariable("channel", engine.Converter.FromObject(ctx.Channel));
                        engine.SetGlobalVariable("author", engine.Converter.FromObject(ctx.Member ?? ctx.User));
                        if (ctx.Guild != null)
                            engine.SetGlobalVariable("guild", engine.Converter.FromObject(ctx.Guild));

                        engine.SetGlobalFunction("element_at", JSElementAt);
                        engine.SetGlobalFunction("first", JSFirst);
                        engine.SetGlobalFunction("last", JSLast);
                        engine.SetGlobalFunction("to_array", JSToArray);

                        try
                        {
                            watch.Start();
                            var cancellation = new CancellationTokenSource(timeout);
                            var completion = new TaskCompletionSource<JavaScriptValue>(TaskCreationOptions.AttachedToParent);
                            cancellation.Token.Register(e =>
                            {
                                ((JavaScriptRuntime)e).DisableExecution();
                                completion.SetCanceled();
                            }, runtime);

                            var result = engine.EvaluateScriptText(js).Invoke(Enumerable.Empty<JavaScriptValue>());
                            //if (result is JavaScriptObject obj && obj.Prototype.GetPropertyByName("then") is JavaScriptFunction then)
                            //{
                            //    result = await completion.Task.ConfigureAwait(true);
                            //}

                            watch.Stop();

                            if (engine.HasException)
                            {
                                throw new Exception("whoops fuck");
                            }
                            else
                            {
                                builder.WithTitle("JS execution succeeded!")
                                       .WithColor(DiscordColor.Green)
                                       .AddField("Result", $"```json\n{(result?.ToString() ?? "undefined").Truncate(1000)}\n```", false);
                            }
                        }
                        catch (Exception e)
                        {
                            if (!runtime.IsExecutionEnabled)
                            {
                                runtime.EnableExecution();
                            }

                            if (engine.HasException)
                            {
                                var ex = engine.GetAndClearException();
                                if (ex != null)
                                {
                                    var json = engine.Converter.ToString(ex);

                                    if (json != "[object Object]")
                                    {
                                        builder.WithTitle("JS Execution failed!")
                                            .WithColor(DiscordColor.Red)
                                            .WithDescription(json);
                                        return;
                                    }
                                    else
                                    {
                                        e = (Exception)engine.Converter.ToObject(ex);
                                    }
                                }
                            }

                            builder.WithTitle("JS Execution failed!")
                                .WithColor(DiscordColor.Red)
                                .WithDescription(e.Message);
                        }
                    }
                }
                finally
                {
                    try
                    {
                        runtime.CollectGarbage();
                        if (!runtime.IsExecutionEnabled)
                            runtime.EnableExecution();
                    }
                    catch { }

                    context?.Dispose();

                    builder.WithFooter($"Evaluated in {watch.ElapsedTicks / (double)TimeSpan.TicksPerSecond}s - {builder.Footer.Text}");

                    await ctx.RespondAsync(embed: builder.Build());
                }

            }
            finally
            {
                semaphore.Release();
            }
        }

        //public static void ResetJSEngine(CommandContext ctx, IJsEngine engine, string use)
        //{
        //    try
        //    {
        //        engine.Interrupt();
        //        engine.Dispose();
        //    }
        //    catch { }

        //    engine = null;

        //    _sharedEngines.TryRemove($"{ctx.Guild?.Id ?? 0}-{use}", out _);
        //}

        public static JavaScriptRuntime GetSharedJSEngine(CommandContext ctx, string use, Action<JavaScriptEngine> creationAction = null)
        {
            if (!_sharedEngines.TryGetValue($"{ctx.Guild?.Id ?? 0}-{use}", out var engine))
            {
                engine = new JavaScriptRuntime(new JavaScriptRuntimeSettings() { AllowScriptInterrupt = true, EnableIdle = true, DisableFatalOnOOM = true });
                engine.MemoryChanging += (o, e) =>
                {
                    if (e.Type == JavaScriptMemoryAllocationEventType.AllocationRequest && (engine.RuntimeMemoryUsage + (ulong)e.Amount) > 268_435_456) // 256MB
                        e.Cancel = true;
                };

                _sharedEngines[$"{ctx.Guild?.Id ?? 0}-{use}"] = engine;
            }

            return engine;
        }

        #region JS Helpers
        private static JavaScriptValue JSElementAt(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            try
            {
                var array = arguments.FirstOrDefault();
                var index = callingEngine.Converter.ToInt32(arguments.ElementAtOrDefault(1));
                if (callingEngine.Converter.ToObject(array) is IEnumerable e)
                {
                    return callingEngine.Converter.FromObject(e.Cast<object>().ElementAtOrDefault(index));
                }
            }
            catch { }

            return callingEngine.UndefinedValue;
        }

        private static JavaScriptValue JSFirst(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            try
            {
                var array = arguments.FirstOrDefault();
                if (callingEngine.Converter.ToObject(array) is IEnumerable e)
                {
                    return callingEngine.Converter.FromObject(e.Cast<object>().First());
                }
            }
            catch { }

            return callingEngine.UndefinedValue;
        }

        private static JavaScriptValue JSLast(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            try
            {
                var array = arguments.FirstOrDefault();
                if (callingEngine.Converter.ToObject(array) is IEnumerable e)
                {
                    return callingEngine.Converter.FromObject(e.Cast<object>().Last());
                }
            }
            catch { }

            return callingEngine.UndefinedValue;
        }

        private static JavaScriptValue JSToArray(JavaScriptEngine callingEngine, bool asConstructor, JavaScriptValue thisValue, IEnumerable<JavaScriptValue> arguments)
        {
            try
            {
                var array = arguments.FirstOrDefault();
                if (callingEngine.Converter.ToObject(array) is IEnumerable e)
                {
                    return callingEngine.Converter.FromObject(e.Cast<object>().ToArray());
                }
            }
            catch { }

            return callingEngine.UndefinedValue;
        }

        #endregion

        public static void OnJsFailure(DiscordEmbedBuilder builder, Exception ex)
        {
            builder.WithTitle("JS execution failed!")
                   .WithColor(DiscordColor.Red);

            //if (ex is Microsoft.Scripting.JavaScript. jsEx)
            //{
            //    builder.WithDescription($"{jsEx.Category}: {jsEx.Message}");
            //    builder.AddField("Source Fragment", $"```js\n{(!string.IsNullOrWhiteSpace(jsEx.SourceFragment) ? jsEx.SourceFragment : "unknown")}\n```", false);
            //    builder.AddField("Line", jsEx.LineNumber.ToString(), true);
            //    builder.AddField("Column", jsEx.ColumnNumber.ToString(), true);
            //}
            //else
            //{
            builder.WithDescription(ex.Message);
            //}
        }
    }
}
