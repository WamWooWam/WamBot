using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamBot.Api;
using WamWooWam.Core;

namespace WhatTheActualFuckCommands
{
    public class Meta : ICommandsAssembly
    {
        public string Name => "What The Actual Fuck";

        public string Description => "Look, I get bored, okay??";
    }

    public static class Static
    {
        public static string ShrinkToEmbed(string code)
        {
            return (code.Length > 1010 ? code.Substring(0, 1000) + "..." : code);
        }

        public static void RunEvalProcess(DiscordMessage invoker, string code, string langcode, DiscordEmbedBuilder builder, Action<DiscordEmbedBuilder, JToken> action)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                string evalExePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Tools", "WamBotEval.exe");

                if (File.Exists(evalExePath))
                {
                    Process evalProcess = new Process();
                    evalProcess.StartInfo.FileName = evalExePath;
                    using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
                    {
                        evalProcess.StartInfo.Arguments =
                            $"{langcode} " +
                            $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(code))} " +
                            $"{server.GetClientHandleAsString()} " +
                            $"\"{Directory.GetCurrentDirectory()}\" " +
                            $"{invoker.Id} " +
                            $"{invoker.ChannelId}";
                        evalProcess.StartInfo.UseShellExecute = false;

                        try
                        {
                            using (StreamReader reader = new StreamReader(server))
                            using (CancellationTokenSource source = new CancellationTokenSource())
                            {
                                evalProcess.Start();
                                string ret = string.Empty;
                                try
                                {
                                    Task.Run(() =>
                                    {
                                        ret = reader.ReadLine();
                                    }, source.Token);
                                }
                                catch { }

                                evalProcess.WaitForExit(10000);

                                if (!evalProcess.HasExited)
                                {
                                    source.Cancel();
                                    evalProcess.Kill();

                                    builder.WithEvalError("Timeout");
                                    return;
                                }
                                else
                                {
                                    if (evalProcess.ExitCode == 0)
                                    {
                                        if (!string.IsNullOrEmpty(ret))
                                        {
                                            try
                                            {
                                                JToken obj = JToken.Parse(ret);
                                                action(builder, obj);

                                            }
                                            catch (JsonException)
                                            {
                                                builder.WithEvalError("Failed to parse response");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            builder.WithEvalError("Process exited before returning");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        builder.WithEvalError($"Process exit code does not indicate success. Exited with code {evalProcess.ExitCode}");
                                        return;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            builder.WithEvalError(ex.Message);
                        }
                    }
                }
                else
                {
                    builder.WithEvalError("Eval executable not found.");
                }
            }
            finally
            {
                builder.AddField("Evaluation Time (ms)", watch.ElapsedMilliseconds.ToString(), true);
            }
        }

        public static DiscordEmbedBuilder WithEvalError(this DiscordEmbedBuilder builder, string error)
        {
            builder.WithColor(new DiscordColor(255, 0, 0));
            builder.AddField("Result", $"Eval Failed ({error})");

            return builder;
        }
    }
}
