using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;

namespace WamBot.Api
{
    public static class CommandExtensions
    {
        private static string configDataPath => Path.Combine(Directory.GetCurrentDirectory(), "Data");
        private static Dictionary<string, Dictionary<string, object>> dataCache = new Dictionary<string, Dictionary<string, object>>();

        static CommandExtensions()
        {
            Timer timer = new Timer
            {
                Interval = 30_000,
                AutoReset = true
            };

            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var item in dataCache)
            {
                File.WriteAllText(Path.Combine(configDataPath, $"{item.Key}.json"), JsonConvert.SerializeObject(item.Value, Formatting.Indented));
            }
        }

        public static T GetData<T>(this DiscordCommand command, string name)
        {
            if (!Directory.Exists(configDataPath))
            {
                Directory.CreateDirectory(configDataPath);
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            string assemblyName = command.GetType().Assembly.GetName().Name;
            string filePath = Path.Combine(configDataPath, $"{assemblyName}.json");

            if (dataCache.TryGetValue(assemblyName, out data))
            {
                return GetFromDictionary<T>(name, data);
            }
            else
            {
                if (File.Exists(filePath))
                {
                    data = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(filePath));
                    dataCache[assemblyName] = data;
                    return GetFromDictionary<T>(name, data);
                }
                else
                {
                    data = new Dictionary<string, object>();
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(data));
                    return GetFromDictionary<T>(name, data);
                }
            }
        }

        public static void SetData<T>(this DiscordCommand command, string name, T data)
        {
            if (!Directory.Exists(configDataPath))
            {
                Directory.CreateDirectory(configDataPath);
            }

            Dictionary<string, object> dataStore = new Dictionary<string, object>();
            string assemblyName = command.GetType().Assembly.GetName().Name;
            string filePath = Path.Combine(configDataPath, $"{assemblyName}.json");

            if (dataCache.TryGetValue(assemblyName, out dataStore))
            {
                dataStore[name] = data;
                //dataCache[assemblyName] = dataStore;
            }
            else
            {
                if (File.Exists(filePath))
                {
                    dataStore = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(filePath));
                    dataStore[name] = data;
                    dataCache[assemblyName] = dataStore;
                }
                else
                {
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(dataStore, Formatting.Indented));
                    dataStore[name] = data;
                    dataCache[assemblyName] = dataStore;
                }
            }
        }

        private static T GetFromDictionary<T>(string name, Dictionary<string, object> data)
        {
            object result = null;
            if (data?.TryGetValue(name, out result) == true)
            {
                if (result is JToken j)
                {
                    return j.ToObject<T>();
                }
                else
                {
                    return (T)result;
                }
            }
            else
            {
                return default(T);
            }
        }
    }
}
