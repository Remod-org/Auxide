using Newtonsoft.Json;
using System;
using System.IO;

namespace Auxide.Scripting
{
    public abstract class ConfigFile
    {
        [JsonIgnore]
        public string Filename { get; private set; }

        protected ConfigFile(string filename)
        {
            Filename = filename;
        }

        public static T Load<T>(string filename) where T : ConfigFile
        {
            T config = (T)Activator.CreateInstance(typeof(T), filename);
            config.Load();
            return config;
        }

        public virtual void Load(string filename = null)
        {
            string source = File.ReadAllText(filename ?? Filename);
            JsonConvert.PopulateObject(source, this);
        }

        public virtual void Save(string filename = null)
        {
            string source = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filename ?? Filename, source);
        }
    }
}
