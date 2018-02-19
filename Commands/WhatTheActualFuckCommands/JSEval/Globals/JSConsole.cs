using Microsoft.Scripting.JavaScript;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using WamBot.Api;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace WhatTheActualFuckCommands.JSEval.Globals
{
    public class JSConsole
    {
        public JSConsole()
        {
        }

        private CommandContext _context;

        public JSConsole(CommandContext context)
        {
            _context = context;
        }

        public void log(object str) => _context.ReplyAsync(str.ToString()).GetAwaiter().GetResult();

        public void log(string format, object[] args) => _context.ReplyAsync(string.Format(format, args)).GetAwaiter().GetResult();      
    }
}
