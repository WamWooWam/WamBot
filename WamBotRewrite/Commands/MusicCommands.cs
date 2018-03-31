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
using WamBotRewrite.Data;
using WamBotRewrite.Models;
using WamWooWam.Core.Windows;

namespace WamBotRewrite.Commands
{
    [RequiresGuild]
    class MusicCommands : CommandCategory
    {
        private static Lazy<MD5> _md5 = new Lazy<MD5>(() => MD5.Create());
        private static readonly ConcurrentDictionary<ulong, ConnectionModel> _activeConnections = new ConcurrentDictionary<ulong, ConnectionModel>();

        public override string Name => "Music";

        public override string Description => "Allows me to connect to voice channels and play music!";

        [Command("Join", "Joins me to a voice channel.", new[] { "join" })]
        public async Task Join(CommandContext ctx)
        {
            if (ctx.Author is IGuildUser guildUser)
            {
                if (guildUser.VoiceChannel != null)
                {
                    if (!_activeConnections.ContainsKey(guildUser.GuildId))
                    {
                        var client = await guildUser.VoiceChannel.ConnectAsync();
                        ConnectionModel model = new ConnectionModel(client, guildUser.VoiceChannel, ctx);
                        _activeConnections[guildUser.GuildId] = model;

                        await ctx.ReplyAsync($"Connected to {guildUser.VoiceChannel.Name}!");

                        var data = await ctx.DbContext.Channels.GetOrCreateAsync(ctx.DbContext, (long)guildUser.VoiceChannel.Id, () => new Channel(guildUser.VoiceChannel));
                        data.Connections += 1;
                        await ctx.DbContext.SaveChangesAsync();

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

                _activeConnections.TryRemove(ctx.Guild.Id, out _);
            }
        }

        [Command("Stats", "Shows statistics about my current voice connection.", new[] { "mstats" })]
        public async Task Stats(CommandContext ctx)
        {
            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
            {
                EmbedBuilder builder = ctx.GetEmbedBuilder("Voice Stats");
                Process process = Process.GetCurrentProcess();

                builder.AddField("Region", connection.Channel.Guild.VoiceRegionId, true);
                builder.AddField("WS Ping", $"{connection.Connection.Latency}ms", true);
                builder.AddField("UDP Ping", $"{connection.Connection.UdpLatency}ms", true);
                builder.AddField("Connection Duration", (DateTime.Now - connection.ConnectTime).ToString(), true);
                builder.AddField("Bitrate", connection.Channel.Bitrate.ToString(), true);
                builder.AddField("Recording", connection.Recording.ToString(), true);

                await ctx.Channel.SendMessageAsync(string.Empty, embed: builder.Build());
            }
        }

        [Command("Download", "Returns the currently playing song as an MP3.", new[] { "mp3", "dl" })]
        public async Task Download(CommandContext ctx)
        {
            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
            {
                using (MediaFoundationReader reader = new MediaFoundationReader(connection.NowPlaying.FilePath))
                {
                    string file = Path.Combine(Path.GetTempPath(), $"{connection.NowPlaying}.mp3");
                    if (File.Exists(file))
                    {
                        await ctx.Channel.SendFileAsync(file, "Here's your song!");
                    }
                    else
                    {
                        MediaFoundationEncoder.EncodeToMp3(reader, file);
                        using (TagLib.File tag = TagLib.File.Create(file))
                        {
                            tag.Tag.Title = connection.NowPlaying.Title;
                            tag.Tag.Album = connection.NowPlaying.Album;
                            tag.Tag.AlbumArtists = new string[] { connection.NowPlaying.Artist };
                            // tag.Tag.Pictures = new TagLib.IPicture[] { new TagLib.Picture(connection.NowPlaying.ThumbnailPath) };
                            tag.Save();
                        }

                        await ctx.Channel.SendFileAsync(file, "Here's your song!");
                    }
                }
            }
        }

        [Command("Volume", "Adjusts the volume.", new[] { "vol", "volume" })]
        public async Task Volume(CommandContext ctx, float? v = null)
        {
            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
            {
                if (v != null && v != float.NaN)
                {
                    if (v > 2)
                    {
                        v = v / 100;
                    }

                    if (v > 2 && ctx.Author.Id != Program.Application.Owner.Id)
                    {
                        await ctx.ReplyAsync("Let's not kill people's ears eh?");
                        return;
                    }

                    if (v < 0.01 && ctx.Author.Id != Program.Application.Owner.Id)
                    {
                        await ctx.ReplyAsync("Little quiet don't you think?");
                        return;
                    }

                    connection.Volume = v.Value;
                    await ctx.ReplyAsync($"Volume set to {v * 100}%.");
                }
                else
                {
                    await ctx.ReplyAsync($"Current volume is {connection.Volume * 100}%");
                }
            }
        }

        [Command("Now Playing", "Get info on what I'm currently playing.", new[] { "playing" })]
        public async Task NowPlaying(CommandContext ctx)
        {
            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
            {
                EmbedBuilder builder = ctx.GetEmbedBuilder("Now Playing");
                if (connection.NowPlaying != null)
                {
                    SongModel song = connection.NowPlaying;
                    StringBuilder str = new StringBuilder();
                    bool album = false;

                    if (!string.IsNullOrWhiteSpace(song.Album))
                    {
                        album = true;
                        str.Append(song.Album);
                    }
                    if (!string.IsNullOrWhiteSpace(song.Artist))
                    {
                        if (album)
                        {
                            str.Append(" - ");
                        }
                        str.Append(song.Artist);
                    }

                    string details = str.ToString();
                    builder.AddField(song.Title, string.IsNullOrWhiteSpace(details) ? "No additional details" : details);
                    if (!string.IsNullOrWhiteSpace(song.Description))
                    {
                        builder.AddField("Description", song.Description.Length < 256 ? song.Description : song.Description.Substring(0, 252) + "...");
                    }
                    builder.AddField("Elapsed", $"{connection.Elapsed.ToString(@"mm\:ss")}/{(connection.NowPlaying.Duration ?? connection.Total).ToString(@"mm\:ss")}", true);
                    builder.AddField("Queued by", song.User.Mention, true);

                    if (!string.IsNullOrWhiteSpace(song.Source))
                    {
                        builder.AddField("Source", song.Source, true);
                    }

                    if (File.Exists(connection.NowPlaying.ThumbnailPath))
                    {
                        builder.WithThumbnailUrl($"attachment://{Path.GetFileName(connection.NowPlaying.ThumbnailPath)}");
                        await ctx.Channel.SendFileAsync(connection.NowPlaying.ThumbnailPath, embed: builder.Build());
                        return;
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync(string.Empty, embed: builder.Build());
                        return;
                    }
                }
                else
                {
                    builder.AddField("Nothing playing!", "Nothing's playing right now! Queue up some songs!");
                }

                await ctx.Channel.SendMessageAsync(string.Empty, embed: builder.Build());

            }
        }

        [IgnoreArguments]
        [Command("Queue", "Queues a song for me to play.", new[] { "queue", "play" })]
        public async Task Queue(CommandContext ctx)
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
                            var message = await ctx.Channel.SendMessageAsync("Downloading...");
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
                                    var message = await ctx.Channel.SendMessageAsync("Attempting to download as direct URL...");
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
                    IUserMessage message = await ctx.Channel.SendMessageAsync($"Downloading \"{Path.GetFileName(attachment.Filename)}\"...");

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

        [Command("Record", "Records the current goings on within a voice channel.", new[] { "rec", "record" })]
        public async Task Record(CommandContext ctx)
        {
            ConnectionModel connection;
            if ((connection = await GetConnectionModelAsync(ctx)) != null)
            {
                if (connection.Recording)
                {
                    var message = await ctx.Channel.SendMessageAsync("Processing your ~~clusterfuck~~ recording...");
                    connection.Recording = false;
                }
                else
                {
                    await ctx.ReplyAsync("Now recording...");
                    await connection.StartRecordingAsync();
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

                                if (reader.WaveFormat.Channels == 1)
                                {
                                    waveProvider = new MonoToStereoProvider16(waveProvider);
                                }

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
                $"-v {(native ? "" : "--write-info-json")} {(File.Exists(Path.Combine(Path.GetTempPath(), id + ".aac")) ? "--skip-download" : "")} -f m4a/aac/bestaudio/worst --hls-prefer-ffmpeg -4 --extract-audio --audio-format m4a --audio-quality 128K {uri} -o {Path.Combine(Path.GetTempPath(), id + ".%(ext)s")}\"")
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
                    if (_activeConnections.TryGetValue(guildUser.Guild.Id, out var connection))
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
