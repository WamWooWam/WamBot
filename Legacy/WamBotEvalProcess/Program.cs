using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotEval.Languages;

namespace WamBotEval
{
    class Program
    {
        internal static DiscordRestClient Discord { get; set; }

        internal static DiscordMessage Message { get; set; }

        static string token;
        static ulong messageId;
        static ulong channelId;

        internal static void InitialiseDiscord()
        {
            Discord = new DiscordRestClient(new DiscordConfiguration()
            {
                Token = token
            });

            Discord.InitializeAsync().GetAwaiter().GetResult();
            Message = Discord.GetMessageAsync(channelId, messageId).GetAwaiter().GetResult();
        }

        static void Main(string[] args)
        {
            if (args.Length == 6)
            {
                string lang = args[0];
                string code = Encoding.UTF8.GetString(Convert.FromBase64String(args[1]));
                string pipeid = args[2];
                string basePath = args[3];

                messageId = ulong.Parse(args[4]);
                channelId = ulong.Parse(args[5]);

                if (File.Exists(Path.Combine(basePath, "config.json")))
                {
                    token = JObject.Parse(File.ReadAllText(Path.Combine(basePath, "config.json")))["Token"].ToObject<string>();
                }

                PipeStream pipeStream = new AnonymousPipeClientStream(PipeDirection.Out, pipeid);
                StreamWriter writer = new StreamWriter(pipeStream) { AutoFlush = true };

                switch (lang)
                {
                    case "lua":
                        LuaEval.RunEval(code, pipeStream, writer);
                        break;
                    case "js":
                        JsEval.RunEval(code, pipeStream, writer);
                        break;
                    case "cs":
                        CSharpEval.RunEval(code, pipeStream, writer);
                        break;
                    default:
                        Console.WriteLine("Unsupported language.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("This is an internal tool to be used by WamBot's eval commands. Use outside will cause unexpected results and is not recommended.");
                Console.Write("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }
    }
}
