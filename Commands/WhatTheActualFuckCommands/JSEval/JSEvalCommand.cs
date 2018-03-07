using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WhatTheActualFuckCommands.JSEval
{
    class JSEvalCommand : DiscordCommand
    {
        static HttpClient _client = new HttpClient();

        public override string Name => "JS Eval";

        public override string Description => "Evaluates JavaScript code";

        public override string[] Aliases => new[] { "js", "eval" };

        public async Task<CommandResult> RunAsync()
        {
            DiscordAttachment attach = Context.Message.Attachments.FirstOrDefault();
            if (attach != null)
            {
                string code = await _client.GetStringAsync(attach.Url);
                DiscordMessage message = await Context.ReplyAsync("Evaluating...");

                DiscordEmbedBuilder builder = Context.GetEmbedBuilder("Evaluation Result");
                builder.AddField("Code", $"```js\r\n{Static.ShrinkToEmbed(code)}\r\n```");
                Static.RunEvalProcess(Context.Message, code, "js", builder, RunJSEval);

                await message.DeleteAsync();
                return builder.Build();
            }

            return "Type or upload code!";
        }

        public async Task<CommandResult> RunAsync(params string[] rawcode)
        {
            string code = Context.Message.Content
                .Substring(Context.Message.Content.IndexOf(" "))
                .Trim()
                .Trim('`');

            DiscordMessage message = await Context.ReplyAsync("Evaluating...");

            DiscordEmbedBuilder builder = Context.GetEmbedBuilder("Evaluation Result");
            builder.AddField("Code", $"```js\r\n{Static.ShrinkToEmbed(code)}\r\n```");

            Static.RunEvalProcess(Context.Message, code, "js", builder, RunJSEval);

            await message.DeleteAsync();
            return builder.Build();
        }

        private static void RunJSEval(DiscordEmbedBuilder builder, JToken obj)
        {
            if (obj.Type == JTokenType.String)
            {
                builder.AddField("Eval Result", $"```\r\n{Static.ShrinkToEmbed(obj.ToObject<string>())}\r\n```");
            }
            else
            {
                try
                {
                    Exception ex = obj.ToObject<Exception>();
                    builder.WithColor(new DiscordColor(255, 0, 0));
                    builder.AddField("Result", $"Eval Failed ({ex.Message})");
                }
                catch
                {
                    builder.WithColor(new DiscordColor(255, 0, 0));
                    builder.AddField("Result", $"Eval Failed (unknown object type)");
                }
            }
        }
    }
}
