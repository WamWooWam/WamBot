using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace WhatTheActualFuckCommands.JSEval.Globals
{
    public class Tools
    {
        public Tools() { }
        public async Task wait(int delay) => await Task.Delay(delay);
    }
}
