using System;
using System.Collections.Generic;
using System.Text;

namespace WamBotRewrite.Data
{
    public class Config
    {
        public Config()
        {
            AdditionalPluginDirectories = new HashSet<string>();
            SeenGuilds = new HashSet<ulong>();
            AnnouncementChnanels = new Dictionary<ulong, ulong>();
            DisallowedGuilds = new HashSet<ulong>();
            
            StatusUpdateInterval = TimeSpan.FromMinutes(5);
        }

        public string Token { get; set; }

        public Guid ApplicationInsightsKey { get; set; }

        public string Prefix { get; set; }

        public HashSet<string> AdditionalPluginDirectories { get; set; }

        public HashSet<ulong> SeenGuilds { get; set; }

        public HashSet<ulong> DisallowedGuilds { get; set; }

        public Dictionary<ulong, ulong> AnnouncementChnanels { get; set; }

        public TimeSpan StatusUpdateInterval { get; set; }

    }
}
