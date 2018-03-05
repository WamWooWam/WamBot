using DSharpPlus;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace WamBot.Api
{
    public abstract class BaseDiscordCommand
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract string[] Aliases { get; }

        public abstract Func<int, bool> ArgumentCountPrecidate { get; }

        public virtual string Usage => null;

        public virtual Permissions RequiredPermissions => Permissions.SendMessages;

        public virtual Permissions? UserPermissions => null;

        public virtual Permissions? BotPermissions => null;

        public abstract Task<CommandResult> RunCommand(string[] args, CommandContext context);
    }
}
