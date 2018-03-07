using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamBot.Api;

namespace BaseCommands
{
    [Owner]
    class RepeatCommand : DiscordCommand
    {
        static Dictionary<ulong, CancellationTokenSource> CancelationTokens = new Dictionary<ulong, CancellationTokenSource>();

        public override string Name => "Repeat";

        public override string Description => "Don't run this...";

        public override string[] Aliases => new[] { "rep" };

        public CommandResult Cancel()
        {
            if(CancelationTokens.TryGetValue(Context.Channel.Id, out var src))
            {
                src.Cancel();
                return ("Cancelled!");
            }

            return ("No task to cancel!");
        }

        public async Task<CommandResult> Run(int no, TimeSpan delay, params string[] args)
        {
            CancelationTokens[Context.Channel.Id] = new CancellationTokenSource();

            for (int i = 0; i < no; i++)
            {
                CancelationTokens[Context.Channel.Id].Token.ThrowIfCancellationRequested();
                await Context.Channel.SendMessageAsync(string.Join(" ", args));
                await Task.Delay(delay, CancelationTokens[Context.Channel.Id].Token);
            }

            return CommandResult.Empty;
        }
    }
}
