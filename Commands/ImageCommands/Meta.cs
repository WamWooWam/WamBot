using SixLabors.ImageSharp;
using System;
using System.Globalization;
using WamBot.Api;

namespace ImageCommands
{
    public class Meta : ICommandsAssembly
    {
        public string Name => "Image";

        public string Description => "Commands to manipulate and mess with images, powered by ImageSharp";

        public Version Version => new Version(1, 0, 0, 0);
    }

    internal static class Tools
    {
        public static void GetSize(string w, string h, out int width, out int height)
        {
            if (!int.TryParse(w, out width) || !int.TryParse(h, out height))
            {
                throw new ArgumentException();
            }
        }

        public static Rgba32 GetColour(string arg)
        {
            if (uint.TryParse(arg.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint n))
            {
                return new Rgba32((byte)(n >> 16), (byte)(n >> 8 & 255), (byte)(n & 255));
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}
