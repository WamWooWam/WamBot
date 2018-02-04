using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using WamBot.Api;
using System.Globalization;

namespace MathsCommands
{
    class DecimalCommand : ModernDiscordCommand
    {
        public override string Name => "Decimal";

        public override string Description => "Converts a hex or binary string to an integer";

        public override string[] Aliases => new[] { "dec", "decimal", "int" };

        public CommandResult Run(string str)
        {
            if (str.StartsWith("0x") || str.Except(new[] { '0', '1' }).Any())
            {
                str = str.StartsWith("0x") ? str.Substring(2) : str;
                if(ulong.TryParse(str, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out ulong i))
                {
                    return i.ToString();
                }
                else
                {
                    return $"Unable to parse \"{str}\" as hex.";
                }
            }
            else
            {
                try
                {
                    ulong i = Convert.ToUInt64(str, 2);
                    return i.ToString();
                }
                catch
                {
                    return $"Unable to parse \"{str}\" as binary.";
                }
            }
        }
    }
}
