using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace WamWooWam.Core
{
    public class Settings
    {
        private static Dictionary<string, JToken> _rawSettings;
        private static Dictionary<string, object> _settingsCache = new Dictionary<string, object>();
        private static Dictionary<Type, JSchema> _schemaCache = new Dictionary<Type, JSchema>();

        private static Lazy<JSchemaGenerator> _schemaGenerator = new Lazy<JSchemaGenerator>(() => new JSchemaGenerator());
        private static Lazy<string> _settingsDirectoryLazy = new Lazy<string>(() =>
        {
            var assembly = Assembly.GetEntryAssembly();
            var name = assembly.GetName().Name;
            string dir;

#if NET35 || NET4 || NET45 || NET462
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name);
#else
            dir = Path.GetDirectoryName(assembly.Location);
#endif

            return dir;
        });

        public static bool AutoSave { get; set; } = true;

        public static string SettingsDirectory => _settingsDirectoryLazy.Value;

        public static T GetSetting<T>(string name, T def = default)
        {
            EnsureSettings();

            if (_settingsCache.TryGetValue(name, out var v))
            {
                if (v is T t)
                {
                    return t;
                }
                else
                {
                    return def;
                }
            }
            else if (_rawSettings.TryGetValue(name, out var j))
            {
                var schema = GetSchemaForType<T>();

                if (j.IsValid(schema))
                {
                    var obj = j.ToObject<T>();
                    _settingsCache[name] = obj;

                    return obj;
                }
                else
                {
                    return def;
                }
            }
            else
            {
                return def;
            }
        }

        public static T GetSetting<T>(string name)
        {
            EnsureSettings();

            if (_settingsCache.TryGetValue(name, out var v))
            {
                if (v is T t)
                {
                    return t;
                }
                else
                {
                    throw new InvalidCastException("Setting does not match type required.");
                }
            }
            else if (_rawSettings.TryGetValue(name, out var j))
            {
                var obj = j.ToObject<T>();
                _settingsCache[name] = obj;

                return obj;
            }
            else
            {
                throw new KeyNotFoundException("Unable to find setting!");
            }
        }

        public static bool TryGetSetting<T>(string name, out T setting, T def = default)
        {
            EnsureSettings();

            if (_settingsCache.TryGetValue(name, out var v))
            {
                if (v is T t)
                {
                    setting = t;
                    return true;
                }
                else
                {
                    setting = def;
                    return false;
                }
            }
            else if (_rawSettings.TryGetValue(name, out var j))
            {
                var schema = GetSchemaForType<T>();

                if (j.IsValid(schema))
                {
                    var obj = j.ToObject<T>();
                    _settingsCache[name] = obj;

                    setting = obj;
                    return true;
                }
                else
                {
                    setting = def;
                    return false;
                }
            }
            else
            {
                setting = def;
                return false;
            }
        }

        public static void SetSetting<T>(string name, T value)
        {
            EnsureSettings();

            _settingsCache[name] = value;
            _rawSettings[name] = value != null ? JToken.FromObject(value) : null;

            if (AutoSave)
            {
                SaveSettings();
            }
        }

        public static void SaveSettings()
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var filePath = Path.Combine(SettingsDirectory, "settings.json");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(_rawSettings));
        }

        private static void EnsureSettings()
        {
            if (_rawSettings == null)
            {
                var filePath = Path.Combine(SettingsDirectory, "settings.json");
                if (File.Exists(filePath))
                {
                    var str = File.ReadAllText(filePath);
                    _rawSettings = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(str);
                }
                else
                {
                    _rawSettings = new Dictionary<string, JToken>();
                    if (AutoSave)
                    {
                        SaveSettings();
                    }
                }
            }
        }

        private static JSchema GetSchemaForType<T>()
        {
            var type = typeof(T);

            JSchema schema;
            if (!_schemaCache.TryGetValue(type, out schema))
            {
                schema = _schemaGenerator.Value.Generate(type);
                _schemaCache[type] = schema;
            }

            return schema;
        }
    }
}
