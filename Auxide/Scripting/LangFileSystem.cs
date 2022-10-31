using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Auxide.Scripting
{
    public class LangFileSystem
    {
        /// <summary>
        /// Gets the directory that this system works in
        /// </summary>
        public string Directory { get; private set; }
        public string defaultLanguage = "en";
        private Dictionary<string, string> phrases = new Dictionary<string, string>();

        // All currently loaded langfiles
        private readonly Dictionary<string, DynamicConfigFile> _langfiles;

        public string Get(string input, string language = null)
        {
            if (language == null) language = defaultLanguage;

            return phrases.ContainsKey(input) ? phrases[input] : input;
        }

        /// <summary>
        /// Initializes a new instance of the langfileSystem class
        /// </summary>
        /// <param name="directory"></param>
        public LangFileSystem(string directory)
        {
            Directory = directory;
            _langfiles = new Dictionary<string, DynamicConfigFile>();
            KeyValuesConverter converter = new KeyValuesConverter();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);
        }

        public DynamicConfigFile GetFile(string name, string language = null)
        {
            if (language == null) language = defaultLanguage;
            name = DynamicConfigFile.SanitizeName(name);
            if (_langfiles.TryGetValue($"{name}.{language}", out DynamicConfigFile langfile))
            {
                return langfile;
            }

            langfile = new DynamicConfigFile(Path.Combine(Directory, $"{name}.{language}.json"));
            _langfiles.Add($"{name}.{language}", langfile);
            return langfile;
        }

        /// <summary>
        /// Check if langfile exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ExistsLangfile(string name, string language = null)
        {
            if (language == null) language = defaultLanguage;
            return GetFile($"{name}.{language}").Exists();
        }

        /// <summary>
        /// Gets a langfile
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DynamicConfigFile GetLangfile(string name, string language = null)
        {
            if (language == null) language = defaultLanguage;
            DynamicConfigFile langfile = GetFile($"{name}.{language}");

            // Does it exist?
            if (langfile.Exists())
            {
                // Load it
                langfile.Load();
            }
            else
            {
                // Just make a new one
                langfile.Save();
            }

            return langfile;
        }

        /// <summary>
        /// Gets data files from path, with optional search pattern
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public string[] GetFiles(string path = "", string searchPattern = "*")
        {
            return System.IO.Directory.GetFiles(Path.Combine(Directory, path), searchPattern);
        }

        /// <summary>
        /// Saves the specified langfile
        /// </summary>
        /// <param name="name"></param>
        public void SaveLangfile(string name, string language = null)
        {
            if (language == null) language = defaultLanguage;
            GetFile($"{name}.{language}").Save();
        }

        public T ReadObject<T>(string name, string language = null)
        {
            if (language == null) language = defaultLanguage;
            if (!ExistsLangfile($"{name}.{language}"))
            {
                T instance = Activator.CreateInstance<T>();
                return instance;
            }

            return GetFile($"{name}.{language}").ReadObject<T>();
        }

        /// <summary>
        /// Read data files in a batch and send callback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        public void ForEachObject<T>(string name, Action<T> callback)
        {
            string folder = DynamicConfigFile.SanitizeName(name);
            IEnumerable<DynamicConfigFile> files = _langfiles.Where(d => d.Key.StartsWith(folder)).Select(a => a.Value);
            foreach (DynamicConfigFile file in files)
            {
                callback?.Invoke(file.ReadObject<T>());
            }
        }
    }
}
