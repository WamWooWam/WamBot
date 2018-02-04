using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WamBot.Api.Data
{
    public class UserData
    {
        [Key]
        public ulong Id { get; set; }
    }
}
