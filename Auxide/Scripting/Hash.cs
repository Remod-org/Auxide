using System;
using System.Collections;
using System.Collections.Generic;

namespace Auxide.Scripting
{
    public class Hash<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dictionary;

        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return dictionary.IsReadOnly;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue tValue;
                if (TryGetValue(key, out tValue))
                {
                    return tValue;
                }
                if (!typeof(TValue).IsValueType)
                {
                    return default;
                }
                return (TValue)Activator.CreateInstance(typeof(TValue));
            }
            set
            {
                if (value == null)
                {
                    dictionary.Remove(key);
                    return;
                }
                dictionary[key] = value;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return dictionary.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return dictionary.Values;
            }
        }

        public Hash()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public Hash(IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dictionary.Add(item);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            dictionary.CopyTo(array, index);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Remove(item);
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }
    }
}
