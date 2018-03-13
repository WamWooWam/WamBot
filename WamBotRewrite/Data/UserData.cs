using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Discord;

namespace WamBotRewrite.Data
{
    public class User
    {
        public User()
        {
            TransactionsSent = new List<Transaction>();
            TransactionsRecieved = new List<Transaction>();
        }

        internal User(IUser author) : this()
        {
            UserId = (long)author.Id;
        }

        [Key]
        public long UserId { get; set; }

        public sbyte Happiness { get; set; }

        public long CommandsRun { get; set; }

        public decimal Balance { get; set; }

        [InverseProperty("From")]
        public List<Transaction> TransactionsSent { get; set; }

        [InverseProperty("To")]
        public List<Transaction> TransactionsRecieved { get; set; }

        [NotMapped]
        public ICollection<Transaction> Transactions => TransactionsSent.Concat(TransactionsRecieved).OrderBy(t => t.TimeStamp).ToList();
    }
}
