using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using WamBotVoiceProcess.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace MusicCommands
{
    class NowPlayingCommand : MusicCommand
    {
        public override string Name => "Now Playing";

        public override string Description => "Info on what WamBot's currently playing.";

        public override string[] Aliases => new[] { "nowplaying", "musicinfo", "playing" };

        public override Func<int, bool> ArgumentCountPrecidate => x => true;

        public override Task<CommandResult> RunVoiceCommand(string[] args, CommandContext context, ConnectionModel connection)
        {
            DiscordEmbedBuilder builder = context.GetEmbedBuilder("Now Playing");
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
                    return Task.FromResult(new CommandResult(builder.Build(), connection.NowPlaying.ThumbnailPath));
                }
                else
                {
                    return Task.FromResult(new CommandResult(builder.Build()));
                }
            }
            else
            {
                builder.AddField("Nothing playing!", "Nothing's playing right now! Queue up some songs!");
            }

            return Task.FromResult((CommandResult)builder.Build());
        }
    }
}

