using Microsoft.Scripting.JavaScript;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus;

namespace WamBotEval.Languages.Globals
{
    public class JSConsole
    {
        public JSConsole()
        {
        }

        public void log(object str)
        {
            Program.InitialiseDiscord();
            Program.Message.RespondAsync(str.ToString()).GetAwaiter().GetResult();
        }

        public void log(string format, object[] args)
        {
            Program.InitialiseDiscord();
            Program.Message.RespondAsync(string.Format(format, args)).GetAwaiter().GetResult();
        }
    }
}
