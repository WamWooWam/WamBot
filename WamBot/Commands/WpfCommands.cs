using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Markdig;
using Markdig.Wpf;
using Microsoft.Extensions.Hosting;
using Microsoft.Scripting.JavaScript;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WamBot.Services;
using WamWooWam.Wpf;

namespace WamBot.Commands
{
    [Group("wpf")]
    [Description("Commands involving the Windows Presentation Foundation")]
    public class WpfCommands : BaseCommandModule
    {
        private IHostEnvironment _environment;
        private HttpClient _client;

        public WpfCommands(HttpClient client, IHostEnvironment environment)
        {
            _environment = environment;
            _client = client;
        }

        [Command]
        [Description("Renders XAML code")]
        [Aliases("xaml", "render")]
        public async Task DrawXAMLAsync(CommandContext ctx, [RemainingText] string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
            {
                var attach = ctx.Message.Attachments.FirstOrDefault();
                if (attach != null)
                {
                    xaml = await _client.GetStringAsync(attach.Url);
                }
                else
                {
                    await ctx.RespondAsync("No XAML found!");
                }
            }

            xaml = xaml.Trim().TrimStart('`').TrimEnd('`');

            if (xaml.Contains('<'))
            {
                if (!xaml.StartsWith("<"))
                {
                    xaml = xaml.Substring(xaml.IndexOf('<'));
                }

                try
                {
                    await DoRenderAsync(ctx, xaml);
                }
                catch (Exception ex)
                {
                    await ctx.SendFailureMessageAsync("An error occured while processing xaml.", ex);
                }
            }
        }
        [Command]
        [Description("Renders Markdown via XAML")]
        [Aliases("md")]
        public async Task DrawMarkdownAsync(CommandContext ctx, [RemainingText] string md)
        {
            if (string.IsNullOrWhiteSpace(md))
            {
                var attach = ctx.Message.Attachments.FirstOrDefault();
                if (attach != null)
                {
                    md = await _client.GetStringAsync(attach.Url);
                }
                else
                {
                    await ctx.RespondAsync("No XAML found!");
                }
            }

            try
            {
                var dispatcher = Application.Current.Dispatcher;
                var pipeline = new MarkdownPipelineBuilder()
                    .UseSupportedExtensions()
                    .Build();

                var doc = await dispatcher.InvokeAsync(() =>
                {
                    var doc = Markdig.Wpf.Markdown.ToFlowDocument(md, pipeline);
                    doc.Background = Brushes.White;

                    var presenter = new FlowDocumentScrollViewer()
                    {
                        Background = Brushes.White,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        Document = doc,
                        Margin = new Thickness(-4)
                    };

                    var container = new StackPanel();
                    container.Children.Add(presenter);
                    container.CanHorizontallyScroll = false;
                    container.CanVerticallyScroll = false;

                    return container;
                });


                using (var stream = new MemoryStream())
                {
                    await dispatcher.InvokeAsync(() => RenderToStream(doc, stream, false));
                    stream.Seek(0, SeekOrigin.Begin);
                    await ctx.Channel.SendFileAsync("xaml.png", stream);
                }
            }
            catch (Exception ex)
            {
                await ctx.SendFailureMessageAsync("An error occured while processing xaml.", ex);
            }
        }

        public async Task DoRenderAsync(CommandContext ctx, string xaml)
        {
            var parserContext = new ParserContext() { BaseUri = new Uri(_environment.ContentRootPath) };
            parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            parserContext.XmlnsDictionary.Add("sys", "clr-namespace:System;assembly=mscorlib");

            var dispatcher = Application.Current.Dispatcher;

            var dataContext = new { Channel = ctx.Channel, Guild = ctx.Guild, Author = ctx.Member ?? ctx.User };
            var obj = await dispatcher.InvokeAsync(() => ParseXaml(xaml, parserContext, dataContext));

            if (obj is Exception ex)
            {
                throw new ArgumentException($"XAML Parsing failed! {ex.Message}");
            }

            if (!(obj is ContentPresenter element))
            {
                throw new ArgumentException("Okay what the fuck?!");
            }

            using (var stream = new MemoryStream())
            {
                await dispatcher.InvokeAsync(() => RenderToStream(element, stream, false));
                stream.Seek(0, SeekOrigin.Begin);

                await ctx.RespondWithFileAsync("xaml.png", stream);
            }
        }

        private static void RenderToStream(FrameworkElement element, Stream stream, bool light)
        {
            var parent = new Border();
            //Themes.SetTheme(parent, new ThemeConfiguration()
            //{
            //    AccentColour = Color.FromArgb(0xFF, 0x72, 0x89, 0xDA),
            //    ColourMode = light ? ThemeColourMode.Light : ThemeColourMode.Dark
            //});

            ////parent.Background = (Brush)parent.FindResource("SystemChromeLowBrush");

            var container = new Grid() { Margin = new Thickness(4) };
            container.Children.Add(element);
            parent.Child = container;

            parent.InvalidateVisual();
            parent.UpdateDefaultStyle();
            parent.Measure(new Size(float.PositiveInfinity, float.PositiveInfinity));
            parent.Arrange(new Rect(container.DesiredSize));

            parent.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            var bmp = new RenderTargetBitmap((int)parent.DesiredSize.Width, (int)parent.DesiredSize.Height, 96, 96, PixelFormats.Default);
            bmp.Render(parent);

            var encoder = new PngBitmapEncoder();
            var frame = BitmapFrame.Create(bmp);
            encoder.Frames.Add(frame);
            encoder.Save(stream);
        }

        private static object ParseXaml(string xaml, ParserContext parserContext, object dataContext = null)
        {
            var contentPresenter = new ContentPresenter()
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                DataContext = dataContext
            };

            try
            {
                var content = XamlReader.Parse(xaml, parserContext);
                contentPresenter.Content = content;

                if (content is FrameworkElement element)
                {
                    element.DataContext = dataContext;

                    var children = VisualTreeHelper.GetChildrenCount(element);
                    for (int i = 0; i < children; i++)
                    {
                        var child = VisualTreeHelper.GetChild(element, i);
                        if (child is FrameworkElement obj && obj.DataContext == null)
                        {
                            obj.DataContext = dataContext;
                        }
                    }
                }

                return contentPresenter;
            }
            catch (Exception ex1)
            {
                return ex1;
            }
        }
    }
}
