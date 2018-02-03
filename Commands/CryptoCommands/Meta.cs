using System;
using System.Linq;
using System.Text;
using WamBot.Api;

namespace CryptoCommands
{
    public class Meta : ICommandsAssembly
    {
        public string Name => "Cryptography";

        public string Description => "Provides a basic set of cryptographic commands";

        public Version Version => new Version(1,0,0,0);
    }

    public static class Tools
    {
        public static string HashToString(byte[] hash)
        {
            return string.Join("", hash.Select(b => b.ToString("x2")));
        }
    }
}
