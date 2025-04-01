using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Extensions
{
    public static class DictionaryExtensions
    {
        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);

            return dictionary;
        }
        
        public static bool TryGetValueAs<TKey, TValue>(this IDictionary<TKey, object> dictionary, TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out var obj) && obj is TValue objValue)
            {
                value = objValue;
                return true;
            }
            
            value = default;
            return false;
        }
        
        public static TValue GetValueAsOrDefault<TKey, TValue>(this IDictionary<TKey, object> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary.TryGetValue(key, out var obj) && obj is TValue objValue)
                return objValue;
            
            return defaultValue;
        }
    }
}
