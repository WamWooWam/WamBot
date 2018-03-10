using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;

namespace WamBotRewrite.Commands
{
    class APICommands : CommandCategory
    {
        //static HttpClient _client = new HttpClient();

        public override string Name => "API Commands";

        public override string Description => "Commands that pull data from external APIs";

        //[Command("Reddit", "Pulls a random post from a specified subreddit", new[] { "reddit" })]
        //public async Task Reddit(CommandContext ctx, string subName = "random")
        //{
        //    JToken obj = JToken.Parse(await _client.GetStringAsync($"http://reddit.com/r/{subName}/random.json"));
        //}
    }
}
