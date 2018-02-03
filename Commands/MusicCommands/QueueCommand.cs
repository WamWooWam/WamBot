using DSharpPlus.Entities;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicCommands.Models;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using WamBot.Api;
using WamWooWam.Core;
using WamWooWam.Core.Windows;

namespace MusicCommands
{
    [HttpClient]
    class QueueCommand : MusicCommand
    {
        private static HttpClient _httpClient;
        private Dictionary<string, SongModel> _ytCache = new Dictionary<string, SongModel>();

        public QueueCommand(HttpClient client)
        {
            _httpClient = client;
        }

        public override string Name => "Queue";

        public override string Description => "Queues a song to be played";

        public override string[] Aliases => new[] { "queue", "play" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x <= 1;

        public override bool Async => true;

        public override string Usage => "[Uri youtubeUrl/string filePath/[UploadedFile] Stream file]";

        public override async Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            SongModel model = new SongModel();

            if (args.Any() && !string.IsNullOrEmpty(args[0]))
            {
                if (File.Exists(args[0]))
                {
                    model.FilePath = args[0];
                    model.Title = Path.GetFileNameWithoutExtension(args[0]);
                }
                else if (Uri.TryCreate(args[0], UriKind.Absolute, out Uri uri))
                {
                    if (uri.Host.ToLowerInvariant().Contains("youtu"))
                    {
                        if (Static.YouTubeService == null)
                        {
                            string key = this.GetData<string>("apiKey");
                            if (key != null)
                            {
                                Static.YouTubeService = new YouTubeService(new BaseClientService.Initializer()
                                {
                                    ApplicationName = "WamBot",
                                    ApiKey = key
                                });
                            }
                            else
                            {
                                return "WamBot has not been configured to enable YouTube downloads.";
                            }
                        }

                        var queryDictionary = HttpUtility.ParseQueryString(uri.Query);
                        string id = queryDictionary["v"];
                        if(string.IsNullOrEmpty(id))
                        {
                            id = uri.Segments.LastOrDefault();
                        }

                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            string filePath = Path.Combine(Path.GetTempPath(), id + ".aac");
                            string thumbnailPath = Path.Combine(Path.GetTempPath(), id + ".jpg");

                            if (!File.Exists(filePath))
                            {
                                //if (File.Exists(filePath))
                                //    File.Delete(filePath);

                                //if (File.Exists(thumbnailPath))
                                //    File.Delete(thumbnailPath);

                                DiscordMessage message = await context.ReplyAsync($"Downloading video...");
                                Video video = await GetVideoFomIdAsync(id);

                                if (video != null)
                                {
                                    ChannelSnippet channel = await GetChannelFromVideoAsync(video);
                                    UpdateModel(model, video, channel);

                                    ProcessStartInfo info = new ProcessStartInfo("cmd", $"/c \"{Path.Combine(Directory.GetCurrentDirectory(), "youtube-dl.exe")} " +
                                        $"-v --youtube-skip-dash-manifest -4 --extract-audio --audio-format aac {uri} -o {Path.Combine(Path.GetTempPath(), id + ".%(ext)s")}\"")
                                    {
                                        WindowStyle = ProcessWindowStyle.Normal,
                                        CreateNoWindow = false,
                                        WorkingDirectory = Directory.GetCurrentDirectory(),
                                        UseShellExecute = true
                                    };

                                    Process process = new Process { StartInfo = info };
                                    process.Start();
                                    process.WaitForExit();

                                    if (!File.Exists(thumbnailPath))
                                    {
                                        using (FileStream thumbStr = File.Create(thumbnailPath))
                                        using (Stream remoteStr = await _httpClient.GetStreamAsync($"https://i.ytimg.com/vi/{id}/mqdefault.jpg"))
                                        {
                                            await remoteStr.CopyToAsync(thumbStr, 81920, connection.Token);
                                        }
                                    }

                                    _ytCache[id] = model;
                                }
                                else
                                {
                                    await context.ReplyAsync($"That video doesn’t exist!");
                                    return CommandResult.Empty;
                                }

                                try { await message.DeleteAsync(); } catch { }
                            }
                            else
                            {
                                if (_ytCache.TryGetValue(id, out var m))
                                {
                                    model = m;
                                }
                                else
                                {
                                    Video video = await GetVideoFomIdAsync(id);
                                    if (video != null)
                                    {
                                        ChannelSnippet channel = await GetChannelFromVideoAsync(video);
                                        UpdateModel(model, video, channel);
                                    }
                                    else
                                    {
                                        await context.ReplyAsync($"Unable to find an appropreate audio track.");
                                        return CommandResult.Empty;
                                    }
                                }
                            }

                            model.FilePath = filePath;
                            model.ThumbnailPath = thumbnailPath;
                        }
                        else
                        {
                            throw new CommandException("Ha! Thought you could trick me so easily? That's not a YouTube URL fool!");
                        }
                    }
                }
            }
            else if (context.Message.Attachments.Any())
            {
                using (MD5 sha = MD5.Create())
                {
                    DiscordAttachment attachment = context.Message.Attachments.First();
                    DiscordMessage message = await context.ReplyAsync($"Downloading \"{Path.GetFileName(attachment.FileName)}\"...");

                    string filePath = "";
                    using (Stream remoteStr = await _httpClient.GetStreamAsync(attachment.Url))
                    {
                        using (MemoryStream memStr = new MemoryStream())
                        {
                            await remoteStr.CopyToAsync(memStr);
                            memStr.Seek(0, SeekOrigin.Begin);

                            byte[] hash = sha.ComputeHash(memStr);
                            string name = string.Join("", hash.Select(b => b.ToString("x2")));
                            filePath = Path.Combine(Path.GetTempPath(), name + Path.GetExtension(attachment.FileName));
                            if (!File.Exists(filePath))
                            {
                                using (FileStream str = File.Create(filePath))
                                {
                                    memStr.Seek(0, SeekOrigin.Begin);
                                    await memStr.CopyToAsync(str, 81920, connection.Token);
                                }
                            }
                        }
                    }

                    model.FilePath = filePath;
                    model.Title = Path.GetFileNameWithoutExtension(attachment.FileName).Replace("_", " ");

                    try { await message.DeleteAsync(); } catch { }
                }
            }
            else
            {
                DiscordEmbedBuilder builder = context.GetEmbedBuilder("Current Queue");
                if (connection.Songs.Any() || connection.NowPlaying != null)
                {
                    if (connection.NowPlaying != null)
                    {
                        AddSongField(builder, connection.NowPlaying, true);
                    }

                    foreach (SongModel song in connection.Songs)
                    {
                        AddSongField(builder, song, false);
                    }
                }
                else
                {
                    builder.AddField("No songs!", "You'll need to add a song or two! You're already using the right command, come on!");
                }

                return builder.Build();
            }

            string defaultThumb = Path.ChangeExtension(model.FilePath, ".png");
            if (!string.IsNullOrEmpty(model.FilePath))
            {
                try
                {
                    using (TagLib.File tag = TagLib.File.Create(model.FilePath))
                    {
                        if (!tag.PossiblyCorrupt)
                        {
                            if (!string.IsNullOrWhiteSpace(tag.Tag.Title))
                            {
                                model.Title = tag.Tag.Title;
                            }

                            model.Album = tag.Tag.Album;
                            model.Artist = string.Join(", ", tag.Tag.Performers);

                            var art = tag.Tag.Pictures.FirstOrDefault();
                            if(art != null && !File.Exists(defaultThumb))
                            {
                                using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(art.Data.Data))
                                {
                                    using (FileStream str = File.OpenWrite(defaultThumb))
                                    {
                                        image.SaveAsPng(str);
                                        model.ThumbnailPath = defaultThumb;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(model.ThumbnailPath))
                {
                    if (File.Exists(defaultThumb))
                    {
                        model.ThumbnailPath = defaultThumb;
                    }
                    else
                    {
                        try
                        {
                            using (var img = Thumbnails.GetThumbnail(model.FilePath))
                            {
                                string path = defaultThumb;
                                img.Save(path, ImageFormat.Png);
                                model.ThumbnailPath = path;
                            }
                        }
                        catch { }
                    }
                }

                model.User = context.Invoker;
                connection.Songs.Enqueue(model);
                await context.ReplyAsync($"Queued: {model}");
            }

            return CommandResult.Empty;
        }

        private static void UpdateModel(SongModel model, Video video, ChannelSnippet channel)
        {
            model.Title = video.Snippet.Title;
            model.Album = video.Snippet.PublishedAt?.ToString() ?? "";
            model.Artist = channel.Title;
            model.Duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
        }

        private static async Task<ChannelSnippet> GetChannelFromVideoAsync(Video video)
        {
            var rawList = Static.YouTubeService.Channels.List("snippet");
            rawList.Id = video.Snippet.ChannelId;

            var list = await rawList.ExecuteAsync();
            return list.Items.FirstOrDefault()?.Snippet;
        }

        private static async Task<Video> GetVideoFomIdAsync(string id)
        {
            var rawList = Static.YouTubeService.Videos.List("snippet&contentDetails");
            rawList.Id = id;

            var list = await rawList.ExecuteAsync();
            return list.Items.FirstOrDefault();
        }

        private static void AddSongField(DiscordEmbedBuilder builder, SongModel song, bool playing)
        {
            builder.AddField(
                playing ? $"Now Playing: {song.Title}" : song.Title,
                (!string.IsNullOrWhiteSpace(song.Album) && !string.IsNullOrWhiteSpace(song.Artist) ? $"{song.Album} - {song.Artist}" :
                (!string.IsNullOrWhiteSpace(song.Artist) ? song.Artist :
                (!string.IsNullOrWhiteSpace(song.Album) ? song.Album : "No Additional Info"))) + " - " + song.User.Mention,
                true);
        }
    }
}
