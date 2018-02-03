using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace CryptoCommands
{
    class MD5Command : ModernDiscordCommand, IDisposable
    {
        public override string Name => "MD5";

        public override string Description => "Generates a MD5 hash of the given UTF8 text.";

        public override string[] Aliases => new[] { "md5" };

        private static Lazy<MD5> md5 = new Lazy<MD5>(() => MD5.Create());

        public CommandResult Run(string text)
        {
            return ($"\"{text}\" = \"{Tools.HashToString(md5.Value.ComputeHash(Encoding.UTF8.GetBytes(text)))}\"");
        }

        public void Dispose()
        {
            md5.Value.Dispose();
        }
    }
}
