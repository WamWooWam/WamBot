using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace WamBotRewrite.Api
{
    /// <summary>
    /// Defines specific permissions a command needs in order to run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class PermissionsAttribute : Attribute
    {
        public GuildPermission BotPermissions { get; set; } = GuildPermission.SendMessages;
        public GuildPermission UserPermissions { get; set; } = GuildPermission.SendMessages;
    }
}
