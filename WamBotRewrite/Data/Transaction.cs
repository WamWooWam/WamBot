using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WamWooWam.Core;

namespace WamBotRewrite.Data
{
    public class Transaction
    {
        public Transaction() {

        }

        public Transaction(User from, User to, decimal amount, string reason = null)
        {
            From = from;
            FromUserId = from.UserId;
            To = to;
            ToUserId = to.UserId;
            Amount = amount;
            Reason = reason;
            TimeStamp = DateTimeOffset.Now; 
        }

        [Key]
        public Guid TransactionId { get; set; }

        public long FromUserId { get; set; }
        public User From { get; set; }

        public long ToUserId { get; set; }        
        public User To { get; set; }

        public string Reason { get; set; }

        public decimal Amount { get; set; }

        public DateTimeOffset TimeStamp { get; set; } 
    }
}