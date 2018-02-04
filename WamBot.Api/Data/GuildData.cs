using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WamBot.Api.Data
{
    public class GuildData
    {
        [Key]
        public ulong Id { get; set; }

        public List<string> Variables { get; set; }
    }
}
