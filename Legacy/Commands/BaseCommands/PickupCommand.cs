using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    class PickupCommand : DiscordCommand
    {
        static HttpClient _client;

        public PickupCommand()
        {
            if (_client == null)
                _client = new HttpClient();
        }

        public override string Name => "Pickup";

        public override string Description => "Grabs a random pickup line";

        public override string[] Aliases => new[] { "pickup" };

        public async Task<CommandResult> RunAsync()
        {
            try
            {
                string str = await _client.GetStringAsync("http://pebble-pickup.herokuapp.com/tweets/random");
                JObject obj = JObject.Parse(str);
                JToken tweet = obj["tweet"];
                return $"\"{tweet.ToObject<string>()}\"";
            }
            catch
            {
                throw new CommandException("Oops! That didn't work! Sorry!");
            }
        }
    }
}
