using WamBotVoiceProcess.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;
using NAudio;
using NAudio.Wave;
using System.IO;

namespace MusicCommands
{
    class DownloadCommand : MusicCommand
    {
        public override string Name => "Download";

        public override string Description => "Downloads the current song as an MP3 file.";

        public override string[] Aliases => new[] { "mp3", "download" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override async Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            using (MediaFoundationReader reader = new MediaFoundationReader(connection.NowPlaying.FilePath))
            {
                string file = Path.Combine(Path.GetTempPath(), $"{connection.NowPlaying}.mp3");
                if (File.Exists(file))
                {
                    await context.Channel.SendFileAsync(file);
                }
                else
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, file);
                    using(TagLib.File tag = TagLib.File.Create(file))
                    {
                        tag.Tag.Title = connection.NowPlaying.Title;
                        tag.Tag.Album = connection.NowPlaying.Album;
                        tag.Tag.AlbumArtists = new string[] { connection.NowPlaying.Artist };
                        tag.Tag.Pictures = new TagLib.IPicture[] { new TagLib.Picture(connection.NowPlaying.ThumbnailPath) };
                        tag.Save();
                    }

                    await context.Channel.SendFileAsync(file);
                }
            }

            return CommandResult.Empty;
        }
    }
}
