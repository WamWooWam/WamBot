using System;

namespace WamWooWam.Core
{
    [Obsolete("A treasure trove of bad ideas")]
    public static class ConsolePlus
    {
        public static bool Debug { private get; set; }

        /// <summary>
        /// ConsolePlus equivelent to Console.Write()
        /// </summary>
        /// <param name="text">Text to be writen</param>
        /// <param name="colour">The colour that text should be</param>
        public static void Write(string text, ConsoleColor colour = ConsoleColor.White)
        {
            var OrigColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            Console.Write(" " + text);
            Console.ForegroundColor = OrigColour;
        }

        /// <summary>
        /// ConsolePlus equivelent to Console.WriteLine()
        /// </summary>
        /// <param name="text">Text to be writen</param>
        /// <param name="colour">The colour that text should be</param>
        public static void WriteLine(string text, ConsoleColor colour = ConsoleColor.White)
        {
            var OrigColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            Console.WriteLine(" " + text);
            Console.ForegroundColor = OrigColour;
        }

        /// <summary>
        /// Writes text only if debug is enabled
        /// </summary>
        /// <param name="source">A short (4 char) string detailing the output's source</param>
        /// <param name="text">The text to be writen</param>
        public static void WriteDebug(string source, string text, ConsoleColor colour = ConsoleColor.Cyan)
        {
            if (Debug)
            {
                WriteLine($"{source}: {text}", colour);
            }
        }

        /// <summary>
        /// Writes text only if debug is enabled
        /// </summary>
        /// <param name="Text">The text to be writen</param>
        public static void WriteDebug(string Text, ConsoleColor Colour = ConsoleColor.Cyan)
        {
            if (Debug)
            {
                WriteLine($" {Text}", Colour);
            }
        }

        /// <summary>
        /// Writes a simple heading to the console, e.g.
        /// 
        ///  --- Example Heading ---
        ///  
        /// </summary>
        /// <param name="HeadingText">The text of the heading</param>
        /// <param name="NewLine">Add a new line after the heading?</param>
        /// <param name="colour">The colour the heading should be (defaults to yellow)</param>
        public static void WriteHeading(string HeadingText, bool NewLine = true, ConsoleColor colour = ConsoleColor.Yellow)
        {
            var OrigColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            Console.WriteLine();
            Console.WriteLine($" --- {HeadingText} --- ");
            if (NewLine)
            {
                Console.WriteLine();
            }

            Console.ForegroundColor = OrigColour;
        }

        /// <summary>
        /// Writes a smaller heading, with a subheading. e.g.
        /// 
        ///   -- Example Heading Thing --
        ///   Example Subheading
        ///   
        /// </summary>
        /// <param name="header">The heading text</param>
        /// <param name="subHeader">The subheading text</param>
        /// <param name="newLine">Add a new line after the heading?</param>
        /// <param name="colour">The colour the heading should be (defaults to yellow)</param>
        public static void WriteSubHeading(string header, string subHeader = null, bool newLine = true, ConsoleColor colour = ConsoleColor.Yellow)
        {
            var OrigColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            Console.WriteLine();
            Console.WriteLine($"  -- {header} --  ");
            if (!string.IsNullOrEmpty(subHeader))
            {
                Console.Write($" {subHeader}");
            }

            if (newLine)
            {
                Console.WriteLine();
            }

            Console.ForegroundColor = OrigColour;
        }

        public static void WriteHeading(string header, string subHeader, bool newLine = true, ConsoleColor colour = ConsoleColor.Yellow)
        {
            var orig = Console.ForegroundColor;
            Console.WriteLine();
            Console.ForegroundColor = colour;
            Console.WriteLine($" --- {header} --- ");
            Console.Write($" {subHeader}");
            if (newLine)
            {
                Console.WriteLine();
            }
            Console.ForegroundColor = orig;
        }
    }
}
