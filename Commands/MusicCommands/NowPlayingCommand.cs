using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using MusicCommands.Models;
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
                string songDetails = !string.IsNullOrWhiteSpace(song.Album) && !string.IsNullOrWhiteSpace(song.Artist) ? $"{song.Album} - {song.Artist}" :
                                !string.IsNullOrWhiteSpace(song.Artist) ? song.Artist :
                                !string.IsNullOrWhiteSpace(song.Album) ? song.Album : "";

                builder.AddField(song.Title, $"{(!string.IsNullOrWhiteSpace(songDetails) ? $"{songDetails}\r\n" : "")}Elapsed: {connection.Elapsed.ToString(@"mm\:ss")}/{connection.Total.ToString(@"mm\:ss")}");
                builder.AddField("Queued by", song.User.Mention);

                if(File.Exists(connection.NowPlaying.ThumbnailPath))
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

