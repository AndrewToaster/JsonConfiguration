using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JsonConfiguration
{
    /// <summary>
    /// Class used for configuration using JSON
    /// </summary>
    public class JsonConfig
    {
        /// <summary>
        /// The version of the implementation
        /// </summary>
        public const string VERSION = "1.0";

        /// <summary>
        /// The options used for serialization and deserialization
        /// </summary>
        private static readonly JsonSerializerOptions _options;
        /// <summary>
        /// The path to the config file
        /// </summary>
        private readonly string _jsonPath;

        /// <summary>
        /// Indicates whether or not to call <see cref="Save"/> after certain functions
        /// </summary>
        public bool AutoFlush { get; set; }

        /// <summary>
        /// The config dictionary loaded from JSON
        /// </summary>
        public Dictionary<string, string> Entries { get; }

        /// <summary>
        /// Sets or Gets a entry with the specified key
        /// </summary>
        /// <param name="key">The key to use</param>
        /// <returns>The value of this entry</returns>
        public string this[string key] { get => GetEntry(key); set => SetEntry(key, value); }

        /// <summary>
        /// Initilizes static fields
        /// </summary>
        static JsonConfig()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                MaxDepth = 8,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="JsonConfig"/>
        /// </summary>
        private JsonConfig(JSON_Root jsonConfig, string configFile, bool autoFlush)
        {
            _jsonPath = configFile;
            Entries = jsonConfig;
            AutoFlush = autoFlush;
        }

        /// <summary>
        /// Returns a entry insde the config
        /// </summary>
        /// <param name="key">The key of the entry to use</param>
        /// <returns>Value associated with the <paramref name="key"/>, <see langword="null"/><returns>
        public string GetEntry(string key)
        {
            return Entries.GetValueOrDefault(key);
        }

        /// <summary>
        /// Returns a entry insde the config
        /// </summary>
        /// <param name="key">The key of the entry to use</param>
        /// <returns>Value associated with the <paramref name="key"/>, <see langword="null"/> if it doesn't exist<returns>
        public TEntry GetEntry<TEntry>(string key)
        {
            return JsonSerializer.Deserialize<TEntry>(Entries.GetValueOrDefault(key));
        }

        /// <summary>
        /// Returns wheter or not entry <paramref name="key"/> is present in the config
        /// </summary>
        /// <param name="key">The key of the entry to check</param>
        /// <returns><see langword="true"/> if <paramref name="key"/> is present, otherwise <see langword="false"/></returns>
        public bool ContainsEntry(string key)
        {
            return Entries.ContainsKey(key);
        }

        /// <summary>
        /// Sets a entry inside the config
        /// </summary>
        /// <param name="key">The key of the entry to use</param>
        /// <param name="value">The value to set the entry to</param>
        public void SetEntry(string key, string value)
        {
            Entries[key] = value;
            TryAutoFlush();
        }

        /// <summary>
        /// Sets a entry inside the config
        /// </summary>
        /// <param name="key">The key of the entry to use</param>
        /// <param name="value">The value to set the entry to</param>
        public void SetEntry<TEntry>(string key, TEntry value)
        {
            Entries[key] = JsonSerializer.Serialize(value);
            TryAutoFlush();
        }

        /// <summary>
        /// Retrieves an entry and modifies it with <paramref name="modFunc"/>, then sets the new value to the <paramref name="key"/>
        /// </summary>
        /// <typeparam name="TEntry">The type to modify</typeparam>
        /// <param name="key">The key of the entry to modify</param>
        /// <param name="modFunc">The action that modifies the value</param>
        public void ModifyEntry<TEntry>(string key, Action<TEntry> modFunc)
        {
            TEntry entry = JsonSerializer.Deserialize<TEntry>(Entries.GetValueThrow(key));
            modFunc(entry);
            Entries[key] = JsonSerializer.Serialize(entry);

            TryAutoFlush();
        }

        /// <summary>
        /// Removes a entry inside the config
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The key of the entry to use</returns>
        public bool RemoveEntry(string key)
        {
            bool removed = Entries.Remove(key);

            if (removed)
                TryAutoFlush();

            return removed;
        }

        /// <summary>
        /// Writes the JSON config to the file
        /// </summary>
        public void Save()
        {
            using FileStream fileStream = File.OpenWrite(_jsonPath);
            byte[] data = JsonSerializer.SerializeToUtf8Bytes<JSON_Root>(Entries, _options);
            fileStream.Write(data, 0, data.Length);
        }
        /// <summary>
        /// Writes the JSON config to the file
        /// </summary>
        public async Task SaveAsync()
        {
            using FileStream fileStream = File.OpenWrite(_jsonPath);
            await JsonSerializer.SerializeAsync<JSON_Root>(fileStream, Entries, _options);
        }

        /// <summary>
        /// Calls <see cref="Save"/> if <see cref="AutoFlush"/> is <see langword="true"/>, otherwise does nothing
        /// </summary>
        private void TryAutoFlush()
        {
            if (AutoFlush)
                Save();
        }

        /// <summary>
        /// Creates a new instance of <see cref="JsonConfig"/> using a specified. If not found, creates a new config
        /// </summary>
        /// <param name="configPath">The path to the config</param>
        /// <param name="allowVersionMismatch">Whether or not to allow different config versions</param>
        /// <returns>Created wrapper</returns>
        public static JsonConfig LoadFromFile(string configPath, bool autoFlush = false, bool allowVersionMismatch = false)
        {
            if (File.Exists(configPath))
            {
                JSON_Root jsonConfig = JsonSerializer.Deserialize<JSON_Root>(File.ReadAllText(configPath), _options);

                if (jsonConfig.ConfigVersion != VERSION && !allowVersionMismatch)
                    throw new VersionMismatchException(VERSION, jsonConfig.ConfigVersion);

                return new JsonConfig(jsonConfig, configPath, autoFlush);
            }
            else
            {
                FileInfo fi = new FileInfo(configPath);
                Directory.CreateDirectory(fi.DirectoryName); 
                
                using (FileStream fileStream = fi.Create())
                {
                    JSON_Root jsonConfig = JSON_Root.Default;
                    byte[] data = JsonSerializer.SerializeToUtf8Bytes(jsonConfig, _options);

                    fileStream.Write(data, 0, data.Length);

                    return new JsonConfig(jsonConfig, configPath, autoFlush);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="JsonConfig"/> using a specified. If not found, creates a new config
        /// </summary>
        /// <param name="configPath">The path to the config</param>
        /// <param name="allowVersionMismatch">Whether or not to allow different config versions</param>
        /// <returns>Created wrapper</returns>
        public static async Task<JsonConfig> LoadFromFileAsync(string configPath, bool autoFlush = false, bool allowVersionMismatch = false)
        {
            if (File.Exists(configPath))
            {
                using FileStream fileStream = File.OpenRead(configPath);
                JSON_Root jsonConfig = await JsonSerializer.DeserializeAsync<JSON_Root>(fileStream, _options);

                if (jsonConfig.ConfigVersion != VERSION && !allowVersionMismatch)
                    throw new VersionMismatchException(VERSION, jsonConfig.ConfigVersion);

                return new JsonConfig(jsonConfig, configPath, autoFlush);
            }
            else
            {
                FileInfo fi = new FileInfo(configPath);
                Directory.CreateDirectory(fi.DirectoryName);

                using (FileStream fileStream = fi.Create())
                {
                    JSON_Root jsonConfig = JSON_Root.Default;
                    await JsonSerializer.SerializeAsync(fileStream, jsonConfig, _options);

                    return new JsonConfig(jsonConfig, configPath, autoFlush);
                }
            }
        }

        /// <summary>
        /// The root JSON object of the config
        /// </summary>
        private class JSON_Root
        {
            public static JSON_Root Default { get => new JSON_Root() { ConfigVersion = VERSION, Entries = new JSON_Entry[0]}; }

            public string ConfigVersion { get; set; }
            public JSON_Entry[] Entries { get; set; }

            public static implicit operator Dictionary<string, string>(JSON_Root config)
            {
                return new Dictionary<string, string>(config.Entries.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));
            }
            public static implicit operator JSON_Root(Dictionary<string, string> dict)
            {
                return new JSON_Root()
                {
                    ConfigVersion = VERSION,
                    Entries = dict.Select(x => new JSON_Entry() { Key = x.Key, Value = x.Value }).ToArray()
                };
            }
        }

        /// <summary>
        /// The entry in the <see cref="JSON_Root"/>
        /// </summary>
        private class JSON_Entry
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }

    /// <summary>
    /// Exception for when versions do not match
    /// </summary>
    public class VersionMismatchException : Exception
    {
        /// <summary>
        /// Creates a new instance <see cref="VersionMismatchException"/>
        /// </summary>
        /// <param name="expectedVersion">The expected version</param>
        /// <param name="version">The mismatched version</param>
        public VersionMismatchException(string expectedVersion, string version) : 
            base($"The version '{version}' does not match the expected version '{expectedVersion}'")
        {
        }
    }
}
