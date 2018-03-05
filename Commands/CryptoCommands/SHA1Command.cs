using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace CryptoCommands
{
    class SHA1Command : DiscordCommand
    {
        public override string Name => "SHA1";

        public override string Description => "Generates a SHA1 hash of the given UTF8 text.";

        public override string[] Aliases => new[] { "sha1" };

        private static Lazy<SHA1> sha = new Lazy<SHA1>(() => SHA1.Create());

        public CommandResult Run(string text)
        {
            return ($"\"{text}\" = \"{Tools.HashToString(sha.Value.ComputeHash(Encoding.UTF8.GetBytes(text)))}\"");
        }
    }
}
