using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WamCash.Entities
{
    public class Account
    {
        [JsonConstructor]
        public Account() { }

        public Account(DiscordUser user)
        {
            Id = user.Id;
            Balance = 0;
            TransactionHistory = new List<Transaction>();
            Name = user.Username;
        }

        [Key]
        public ulong Id { get; set; }

        public string Name { get; set; }

        public decimal Balance { get; set; }

        public List<Transaction> TransactionHistory { get; set; }
    }
}
