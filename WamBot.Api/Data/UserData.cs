using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using DSharpPlus.Entities;

namespace WamBot.Api.Data
{
    public class UserData
    {
        private UserData() { }

        internal UserData(DiscordUser author)
        {
            Id = author.Id;
        }

        [Key]
        public ulong Id { get; internal set;}
    }
}
