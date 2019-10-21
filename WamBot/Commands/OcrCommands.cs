using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SkiaSharp;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Web.Http;

namespace WamBot.Commands
{
    [Group("OCR")]
    public class OcrCommands : BaseCommandModule
    {
        private static HttpClient _httpClient
            = new HttpClient();

        [Command("Text")]
        [Description("Perform optical character recognition on an attached image.")]
        public async Task OcrAsync(CommandContext ctx, string language = "en-GB")
        {
            if (!ctx.Message.Attachments.Any(m => m.Width != 0))
            {
                await ctx.RespondAsync("No images have been specified!");
                return;
            }

            var lang = await GetOCRLanguageAsync(ctx, language);
            if (lang == null)
                return;

            var engine = OcrEngine.TryCreateFromLanguage(lang);

            foreach (var attachment in ctx.Message.Attachments)
            {
                var name = Path.GetTempFileName();
                var result = await RunOCRAsync(ctx, engine, new Uri(attachment.Url), name);

                var builder = new StringBuilder();
                builder.AppendLine("```");
                foreach (var line in result.Lines)
                {
                    builder.AppendLine(line.Text);
                }

                builder.AppendLine("```");
                await ctx.RespondAsync(builder.ToString());
            }
        }

        [Command("Text")]
        [Description("Perform optical character recognition on a specified image.")]
        public async Task OcrAsync(CommandContext ctx, Uri url, string language = "en-GB")
        {
            var name = Path.GetTempFileName();
            var lang = await GetOCRLanguageAsync(ctx, language);
            if (lang == null)
                return;

            var engine = OcrEngine.TryCreateFromLanguage(lang);
            var result = await RunOCRAsync(ctx, engine, url, name);

            var builder = new StringBuilder();
            builder.AppendLine("```");
            foreach (var line in result.Lines)
            {
                builder.AppendLine(line.Text);
            }

            builder.AppendLine("```");
            await ctx.RespondAsync(builder.ToString());
        }

        [Command("Generate")]
        [Description("Performs optical character recognition on an image, then generates a new image containing a formatted view of the resulting text.")]
        public async Task OcrGenerateAsync(CommandContext ctx, string language = "en-GB")
        {
            if (!ctx.Message.Attachments.Any(m => m.Width != 0))
            {
                await ctx.RespondAsync("No images have been specified!");
                return;
            }

            var lang = await GetOCRLanguageAsync(ctx, language);
            if (lang == null)
                return;

            var engine = OcrEngine.TryCreateFromLanguage(lang);

            foreach (var attachment in ctx.Message.Attachments)
            {
                OcrResult result = null;
                SoftwareBitmap softwareBitmap = null;
                var name = Path.ChangeExtension(Path.GetTempFileName(), "png");

                using (var file = File.Create(name))
                using (var resp = await _httpClient.GetAsync(new Uri(attachment.Url), HttpCompletionOption.ResponseHeadersRead))
                using (var content = await resp.Content.ReadAsInputStreamAsync())
                {
                    await content.AsStreamForRead().CopyToAsync(file);
                    file.Seek(0, SeekOrigin.Begin);

                    var decoder = await BitmapDecoder.CreateAsync(file.AsRandomAccessStream());

                    softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    result = await engine.RecognizeAsync(softwareBitmap);
                }

                if (!result.Lines.Any())
                {
                    await ctx.RespondAsync("OCR returned no text!");
                    return;
                }

                // divide the text into blocks
                // this looks complex but in reality it just looks at the distance between
                // the left of each line and the previous one, and splits if they're too far away

                // realistically this could probably work significantly better if it looked at the
                // rect as a whole but w/e

                var ocrBlocks = new List<List<OcrLine>>();
                var current = new List<OcrLine>();
                var line = result.Lines.First();
                current.Add(line);

                foreach (var l1 in result.Lines.Skip(1))
                {
                    var x1 = line.Words[0].BoundingRect.Left;
                    var y1 = line.Words[0].BoundingRect.Bottom;

                    var x2 = l1.Words[0].BoundingRect.Left;
                    var y2 = l1.Words[0].BoundingRect.Bottom;

                    // abusing trig formule is my kink
                    var distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                    if (distance < 100f)
                    {
                        current.Add(l1);
                    }
                    else
                    {
                        ocrBlocks.Add(current);
                        current = new List<OcrLine>() { l1 };
                    }

                    line = l1;
                }

                ocrBlocks.Add(current);

                // setup drawing context
                var imageInfo = new SKImageInfo(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(imageInfo);
                using var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                using var text = new SKPaint()
                {
                    Color = SKColors.Black,
                    IsAntialias = true,
                    SubpixelText = true,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI")
                };

                foreach (var block in ocrBlocks)
                {
                    // work out the average X position for this block

                    var x = 0f;
                    foreach (var l in block)
                    {
                        x += (float)l.Words[0].BoundingRect.Left;
                    }

                    x /= block.Count;

                    foreach (var l in block)
                    {
                        // work out the average Y position and text size for this line
                        var y = 0f;
                        var textSize = 0f;

                        foreach (var word in l.Words)
                        {
                            y += (float)word.BoundingRect.Bottom;
                            textSize += (float)(word.BoundingRect.Height);
                        }

                        y /= l.Words.Count;
                        textSize /= l.Words.Count;

                        // draw the whole line in one go
                        text.TextSize = textSize;
                        canvas.DrawText(l.Text, x, y, text);
                    }
                }

                canvas.Flush();

                using (var stream = new SKFileWStream(name))
                    SKPixmap.Encode(stream, surface.PeekPixels(), SKEncodedImageFormat.Png, 90);

                await ctx.RespondWithFileAsync(name);
                softwareBitmap.Dispose();
            }
        }


        private static async Task<Language> GetOCRLanguageAsync(CommandContext ctx, string language)
        {
            if (!Language.IsWellFormed(language))
            {
                await ctx.RespondAsync("That language isn't valid!");
                return null;
            }

            var lang = new Language(language);
            if (!OcrEngine.IsLanguageSupported(lang))
            {
                await ctx.RespondAsync("My computer doesnt support OCR in that language!");
                return null;
            }

            return lang;
        }

        private static async Task<OcrResult> RunOCRAsync(CommandContext ctx, OcrEngine engine, Uri url, string fileName)
        {
            using var resp = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            var type = resp.Content.Headers.ContentType;
            if (!type.MediaType.StartsWith("image"))
                return null;

            using var file = File.Create(fileName);
            using var content = await resp.Content.ReadAsInputStreamAsync();
            await content.AsStreamForRead().CopyToAsync(file);
            file.Seek(0, SeekOrigin.Begin);

            var decoder = await BitmapDecoder.CreateAsync(file.AsRandomAccessStream());
            using var bitmap = await decoder.GetSoftwareBitmapAsync();

            return await engine.RecognizeAsync(bitmap);
        }
    }
}
