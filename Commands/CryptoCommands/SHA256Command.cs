using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace CryptoCommands
{
    class SHA256Command : ModernDiscordCommand
    {
        public override string Name => "SHA256";

        public override string Description => "Generates a SHA256 hash of the given UTF8 text.";

        public override string[] Aliases => new[] { "sha", "sha256" };

        private static Lazy<SHA256> sha = new Lazy<SHA256>(() => SHA256.Create());

        public CommandResult Run(string text)
        {
            return ($"\"{text}\" = \"{Tools.HashToString(sha.Value.ComputeHash(Encoding.UTF8.GetBytes(text)))}\"");
        }
    }
}
