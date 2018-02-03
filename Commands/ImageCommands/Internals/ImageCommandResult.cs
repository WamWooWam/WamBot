using DSharpPlus.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WamBot.Api;
using WamWooWam.Core;

namespace ImageCommands.Internals
{
    public static class ImageCommandResult
    {
        static JpegEncoder jpgEncoder = new JpegEncoder() { Quality = 75 };
        static PngEncoder pngEncoder = new PngEncoder() { CompressionLevel = 4 };
        internal static Dictionary<ulong, Image<Rgba32>> ImageCache { get; set; } = new Dictionary<ulong, Image<Rgba32>>();

        public static CommandResult ToResult(this Image<Rgba32> image, CommandContext ctx, string caption = null, DiscordEmbedBuilder emb = null)
        {
            MemoryStream str = new MemoryStream();
            string ext = image.Frames.Count > 1 ? ".gif" : ".png";

            if (!image.MetaData.Properties.Any(p => p.Name == "Program"))
            {
                image.MetaData.Properties.Add(new ImageProperty("Program", "WamBot Image Commands"));
            }

            if (image.Frames.Count > 1)
            {
                image.SaveAsGif(str);
            }
            else
            {
                if (image.Width > 2048 || image.Height > 2048)
                {
                    image.SaveAsJpeg(str, jpgEncoder);
                    ext = ".jpg";
                }
                else
                {
                    image.SaveAsPng(str, pngEncoder);
                }
            }

            str.Seek(0, SeekOrigin.Begin);

            ImageCache[ctx.Channel.Id] = image;
            string file = Strings.RandomString(12) + (ext);

            if (emb != null)
            {
                emb.WithImageUrl($"attachment://{file}");
            }

            return new CommandResult()
            {
                ReturnType = ReturnType.File,
                FileName = file,
                Stream = str,
                ResultEmbed = emb?.Build(),
                ResultText = caption
            };
        }
    }
}
