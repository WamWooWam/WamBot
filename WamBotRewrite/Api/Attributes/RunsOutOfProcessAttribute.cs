using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotRewrite.Api
{
    class RunOutOfProcessAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating wether this command needs a Discord
        /// WebSocket connection while running out of process
        /// </summary>
        public bool RequiresDiscord { get; set; } = false;

        public TimeSpan Timeout { get; set; }
    }
}
