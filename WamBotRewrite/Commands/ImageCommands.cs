using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;
using System.ComponentModel.DataAnnotations;
using SixLabors.Shapes;
using SixLabors.Primitives;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using Discord;

namespace WamBotRewrite.Commands
{
    [RequiresGuild]
    class ImageCommands : CommandCategory
    {
        internal static Dictionary<ulong, Image<Rgba32>> ImageCache { get; private set; } = new Dictionary<ulong, Image<Rgba32>>();

        public override string Name => "Image";

        public override string Description => "Commands to manipulate and mess with images, powered by ImageSharp!";

        [Command("Create", "Creates a new blank image.", new[] { "create", "new" })]
        public async Task Create(CommandContext ctx, [Range(0, 4096)]int width = 300, [Range(0, 4096)]int height = 300, Rgba32? color = null)
        {
            if (ImageCache.TryGetValue(ctx.Guild.Id, out var oldImage))
            {
                oldImage.Dispose();
            }

            Image<Rgba32> newImage = new Image<Rgba32>(width, height);
            newImage.Mutate(m => m.Fill(color ?? new Rgba32(255, 255, 255)));

            await ctx.ReplyAsync(newImage);
        }

        [Command("Ellipse", "Draws an ellipse on to an image.", new[] { "ellipse" })]
        public async Task Ellipse(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(0, float.MaxValue)] float x, [Range(0, float.MaxValue)] float y, [Range(1, float.MaxValue)] float width, [Range(1, float.MaxValue)] float height, Rgba32? colour = null)
        {
            EllipsePolygon ellipse = new EllipsePolygon(x, y, width, height);
            image.Mutate(i => i.Fill(colour ?? new Rgba32(0, 0, 0), ellipse));
            await ctx.ReplyAsync(image);
        }

        [Command("Fill", "Fills and clears the canvas with a specified colour.", new[] { "fill", "clear" })]
        public async Task Fill(CommandContext ctx, [Implicit] Image<Rgba32> image, Rgba32 colour)
        {
            image.Mutate(m => m.Fill(colour));
            await ctx.ReplyAsync(image);
        }

        [Command("Polygon", "Draws a polygon on to an image.", new[] { "poly", "polygon" })]
        public async Task Polygon(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(0, float.MaxValue)] float x, [Range(0, float.MaxValue)] float y, [Range(1, int.MaxValue)] int vertecies, [Range(1, float.MaxValue)] float radius, Rgba32? colour = null)
        {
            colour = colour ?? new Rgba32(0, 0, 0);
            RegularPolygon poly = new RegularPolygon(new SixLabors.Primitives.PointF(x + radius, y + radius), vertecies, radius);
            image.Mutate(i => i.Fill(colour.Value, poly));
            await ctx.ReplyAsync(image);
        }

        [Command("Rectangle", "Draws a rectangle on to an image.", new[] { "rect" })]
        public async Task Rectangle(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(0, float.MaxValue)] float x, [Range(0, float.MaxValue)] float y, [Range(1, float.MaxValue)] float width, [Range(1, float.MaxValue)] float height, Rgba32? colour = null)
        {
            RectangleF rectangle = new RectangleF(x, y, width, height);
            image.Mutate(i => i.Fill(colour.Value, rectangle));
            await ctx.ReplyAsync(image);
        }

        [Command("Text", "Draws text on to an image.", new[] { "text", "txt" })]
        public async Task Text(CommandContext ctx, [Implicit]Image<Rgba32> image, string text, Rgba32? color = null, float fontSize = 14, FontFamily family = null, float? x = null, float? y = null)
        {
            color = color ?? new Rgba32(0, 0, 0);
            family = family ?? SystemFonts.Find("Segoe UI");
            Font font = family.CreateFont(fontSize, FontStyle.Regular);
            SizeF bounds = TextMeasurer.Measure(text, new RendererOptions(font) { ApplyKerning = true, WrappingWidth = image.Width - 20 });

            x = x ?? (image.Width - bounds.Width) / 2;
            y = y ?? (image.Height - bounds.Height) / 2;

            TextGraphicsOptions options = new TextGraphicsOptions(true)
            {
                ApplyKerning = true,
                WrapTextWidth = image.Width - 20,
                Antialias = true,
                AntialiasSubpixelDepth = 4
            };

            image.Mutate(m => m.DrawText(text, font, color.Value, new PointF(x.Value, y.Value), options));

            await ctx.ReplyAsync(image);
        }

        [Command("Black & White", "It don't matter if you're black or white. Especially if you use this command, then you can be both!", new[] { "bw" })]
        public async Task BlackWhite(CommandContext ctx, [Implicit] Image<Rgba32> image)
        {
            image.Mutate(m => m.BlackWhite());
            await ctx.ReplyAsync(image);
        }

        [Command("Gaussian Blur", "Gaussian blurs an image.", new[] { "blur", "gaussian" })]
        public async Task GaussianBlur(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(1, 192)] float radius = 10)
        {
            image.Mutate(m => m.GaussianBlur(radius));
            await ctx.ReplyAsync(image);
        }

        [Command("Gaussian Sharpen", "Gaussian sharpens an image.", new[] { "sharpen" })]
        public async Task GaussianSharpen(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(1, 192)] float delta = 5)
        {
            image.Mutate(m => m.GaussianSharpen(delta));
            await ctx.ReplyAsync(image);
        }

        [Command("Greyscale", "Makes an image greyscale.", new[] { "grey", "gray", "greyscale", "grayscale" })]
        public async Task Greyscale(CommandContext ctx, [Implicit] Image<Rgba32> image)
        {
            image.Mutate(m => m.Grayscale());
            await ctx.ReplyAsync(image);
        }

        [Command("Invert", "Inverts the colours of an image.", new[] { "invert" })]
        public async Task Invert(CommandContext ctx, [Implicit] Image<Rgba32> image)
        {
            image.Mutate(m => m.Invert());
            await ctx.ReplyAsync(image);
        }

        [Command("Oil Paint", "Applies an oil paint effect to an image.", new[] { "oil" })]
        public async Task OilPaint(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(1, 128)] int levels = 30, [Range(1, 128)] int brushSize = 10)
        {
            image.Mutate(m => m.OilPaint(30, 10));
            await ctx.ReplyAsync(image);
        }

        [Command("Pixelate", "Pixelates an image.", new[] { "pixelate" })]
        public async Task Pixelate(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(1, 128)] int size = 5)
        {
            image.Mutate(m => m.Pixelate(size));
            await ctx.ReplyAsync(image);
        }

        [Command("Saturation", "Adjusts the saturation of an image.", new[] { "sat", "saturation" })]
        public async Task Saturation(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(1, 256)] int sat = 5)
        {
            image.Mutate(m => m.Saturation(sat));
            await ctx.ReplyAsync(image);
        }

        [Command("Sepia", "Apples a sepia effect to an image.", new[] { "sepia" })]
        public async Task Sepia(CommandContext ctx, [Implicit] Image<Rgba32> image)
        {
            image.Mutate(i => i.Sepia());
            await ctx.ReplyAsync(image);
        }

        [Command("Opacity", "Adjusts an image's overall opacity.", new[] { "opacity", "alpha" })]
        public async Task Opacity(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(0, 1)] float opacity)
        {
            image.Mutate(i => i.Alpha(opacity));
            await ctx.ReplyAsync(image);
        }

        [Command("Rotate", "Rotates an image.", new[] { "rotate", "rot" })]
        public async Task Rotate(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(0, 360)]float degrees = 90)
        {
            image.Mutate(m => m.Rotate(degrees));
            await ctx.ReplyAsync(image);
        }

        [Command("Resize", "Resizes an image.", new[] { "resize" })]
        public async Task Resize(CommandContext ctx, [Implicit] Image<Rgba32> image, [Range(1, 300)]double percentage)
        {
            int width = 0;
            int height = 0;

            double percent = percentage / 100d;
            width = (int)(image.Width * percent);
            height = (int)(image.Height * percent);

            if (width <= 8192 && height <= 8192)
            {
                image.Mutate(i =>
                {
                    i.Resize(width, height);
                });

                await ctx.ReplyAsync(image);
            }
            else
            {
                throw new CommandException("Woah that image'd be waaaaayyy to big! No thanks!!");
            }
        }

        [Command("Meta", "Returns metadata from an image.", new[] { "meta" })]
        public async Task Meta(CommandContext ctx, [Implicit] Image<Rgba32> image)
        {
            EmbedBuilder b = ctx.GetEmbedBuilder("Image");
            b.AddField("Resolution", $"{image.Width}x{image.Height} ({image.Width * image.Height} pixels)", true);
            b.AddField("Frames", image.Frames.Count.ToString(), true);

            if (image.MetaData.ExifProfile != null)
            {
                foreach (var exif in image.MetaData.ExifProfile?.Values)
                {
                    b.AddField(exif.Tag.ToString(), exif.Value.ToString(), true);
                }
            }

            if (image.MetaData.IccProfile != null)
            {
                b.AddField("Class", image.MetaData.IccProfile.Header.Class.ToString(), true);
                b.AddField("Creation Date", image.MetaData.IccProfile.Header.CreationDate.ToString(), true);
                b.AddField("Creator Signature", image.MetaData.IccProfile.Header.CreatorSignature, true);
                b.AddField("Device Manafacturer", image.MetaData.IccProfile.Header.DeviceManufacturer.ToString(), true);
                b.AddField("Device Model", image.MetaData.IccProfile.Header.DeviceModel.ToString(), true);
                b.AddField("Device Attributes", image.MetaData.IccProfile.Header.DeviceAttributes.ToString(), true);
            }

            foreach (var property in image.MetaData.Properties)
            {
                if (!string.IsNullOrEmpty(property.Name) && !string.IsNullOrEmpty(property.Value))
                {
                    b.AddField(property.Name, property.Value, true);
                }
            }

            using (Image<Rgba32> tempImage = image.Clone())
            {
                Rgba32[] byteData = new Rgba32[1];
                tempImage.Mutate(m => m.Resize(1, 1));
                tempImage.SavePixelData(byteData);

                Rgba32 c = byteData[0];
                b.WithColor(new Color(c.R, c.G, c.B));
            }

            await ctx.ReplyAsync(image, b);
        }
    }
}
