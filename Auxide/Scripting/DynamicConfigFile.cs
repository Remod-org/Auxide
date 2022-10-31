using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Auxide.Scripting
{
    public class DynamicConfigFile : ConfigFile, IEnumerable<KeyValuePair<string, object>>
    {
        public JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings();
        private Dictionary<string, object> _keyvalues;
        private readonly JsonSerializerSettings _settings;
        private readonly string _chroot;

        public DynamicConfigFile(string filename) : base(filename)
        {
            _keyvalues = new Dictionary<string, object>();
            _settings = new JsonSerializerSettings();
            _settings.Converters.Add(new KeyValuesConverter());
            _chroot = Auxide.TopPath;
        }

        public override void Load(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            if (Exists(filename))
            {
                string source = File.ReadAllText(filename);
                _keyvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(source, _settings);
                return;
            }
            //Save();
        }

        public T ReadObject<T>(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            T customObject;
            if (Exists(filename))
            {
                string source = File.ReadAllText(filename);
                customObject = JsonConvert.DeserializeObject<T>(source, Settings);
            }
            else
            {
                customObject = Activator.CreateInstance<T>();
                WriteObject(customObject, false, filename);
            }
            return customObject;
        }

        public override void Save(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            string dir = Path.GetDirectoryName(filename);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filename, JsonConvert.SerializeObject(_keyvalues, Formatting.Indented, _settings));
        }

        public void WriteObject<T>(T config, bool sync = false, string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            string dir = Path.GetDirectoryName(filename);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonConvert.SerializeObject(config, Formatting.Indented, Settings);
            File.WriteAllText(filename, json);
            if (sync)
            {
                _keyvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, _settings);
            }
        }

        public bool Exists(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            string dir = Path.GetDirectoryName(filename);
            if (dir != null && !Directory.Exists(dir))
            {
                return false;
            }

            return File.Exists(filename);
        }

        private string CheckPath(string filename)
        {
            filename = SanitizeName(filename);
            string path = Path.GetFullPath(filename);
            if (!path.StartsWith(_chroot, StringComparison.Ordinal))
            {
                throw new Exception($"Access to top directory only!\nPath: {path}");
            }

            return path;
        }

        public static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            name = name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            name = Regex.Replace(name, "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]", "_");
            name = Regex.Replace(name, @"\.+", ".");
            return name.TrimStart('.');
        }

        public void Clear()
        {
            _keyvalues.Clear();
        }

        public void Remove(string key)
        {
            _keyvalues.Remove(key);
        }

        public object this[string key]
        {
            get
            {
                return _keyvalues.TryGetValue(key, out object val) ? val : null;
            }
            set
            {
                _keyvalues[key] = value;
            }
        }

        public object this[string keyLevel1, string keyLevel2]
        {
            get { return Get(keyLevel1, keyLevel2); }
            set { Set(keyLevel1, keyLevel2, value); }
        }

        public object this[string keyLevel1, string keyLevel2, string keyLevel3]
        {
            get { return Get(keyLevel1, keyLevel2, keyLevel3); }
            set { Set(keyLevel1, keyLevel2, keyLevel3, value); }
        }

        public object ConvertValue(object value, Type destinationType)
        {
            if (!destinationType.IsGenericType)
            {
                return Convert.ChangeType(value, destinationType);
            }

            if (destinationType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type valueType = destinationType.GetGenericArguments()[0];
                IList list = (IList)Activator.CreateInstance(destinationType);
                foreach (object val in (IList)value)
                {
                    list.Add(Convert.ChangeType(val, valueType));
                }

                return list;
            }
            if (destinationType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = destinationType.GetGenericArguments()[0];
                Type valueType = destinationType.GetGenericArguments()[1];
                IDictionary dict = (IDictionary)Activator.CreateInstance(destinationType);
                foreach (object key in ((IDictionary)value).Keys)
                {
                    dict.Add(Convert.ChangeType(key, keyType), Convert.ChangeType(((IDictionary)value)[key], valueType));
                }

                return dict;
            }
            throw new InvalidCastException("Generic types other than List<> and Dictionary<,> are not supported");
        }

        public T ConvertValue<T>(object value) => (T)ConvertValue(value, typeof(T));

        public object Get(params string[] path)
        {
            if (path.Length < 1)
            {
                throw new ArgumentException("path must not be empty");
            }

            if (!_keyvalues.TryGetValue(path[0], out object val))
            {
                return null;
            }

            for (int i = 1; i < path.Length; i++)
            {
                Dictionary<string, object> dict = val as Dictionary<string, object>;
                if (dict == null || !dict.TryGetValue(path[i], out val))
                {
                    return null;
                }
            }
            return val;
        }

        public T Get<T>(params string[] path) => ConvertValue<T>(Get(path));

        public void Set(params object[] pathAndTrailingValue)
        {
            if (pathAndTrailingValue.Length < 2)
            {
                throw new ArgumentException("path must not be empty");
            }

            string[] path = new string[pathAndTrailingValue.Length - 1];
            for (int i = 0; i < pathAndTrailingValue.Length - 1; i++)
            {
                path[i] = (string)pathAndTrailingValue[i];
            }

            object value = pathAndTrailingValue[pathAndTrailingValue.Length - 1];
            if (path.Length == 1)
            {
                _keyvalues[path[0]] = value;
                return;
            }
            if (!_keyvalues.TryGetValue(path[0], out object val))
            {
                _keyvalues[path[0]] = val = new Dictionary<string, object>();
            }

            for (int i = 1; i < path.Length - 1; i++)
            {
                if (!(val is Dictionary<string, object>))
                {
                    throw new ArgumentException("path is not a dictionary");
                }

                Dictionary<string, object> oldVal = (Dictionary<string, object>)val;
                if (!oldVal.TryGetValue(path[i], out val))
                {
                    oldVal[path[i]] = val = new Dictionary<string, object>();
                }
            }
            ((Dictionary<string, object>)val)[path[path.Length - 1]] = value;
        }

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _keyvalues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _keyvalues.GetEnumerator();

        #endregion IEnumerable
    }

    public class KeyValuesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, object>) || objectType == typeof(List<object>);
        }

        private void Throw(string message)
        {
            throw new Exception(message);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Dictionary<string, object>))
            {
                // Get the dictionary to populate
                Dictionary<string, object> dict = existingValue as Dictionary<string, object> ?? new Dictionary<string, object>();
                if (reader.TokenType == JsonToken.StartArray)
                {
                    return dict;
                }
                // Read until end of object
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    // Read property name
                    if (reader.TokenType != JsonToken.PropertyName)
                    {
                        Throw("Unexpected token: " + reader.TokenType);
                    }

                    string propname = reader.Value as string;
                    if (!reader.Read())
                    {
                        Throw("Unexpected end of json");
                    }

                    // What type of object are we reading?
                    switch (reader.TokenType)
                    {
                        case JsonToken.String:
                        case JsonToken.Float:
                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Date:
                        case JsonToken.Null:
                            dict[propname] = reader.Value;
                            break;

                        case JsonToken.Integer:
                            string value = reader.Value.ToString();
                            int result;
                            if (int.TryParse(value, out result))
                            {
                                dict[propname] = result;
                            }
                            else
                            {
                                dict[propname] = value;
                            }

                            break;

                        case JsonToken.StartObject:
                            dict[propname] = serializer.Deserialize<Dictionary<string, object>>(reader);
                            break;

                        case JsonToken.StartArray:
                            dict[propname] = serializer.Deserialize<List<object>>(reader);
                            break;

                        default:
                            Throw("Unexpected token: " + reader.TokenType);
                            break;
                    }
                }

                // Return it
                return dict;
            }
            if (objectType == typeof(List<object>))
            {
                // Get the list to populate
                List<object> list = existingValue as List<object> ?? new List<object>();

                // Read until end of array
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    // What type of object are we reading?
                    switch (reader.TokenType)
                    {
                        case JsonToken.String:
                        case JsonToken.Float:
                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Date:
                        case JsonToken.Null:
                            list.Add(reader.Value);
                            break;

                        case JsonToken.Integer:
                            string value = reader.Value.ToString();
                            int result;
                            if (int.TryParse(value, out result))
                            {
                                list.Add(result);
                            }
                            else
                            {
                                list.Add(value);
                            }

                            break;

                        case JsonToken.StartObject:
                            list.Add(serializer.Deserialize<Dictionary<string, object>>(reader));
                            break;

                        case JsonToken.StartArray:
                            list.Add(serializer.Deserialize<List<object>>(reader));
                            break;

                        default:
                            Throw("Unexpected token: " + reader.TokenType);
                            break;
                    }
                }

                // Return it
                return list;
            }
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Get the dictionary to write
            if (value is Dictionary<string, object> dict)
            {

                // Start object
                writer.WriteStartObject();

                // Simply loop through and serialize
                foreach (KeyValuePair<string, object> pair in dict.OrderBy(i => i.Key))
                {
                    writer.WritePropertyName(pair.Key, true);
                    serializer.Serialize(writer, pair.Value);
                }

                // End object
                writer.WriteEndObject();
            }
            else if (value is List<object> list) // Get the list to write
            {

                // Start array
                writer.WriteStartArray();

                // Simply loop through and serialize
                foreach (object t in list)
                {
                    serializer.Serialize(writer, t);
                }

                // End array
                writer.WriteEndArray();
            }
        }
    }
}
