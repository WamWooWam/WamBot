using Discord;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Commands;

namespace WamBotRewrite.Api.Converters
{
    class ImageConverter : IParamConverter
    {
        static HttpClient _client;

        public ImageConverter()
        {
            _client = new HttpClient();
        }

        public Type[] AcceptedTypes => new[] { typeof(Image<Rgba32>) };

        public async Task<object> Convert(string arg, Type to, CommandContext context)
        {
            Image<Rgba32> finishedImage = null;

            Uri url = null;
            var attachment = context.Message.Attachments.FirstOrDefault();
            if (attachment != null && attachment.Width > 0 && attachment.Height > 0)
            {
                url = new Uri(attachment.Url);
            }
            else
            {
                foreach (string a in context.Arguments)
                {
                    if (Uri.TryCreate(a, UriKind.Absolute, out url))
                    {
                        var temp = context.Arguments.ToList();
                        temp.Remove(a);
                        context.Arguments = temp.ToArray();

                        break;
                    }
                }
            }

            if (url == null)
            {
                if (ImageCommands.ImageCache.TryGetValue(context.Channel.Id, out var img) && img != null)
                {
                    return img;
                }
                else
                {
                    var messages = await context.Channel.GetMessagesAsync(context.Message, Direction.Before, 10).FlattenAsync();
                    var msg = messages.FirstOrDefault(m => m.Attachments.Any() && m.Attachments.FirstOrDefault()?.Width != 0);

                    if (msg != null)
                    {
                        attachment = msg.Attachments.FirstOrDefault();
                        if (attachment != null && attachment.Width > 0 && attachment.Height > 0)
                        {
                            url = new Uri(attachment.Url);
                        }
                    }
                }
            }

            finishedImage = SixLabors.ImageSharp.Image.Load<Rgba32>(await _client.GetStreamAsync(url));
            return finishedImage;
        }
    }
}
