using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;
using WamBotRewrite.Commands;
using WamWooWam.Core;

namespace WamBotRewrite
{
    static class ImageCommandSupport
    {
        static JpegEncoder _jpgEncoder = new JpegEncoder() { Quality = 75 };
        static PngEncoder _pngEncoder = new PngEncoder() { CompressionLevel = 4 };

        public static async Task ReplyAsync(this CommandContext ctx, Image<Rgba32> image, Discord.EmbedBuilder emb = null)
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
                    image.SaveAsJpeg(str, _jpgEncoder);
                    ext = ".jpg";
                }
                else
                {
                    image.SaveAsPng(str, _pngEncoder);
                }
            }

            str.Seek(0, SeekOrigin.Begin);

            ImageCommands.ImageCache[ctx.Channel.Id] = image;
            string file = Strings.RandomString(12) + (ext);

            if (emb != null)
            {
                emb.WithImageUrl($"attachment://{file}");
            }

            if (ctx.ChannelData != null)
                ctx.ChannelData.MessagesSent += 1;

            await ctx.Channel.SendFileAsync(str, file, embed: emb?.Build());
        }
    }
}
