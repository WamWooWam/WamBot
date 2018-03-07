using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    [Owner]
    [HttpClient]
    class HttpCancelCommand : BaseDiscordCommand
    {
        HttpClient _client;

        public HttpCancelCommand(HttpClient client)
        {
            _client = client;
        }

        public override string Name => "Http Cancel";

        public override string Description => "Cancels all pending HTTP requests on the default bot HttpClient";

        public override string[] Aliases => new[] { "httpcancel" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override Task<CommandResult> RunCommand(string[] args, CommandContext context)
        {
            _client.CancelPendingRequests();
            return Task.FromResult<CommandResult>("Canceled pending HTTP requests.");
        }
    }
}
