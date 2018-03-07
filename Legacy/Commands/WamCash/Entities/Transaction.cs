using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using WamWooWam.Core;

namespace WamCash.Entities
{
    public class Transaction
    {
        [JsonConstructor]
        public Transaction() { }

        public Transaction(ulong id, decimal amount, string reason = null)
        {
            Id = Strings.RandomString(32);
            FromId = id;
            Amount = amount;
            Reason = reason;
        }

        [Key]
        public string Id { get; set; }

        public ulong FromId { get; set; }

        public string Reason { get; set; }

        public decimal Amount { get; set; }

        public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;
    }
}