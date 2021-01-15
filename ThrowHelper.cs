using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonConfiguration
{
    internal static class ThrowHelper
    {
        public static TVal GetValueThrow<TKey,TVal>(this IDictionary<TKey, TVal> dict, TKey key)
        {
            if (!dict.ContainsKey(key))
                throw new KeyNotFoundException($"The key could not be found in the dictionary: {key}");

            return dict[key];
        }
    }
}
