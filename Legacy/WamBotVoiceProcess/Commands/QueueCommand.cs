using DSharpPlus.Entities;
using WamBotVoiceProcess.Models;
using Newtonsoft.Json.Linq;
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
using System.Text;
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
        private static Lazy<MD5> _md5 = new Lazy<MD5>(() => MD5.Create());
        private Dictionary<string, SongModel> _ytCache = new Dictionary<string, SongModel>();

        public QueueCommand(HttpClient client)
        {
            _httpClient = client;
        }

        public override string Name => "Queue";

        public override string Description => "Queues a song to be played";

        public override string[] Aliases => new[] { "queue", "play" };

        public override Func<int, bool> ArgumentCountPrecidate => x => x <= 1;

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
                    byte[] hash = _md5.Value.ComputeHash(Encoding.UTF8.GetBytes(uri.ToString()));
                    string name = "";

                    if (uri.Host.ToLowerInvariant().Contains("youtu"))
                    {
                        var queryDictionary = HttpUtility.ParseQueryString(uri.Query);
                        name = queryDictionary["v"];
                        if (string.IsNullOrEmpty(name))
                        {
                            name = uri.Segments.LastOrDefault();
                        }
                    }
                    else
                    {
                        name = string.Join("", hash.Select(b => b.ToString("x2")));
                    }

                    string path = Path.Combine(Path.GetTempPath(), name + ".m4a");
                    string thumbnailPath = Path.ChangeExtension(path, ".jpg");
                    string metaPath = Path.ChangeExtension(path, ".info.json");

                    model.Source = uri.ToString();

                    if (!File.Exists(path) || !File.Exists(metaPath))
                    {
                        var message = await context.ReplyAsync("Attempting to download via youtube-dl");
                        DownloadAndWait(uri, name, false);
                        await message.DeleteAsync();
                    }

                    if (File.Exists(metaPath))
                    {
                        JObject meta = JObject.Parse(File.ReadAllText(metaPath));

                        if (File.Exists(path))
                        {
                            model.FilePath = path;
                        }
                        else if (File.Exists(Path.ChangeExtension(path, ".aac")))
                        {
                            model.FilePath = Path.ChangeExtension(path, ".aac");
                        }

                        model.Title = meta["fulltitle"].ToObject<string>();
                        model.Album = meta["uploader"]?.ToObject<string>();
                        model.Artist = meta["extractor_key"].ToObject<string>();
                        if (meta.TryGetValue("duration", out var token))
                        {
                            model.Duration = TimeSpan.FromSeconds(token.ToObject<int>());
                        }
                        model.Description = meta["description"]?.ToObject<string>();

                        if (!File.Exists(thumbnailPath) && meta.TryGetValue("thumbnail", out var thumbToken))
                        {
                            using (FileStream thumbStr = File.Create(thumbnailPath))
                            using (Stream remoteStr = await _httpClient.GetStreamAsync(thumbToken.ToObject<string>()))
                            {
                                await remoteStr.CopyToAsync(thumbStr, 81920, connection.Token);
                            }
                        }

                        if (File.Exists(thumbnailPath))
                        {
                            model.ThumbnailPath = thumbnailPath;
                        }
                    }
                    else
                    {
                        if (File.Exists(path))
                        {
                            model.Title = "Metadata Unavailable";
                            model.FilePath = path;
                        }
                        else
                        {
                            try
                            {
                                var message = await context.ReplyAsync("Attempting to download as direct URL...");
                                using (Stream remoteStr = await _httpClient.GetStreamAsync(uri))
                                using (FileStream str = File.Create(path))
                                {
                                    await remoteStr.CopyToAsync(str);
                                }

                                model.FilePath = path;
                            }
                            catch (Exception ex)
                            {
                                return $"Failed to download URL. {ex.Message}";
                            }
                        }
                    }
                }
            }
            else if (context.Message.Attachments.Any())
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

                        byte[] hash = _md5.Value.ComputeHash(memStr);
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
                    using (var tag = TagLib.File.Create(model.FilePath))
                    {
                        if (!tag.PossiblyCorrupt)
                        {
                            if (!string.IsNullOrWhiteSpace(tag.Tag.Title))
                            {
                                model.Title = tag.Tag.Title;
                            }

                            if (string.IsNullOrWhiteSpace(model.Album) || string.IsNullOrWhiteSpace(model.Artist))
                            {
                                model.Album = tag.Tag.Album;
                                model.Artist = string.Join(", ", tag.Tag.Performers);
                            }

                            if (model.Duration == null)
                            {
                                model.Duration = tag.Properties.Duration;
                            }

                            var art = tag.Tag.Pictures.FirstOrDefault();
                            if (art != null && !File.Exists(defaultThumb))
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

                model.User = context.Author;
                connection.Songs.Enqueue(model);
                await context.ReplyAsync($"Queued: {model}");
            }

            return CommandResult.Empty;
        }

        private static void DownloadAndWait(Uri uri, string id, bool native)
        {
            ProcessStartInfo info = new ProcessStartInfo("cmd", $"/c \"{Path.Combine(Directory.GetCurrentDirectory(), "youtube-dl.exe")} " +
                $"-v {(native ? "" : "--write-info-json")} {(File.Exists(Path.Combine(Path.GetTempPath(), id + ".aac")) ? "--skip-download" : "")} -r 512K -f m4a/aac/bestaudio/worst --hls-prefer-ffmpeg -4 --extract-audio --audio-format m4a --audio-quality 128K {uri} -o {Path.Combine(Path.GetTempPath(), id + ".%(ext)s")}\"")
            {
                WindowStyle = ProcessWindowStyle.Normal,
                CreateNoWindow = false,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = true
            };

            Process process = new Process { StartInfo = info };
            process.Start();
            process.WaitForExit();
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
