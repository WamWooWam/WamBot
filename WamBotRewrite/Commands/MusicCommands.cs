using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WamBotRewrite.Api;
using WamBotRewrite.Models;
using WamWooWam.Core.Windows;

namespace WamBotRewrite.Commands
{
    class MusicCommands : CommandCategory
    {
        static HttpClient _httpClient = new HttpClient();
        private static Lazy<MD5> _md5 = new Lazy<MD5>(() => MD5.Create());
        static readonly ConcurrentDictionary<ulong, ConnectionModel> activeConnections = new ConcurrentDictionary<ulong, ConnectionModel>();

        public override string Name => "Music";

        public override string Description => "Allows me to connect to voice channels and play music!";

        [Command("Join", "Joins me to a voice channel.", new[] { "join" })]
        public async Task Join(CommandContext ctx)
        {
            if (ctx.Author is IGuildUser guildUser)
            {
                if (guildUser.VoiceChannel != null)
                {
                    if (!activeConnections.ContainsKey(guildUser.GuildId))
                    {
                        var client = await guildUser.VoiceChannel.ConnectAsync();
                        ConnectionModel model = new ConnectionModel(client);
                        activeConnections[guildUser.GuildId] = model;

                        await ctx.ReplyAsync($"Connected to {guildUser.VoiceChannel.Name}!");
                        await MusicPlayLoop(ctx.Channel, model);
                    }
                    else
                    {
                        await ctx.ReplyAsync("I'm already connected here! Fuck off!");
                    }
                }
                else
                {
                    await ctx.ReplyAsync("You'll need to be in voice to run this command!");
                }
            }
            else
            {
                await ctx.ReplyAsync("I'll need to be in a guild to do this!");
            }
        }

        [Command("Leave", "Makes me leave the voice channel I'm in.", new[] { "leave" })]
        public async Task Leave(CommandContext ctx)
        {
            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
            {
                connection.Connected = false;
                connection.TokenSource.Cancel();

                await connection.Connection.StopAsync();
                await ctx.ReplyAsync("Disconnected!");

                activeConnections.TryRemove(ctx.Guild.Id, out _);
            }
        }

        [IgnoreArguments]
        [Command("Queue", "Queues a song for me to play.", new[] { "queue", "play" })]
        public async Task Command(CommandContext ctx)
        {
            string[] args = ctx.Arguments;

            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
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
                            var message = await ctx.ReplyAsync("Downloading...");
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
                                    var message = await ctx.ReplyAsync("Attempting to download as direct URL...");
                                    using (Stream remoteStr = await _httpClient.GetStreamAsync(uri))
                                    using (FileStream str = File.Create(path))
                                    {
                                        await remoteStr.CopyToAsync(str);
                                    }

                                    model.FilePath = path;
                                }
                                catch (Exception ex)
                                {
                                    await ctx.ReplyAsync($"Failed to download URL. {ex.Message}");
                                }
                            }
                        }
                    }
                }
                else if (ctx.Message.Attachments.Any())
                {
                    IAttachment attachment = ctx.Message.Attachments.First();
                    IUserMessage message = await ctx.ReplyAsync($"Downloading \"{Path.GetFileName(attachment.Filename)}\"...");

                    string filePath = "";
                    using (Stream remoteStr = await _httpClient.GetStreamAsync(attachment.Url))
                    {
                        using (MemoryStream memStr = new MemoryStream())
                        {
                            await remoteStr.CopyToAsync(memStr);
                            memStr.Seek(0, SeekOrigin.Begin);

                            byte[] hash = _md5.Value.ComputeHash(memStr);
                            string name = string.Join("", hash.Select(b => b.ToString("x2")));
                            filePath = Path.Combine(Path.GetTempPath(), name + Path.GetExtension(attachment.Filename));
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
                    model.Title = Path.GetFileNameWithoutExtension(attachment.Filename).Replace("_", " ");

                    try { await message.DeleteAsync(); } catch { }
                }
                else
                {
                    EmbedBuilder builder = ctx.GetEmbedBuilder("Current Queue");
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

                    await ctx.Channel.SendMessageAsync(string.Empty, embed: builder.Build());
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
                                    using (var image = System.Drawing.Image.FromStream(new MemoryStream(art.Data.Data)))
                                    {
                                        using (FileStream str = File.OpenWrite(defaultThumb))
                                        {
                                            image.Save(str, System.Drawing.Imaging.ImageFormat.Png);
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
                                    img.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                                    model.ThumbnailPath = path;
                                }
                            }
                            catch { }
                        }
                    }

                    model.User = ctx.Author as IGuildUser;
                    connection.Songs.Enqueue(model);
                    await ctx.ReplyAsync($"Queued: {model}");
                }
            }
        }

        [Command("Skip", "Skips the song I'm currently playing.", new[] { "skip" })]
        public async Task Skip(CommandContext ctx)
        {
            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
            {
                if (connection.NowPlaying != null)
                {
                    await ctx.ReplyAsync($"Skipping {connection.NowPlaying}...");
                    connection.Skip = true;
                }
                else
                {
                    await ctx.ReplyAsync($"Nothing playing to skip!");
                }
            }
        }

        #region Tools
        private static async Task MusicPlayLoop(IMessageChannel channel, ConnectionModel connection)
        {
            try
            {
                AudioStream stream = connection.Connection.CreatePCMStream(AudioApplication.Music);

                while (connection.Connected)
                {
                    if (connection.Songs.TryDequeue(out SongModel model))
                    {
                        await channel.SendMessageAsync($"Now Playing: **{model}**");

                        try
                        {
                            using (MediaFoundationReader reader = new MediaFoundationReader(model.FilePath))
                            using (MediaFoundationResampler resampler = new MediaFoundationResampler(reader, 48000))
                            {
                                VolumeSampleProvider volume = new VolumeSampleProvider(resampler.ToSampleProvider()) { Volume = connection.Volume };
                                connection.SampleProvider = volume;
                                IWaveProvider waveProvider = connection.SampleProvider.ToWaveProvider16();

                                connection.Total = reader.TotalTime;
                                connection.NowPlaying = model;

                                byte[] buff = new byte[3840];
                                int br = 0;

                                await connection.Connection.SetSpeakingAsync(true);

                                while ((br = waveProvider.Read(buff, 0, buff.Length)) > 0)
                                {
                                    connection.Token.ThrowIfCancellationRequested();

                                    if (connection.Skip)
                                    {
                                        break;
                                    }

                                    if (br < buff.Length)
                                    {
                                        for (int i = br; i < buff.Length; i++)
                                        {
                                            buff[i] = 0;
                                        }
                                    }

                                    connection.Elapsed = reader.CurrentTime;

                                    await stream.WriteAsync(buff, 0, 3840, connection.Token);
                                    connection.RecordBuffer.AddSamples(buff, 0, buff.Length);
                                }

                                if (connection.Connected)
                                {
                                    await connection.Connection.SetSpeakingAsync(false);
                                }

                                connection.Skip = false;
                            }
                        }
                        catch (OperationCanceledException) { break; }
                        catch (InvalidOperationException) { break; }
                        catch (Exception ex)
                        {
                            await channel.SendMessageAsync($"Something went a bit wrong attempting to play that and a {ex.GetType().Name} was thrown. Sorry!\r\n{ex.Message}");
                        }
                    }

                    connection.NowPlaying = null;
                    connection.Skip = false;

                    await Task.Delay(500);
                }
            }
            catch
            {
                if (connection.Connected)
                {
                    throw;
                }
            }
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

        private static void AddSongField(EmbedBuilder builder, SongModel song, bool playing)
        {
            builder.AddField(
                playing ? $"Now Playing: {song.Title}" : song.Title,
                (!string.IsNullOrWhiteSpace(song.Album) && !string.IsNullOrWhiteSpace(song.Artist) ? $"{song.Album} - {song.Artist}" :
                (!string.IsNullOrWhiteSpace(song.Artist) ? song.Artist :
                (!string.IsNullOrWhiteSpace(song.Album) ? song.Album : "No Additional Info"))) + " - " + song.User.Mention,
                true);
        }

        private static async Task<ConnectionModel> GetConnectionModelAsync(CommandContext ctx)
        {
            if (ctx.Author is IGuildUser guildUser)
            {
                if (guildUser.VoiceChannel != null || guildUser.Id == Program.Application.Owner.Id)
                {
                    if (activeConnections.TryGetValue(guildUser.Guild.Id, out var connection))
                    {
                        return connection;
                    }
                    else
                    {
                        await ctx.ReplyAsync("I'm not connected here! Fuck off!");
                        return null;
                    }
                }
                else
                {
                    await ctx.ReplyAsync("You'll need to be in voice to run this command!");
                    return null;
                }
            }
            else
            {
                await ctx.ReplyAsync("I'll need to be in a guild to do this!");
                return null;
            }
        } 
        #endregion
    }
}
