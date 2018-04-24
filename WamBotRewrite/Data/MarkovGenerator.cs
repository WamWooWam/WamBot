using MarkovChains;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WamBotRewrite.Data
{
    class MarkovGenerator : IStringGenerator
    {
        internal static Dictionary<long, Markov<string>> Markovs { get; private set; } = new Dictionary<long, Markov<string>>();
        internal static Dictionary<long, List<string>> MarkovList { get; private set; } = new Dictionary<long, List<string>>();

        public MarkovGenerator()
        {
            string path = Path.Combine(Program.BaseDir, "markov.json");
            if (File.Exists(path))
            {
                MarkovList = JsonConvert.DeserializeObject<Dictionary<long, List<string>>>(File.ReadAllText(path));
                //File.Delete("markov.json");

                foreach (var pair in MarkovList)
                {
                    Markov<string> markov = new Markov<string>(".");
                    markov.Train(pair.Value, 2);
                    Markovs[pair.Key] = markov;
                }
            }
        }

        public string Generate(int length)
        {
            return "";
        }

        public string Generate(User user, int length)
        {
            if (Markovs.TryGetValue(user.UserId, out var markov))
            {
                return markov.Generate(length, " ", true);
            }
            else
            {
                return "";
            }
        }

        public void Train(User user, string[] strings)
        {
            if (Markovs.TryGetValue(user.UserId, out var m))
            {
                m.Train(strings.ToList(), 2);
                MarkovList[user.UserId].AddRange(strings);
            }
            else
            {
                Markov<string> markov = new Markov<string>(".");
                markov.Train(strings.ToList(), 2);

                var markovList = new List<string>();
                markovList.AddRange(strings);

                MarkovList[user.UserId] = markovList;
                Markovs[user.UserId] = markov;
            }
        }

        public void Reset(User user)
        {
            if (Markovs.TryGetValue(user.UserId, out var m))
            {
                MarkovList[user.UserId].Clear();
                Markovs[user.UserId] = new Markov<string>(".");
            }
        }
    }
}
