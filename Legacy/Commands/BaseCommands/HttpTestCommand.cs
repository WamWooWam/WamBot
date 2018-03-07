using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    [HttpClient]
    public class HttpTestCommand : DiscordCommand
    {
        static HttpClient _client;

        public HttpTestCommand(HttpClient client)
        {
            _client = client;
        }

        public override string Name => "Http Test";

        public override string Description => "A command to test HttpClient implememtation";

        public override string[] Aliases => new string[] { "http" };
        
        public async Task<CommandResult> RunCommand(Uri url)
        {
            HttpResponseMessage resp = await _client.GetAsync(url);
            string content = await resp.Content.ReadAsStringAsync();

            return $"Got response from {url}\r\n\r\n```http\r\n" +
                $"HTTP/1.1 {(int)resp.StatusCode} {resp.ReasonPhrase}\r\n" +
                $"{resp.Headers.ToString()}\r\n" +
                $"```";
        }
    }
}
