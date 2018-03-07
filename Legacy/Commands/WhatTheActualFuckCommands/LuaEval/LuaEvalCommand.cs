using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamBot.Api;

namespace WhatTheActualFuckCommands.LuaEval
{
    class LuaEvalCommand : DiscordCommand
    {
        static HttpClient _client = new HttpClient();

        public override string Name => "Lua Eval";

        public override string Description => "Evaluates Lua code";

        public override string[] Aliases => new[] { "lua" };

        public async Task<CommandResult> RunAsync()
        {
            DiscordAttachment attach = Context.Message.Attachments.FirstOrDefault();
            if(attach != null)
            {
                string code = await _client.GetStringAsync(attach.Url);
                DiscordMessage message = await Context.ReplyAsync("Evaluating...");

                DiscordEmbedBuilder builder = Context.GetEmbedBuilder("Evaluation Result");
                builder.AddField("Code", $"```lua\r\n{Static.ShrinkToEmbed(code)}\r\n```");
                Static.RunEvalProcess(Context.Message, code, "lua", builder, ProcessLuaJson);

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
            builder.AddField("Code", $"```lua\r\n{Static.ShrinkToEmbed(code)}\r\n```");
            Static.RunEvalProcess(Context.Message, code, "lua", builder, ProcessLuaJson);

            await message.DeleteAsync();
            return builder.Build();
        }

        private static void ProcessLuaJson(DiscordEmbedBuilder builder, JToken obj)
        {
            if (obj.Type == JTokenType.Null)
            {
                builder.AddField("Result", $"```\r\nnil\r\n```");
            }
            else if (obj.Type == JTokenType.Array)
            {
                object[] objects = obj.ToObject<object[]>();
                builder.AddField("Result", $"```\r\n{Static.ShrinkToEmbed(string.Join(", ", objects))}\r\n```");
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
