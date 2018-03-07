using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Cli.Models
{
    public class StatusModel
    {
        public ActivityType Type { get; set; }
        public string Status { get; set; }
    }
}
