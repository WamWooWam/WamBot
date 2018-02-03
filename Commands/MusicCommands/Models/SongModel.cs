using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCommands.Models
{
    class SongModel
    {
        public string Title { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public string FilePath { get; set; }

        public string ThumbnailPath { get; set; }

        public TimeSpan? Duration { get; set; }

        public DiscordUser User { get; set; }

        public override string ToString()
        {
            string albumArtist = !string.IsNullOrWhiteSpace(Album) && !string.IsNullOrWhiteSpace(Artist) ? $"{Album} - {Artist}" :
                                        !string.IsNullOrWhiteSpace(Artist) ? Artist :
                                        !string.IsNullOrWhiteSpace(Album) ? Album : null;

            return $"{Title}{(!string.IsNullOrWhiteSpace(albumArtist) ? $" - {albumArtist}" : "")}";
        }
    }
}
