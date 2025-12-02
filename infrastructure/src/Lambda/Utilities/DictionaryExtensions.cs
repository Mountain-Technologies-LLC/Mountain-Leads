using System.Collections.Generic;

namespace Lambda.Utilities;

public static class DictionaryExtensions
{
    public static string? GetValueOrDefault(this IDictionary<string, string>? dictionary, string key)
    {
        if (dictionary == null)
            return null;
            
        return dictionary.TryGetValue(key, out var value) ? value : null;
    }
}
