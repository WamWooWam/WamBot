using System;
using System.Collections.Generic;
using System.Text;

namespace ModerationCommands.Data
{
    class GuildData
    {
        public GuildData()
        {
            Hackbans = new List<Hackban>();
        }

        public List<Hackban> Hackbans { get; set; }       
    }
}
