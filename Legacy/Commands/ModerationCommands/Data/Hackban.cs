using System;
using System.Collections.Generic;
using System.Text;

namespace ModerationCommands.Data
{
    class Hackban
    {
        public Hackban(ulong user, ulong issuer)
        {
            User = user;
            Issuer = issuer;
            Timestamp = DateTime.Now;
        }

        public ulong User { get; set; }
        public ulong Issuer { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
