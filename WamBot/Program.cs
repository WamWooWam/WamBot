using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WamBot.Core;

namespace WamBot
{
    class Program
    {
        private static Config _config;

        static async Task Main(string[] args)
        {
            if (File.Exists("config.json"))
            {
                try
                {
                    string str = File.ReadAllText("config.json");
                    _config = JsonConvert.DeserializeObject<Config>(str);

                    if (_config == null)
                    {
                        ConfigLoadFailure();
                    }

                }
                catch
                {
                    ConfigLoadFailure();
                }
            }
            else
            {
                _config = new Config();
                File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));
            }
            
                BotContext context = new BotContext(_config);
                context.LogMessage += Context_LogMessage;
                context.DSharpPlusLogMessage += Context_DSharpPlusLogMessage;

                await context.ConnectAsync();
            

            await Task.Delay(-1);
        }

        private static void Context_DSharpPlusLogMessage(object sender, DSharpPlus.EventArgs.DebugLogMessageEventArgs e)
        {
            Console.WriteLine(e);
        }

        private static void Context_LogMessage(object sender, string e)
        {
            Console.WriteLine(e);
        }

        private static void ConfigLoadFailure()
        {
            throw new NotImplementedException();
        }
    }
}
