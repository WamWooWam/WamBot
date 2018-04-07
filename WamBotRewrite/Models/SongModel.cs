using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Models
{
    class SongModel
    {
        public string Title { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public string Description { get; set; }

        public string FilePath { get; set; }

        public List<string> AdditionalTracks { get; set; } = new List<string>();

        public string ThumbnailPath { get; set; }

        public TimeSpan? Duration { get; set; }

        public IGuildUser User { get; set; }

        public string Source { get; set; }

        public override string ToString()
        {
            string albumArtist = !string.IsNullOrWhiteSpace(Album) && !string.IsNullOrWhiteSpace(Artist) ? $"{Album} - {Artist}" :
                                        !string.IsNullOrWhiteSpace(Artist) ? Artist :
                                        !string.IsNullOrWhiteSpace(Album) ? Album : null;
            string ret = $"{Title}{(!string.IsNullOrWhiteSpace(albumArtist) ? $" - {albumArtist}" : "")}";
            return !string.IsNullOrWhiteSpace(ret) ? ret : "Whoopsy! No metadata here.";
        }
    }
}
