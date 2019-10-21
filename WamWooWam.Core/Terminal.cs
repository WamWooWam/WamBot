using System;
using System.Collections.Generic;
using System.Text;

namespace WamWooWam.Core
{
    public static class Terminal
    {
        public static void WriteColoured(string text, ConsoleColor color)
        {
            var prevColour = Console.ForegroundColor;
            Console.ForegroundColor = color;

            Console.WriteLine(text);

            Console.ForegroundColor = color;
        }
    }
}
