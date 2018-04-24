using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Tweetinvi.Models;

namespace WamBotRewrite.Data
{
    public class OldConfig
    {
        public OldConfig()
        {
            AdditionalPluginDirectories = new HashSet<string>();
            SeenGuilds = new HashSet<ulong>();
            AnnouncementChnanels = new Dictionary<ulong, ulong>();
            DisallowedGuilds = new HashSet<ulong>();

            TwitterCredentials = null;
            EmailCredentials = new NetworkCredential();

            StatusUpdateInterval = TimeSpan.FromMinutes(5);
            StatusMessages = new string[0];
            MemeLines = new string[0];
        }

        public string Prefix { get; set; } // Bot

        public string Token { get; set; } // Bot

        public string ConnectionString { get; set; } // Database

        public Guid ApplicationInsightsKey { get; set; } // Telemetry

        public TwitterCredentials TwitterCredentials { get; set; } // Twitter

        public NetworkCredential EmailCredentials { get; set; } // Email

        public string AbuseEmail { get; set; } // Email

        public HashSet<string> AdditionalPluginDirectories { get; set; } // Unused

        public HashSet<ulong> SeenGuilds { get; set; } // Move to database

        public HashSet<ulong> DisallowedGuilds { get; set; } // Move to database

        public string[] StatusMessages { get; set; } // Bot

        public string[] MemeLines { get; set; } // Bot

        public Dictionary<ulong, ulong> AnnouncementChnanels { get; set; } // Move to database

        public TimeSpan StatusUpdateInterval { get; set; } // Bot

    }

    internal class Config
    {
        public Config()
        {
            Bot = new Bot() { Prefix = "w;", MemeLines = new string[0], StatusMessages = new string[0], StatusUpdateInterval = TimeSpan.FromMinutes(15) };
            Database = new Database();
            Telemetry = new Telemetry() { Enabled = false };
            Twitter = new Twitter() { Enabled = false };
            Email = new Email() { Enabled = false };
            Reddit = new Reddit() { Enabled = false };
        }

        public Config(OldConfig old) : this()
        {
            // Migrate old bot settings 
            Bot.Token = old.Token;
            Bot.Prefix = old.Prefix;
            Bot.StatusMessages = old.StatusMessages;
            Bot.MemeLines = old.MemeLines;
            Bot.StatusUpdateInterval = old.StatusUpdateInterval;

            Database.ConnectionString = old.ConnectionString;

            if(old.ApplicationInsightsKey != Guid.Empty)
            {
                Telemetry.Enabled = true;
                Telemetry.ApplicationInsightsToken = old.ApplicationInsightsKey;
            }

            if(old.TwitterCredentials != null)
            {
                Twitter.Enabled = true;
                Twitter.ConsumerKey = old.TwitterCredentials.ConsumerKey;
                Twitter.ConsumerSecret = old.TwitterCredentials.ConsumerSecret;
                Twitter.AccessToken = old.TwitterCredentials.AccessToken;
                Twitter.AccessTokenSecret = old.TwitterCredentials.AccessTokenSecret;
                Twitter.MarkovTweets = old.Prefix == "w;";
                Twitter.TweetInterval = TimeSpan.FromHours(1);
            }

            if(old.EmailCredentials != null && old.AbuseEmail != null)
            {
                Email.Enabled = true;
                Email.Hostname = "localhost";
                Email.Port = 25;
                Email.Username = old.EmailCredentials.UserName;
                Email.Password = old.EmailCredentials.Password;
                Email.AbuseEmail = old.AbuseEmail;
            }

            Reddit.Enabled = true;
        }

        public Bot Bot { get; set; }

        public Database Database { get; set; }

        public Telemetry Telemetry { get; set; }

        public Twitter Twitter { get; set; }

        public Email Email { get; set; }

        public Reddit Reddit { get; set; }
    }

    internal class Bot
    {
        public string Prefix { get; set; }

        public string Token { get; set; }

        public string[] StatusMessages { get; set; }

        public string[] MemeLines { get; set; }

        public TimeSpan StatusUpdateInterval { get; set; }
    }

    internal class Database
    {
        public string ConnectionString { get; set; }
    }

    internal class Telemetry
    {
        public bool Enabled { get; set; }

        public Guid ApplicationInsightsToken { get; set; }
    }

    internal class Twitter : ITwitterCredentials
    {
        public bool Enabled { get; set; }

        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }

        public bool MarkovTweets { get; set; }
        public TimeSpan TweetInterval { get; set; }

        [JsonIgnore]
        public string ApplicationOnlyBearerToken { get; set; }

        public bool AreSetupForApplicationAuthentication()
        {
            return Enabled;
        }

        public bool AreSetupForUserAuthentication()
        {
            return Enabled;
        }

        public ITwitterCredentials Clone()
        {
            return new TwitterCredentials(this);
        }

        IConsumerCredentials IConsumerCredentials.Clone()
        {
            return new TwitterCredentials(this);
        }
    }

    internal class Email
    {
        public bool Enabled { get; set; }

        public string Hostname { get; set; }

        public ushort Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string AbuseEmail { get; set; }
    }

    internal class Reddit
    {
        public bool Enabled { get; set; }
        public string AccessToken { get; set; }
    }
}
