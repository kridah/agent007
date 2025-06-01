using System.Collections;
using System.Text.Json;

namespace Agent007.Tools
{
    /// <summary>
    /// JSON-style parameter parser for complex LLM tool arguments
    /// </summary>
    public class ParamParser : IReadOnlyDictionary<string, ItemParser>
    {
        private readonly object? _data;
        private readonly Lazy<Dictionary<string, ItemParser>> _items;

        public ParamParser(object? data)
        {
            _data = data;
            _items = new Lazy<Dictionary<string, ItemParser>>(BuildItemsDictionary);
        }

        private Dictionary<string, ItemParser> BuildItemsDictionary()
        {
            if (_data is IDictionary<string, object?> dict)
            {
                return dict.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ItemParser(kvp.Value)
                );
            }
            return new Dictionary<string, ItemParser>();
        }

        /// <summary>
        /// Get an item parser for the specified key
        /// </summary>
        public ItemParser? Get(string key)
        {
            if (_data is IDictionary<string, object?> dict && dict.TryGetValue(key, out var value))
                return new ItemParser(value);

            return null;
        }

        /// <summary>
        /// Get an item parser for the specified array index (when data is an array)
        /// </summary>
        public ItemParser? Get(int index)
        {
            if (_data is IList<object?> list && index >= 0 && index < list.Count)
                return new ItemParser(list[index]);

            return null;
        }

        /// <summary>
        /// Parse data as an array
        /// </summary>
        public ArrayParser AsArray()
        {
            return new ArrayParser(_data);
        }

        /// <summary>
        /// Check if the underlying data is an array
        /// </summary>
        public bool IsArray => _data is IList<object?> or object[];

        /// <summary>
        /// Check if the underlying data is an object
        /// </summary>
        public bool IsObject => _data is IDictionary<string, object?>;

        // IReadOnlyDictionary implementation
        public ItemParser this[string key] => Get(key) ?? new ItemParser(null);

        public IEnumerable<string> Keys => _items.Value.Keys;

        public IEnumerable<ItemParser> Values => _items.Value.Values;

        public int Count => _items.Value.Count;

        public bool ContainsKey(string key) => _items.Value.ContainsKey(key);

        public bool TryGetValue(string key, out ItemParser value)
        {
            return _items.Value.TryGetValue(key, out value!);
        }

        public IEnumerator<KeyValuePair<string, ItemParser>> GetEnumerator()
        {
            return _items.Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Array parser for list-like parameter structures
    /// </summary>
    public class ArrayParser : IReadOnlyList<ItemParser>
    {
        private readonly object? _data;
        private readonly Lazy<List<ItemParser>> _items;

        public ArrayParser(object? data)
        {
            _data = data;
            _items = new Lazy<List<ItemParser>>(BuildItemsList);
        }

        private List<ItemParser> BuildItemsList()
        {
            return _data switch
            {
                IList<object?> list => list.Select(item => new ItemParser(item)).ToList(),
                _ => new List<ItemParser>()
            };
        }

        public ItemParser this[int index] =>
            index >= 0 && index < Count ? _items.Value[index] : new ItemParser(null);

        public int Count => _items.Value.Count;

        public IEnumerator<ItemParser> GetEnumerator() => _items.Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Item parser for individual values
    /// </summary>
    public class ItemParser
    {
        private readonly object? _value;

        public ItemParser(object? value)
        {
            _value = value;
        }

        /// <summary>
        /// Parse as integer with optional default
        /// </summary>
        public int AsInt(int defaultValue = 0)
        {
            return _value switch
            {
                int intValue => intValue,
                long longValue => (int)Math.Clamp(longValue, int.MinValue, int.MaxValue),
                double doubleValue => (int)Math.Round(doubleValue),
                float floatValue => (int)Math.Round(floatValue),
                string strValue when int.TryParse(strValue, out var parsed) => parsed,
                bool boolValue => boolValue ? 1 : 0,
                System.Text.Json.JsonElement jsonElement => ParseJsonElementAsInt(jsonElement, defaultValue),
                _ => defaultValue
            };
        }

        /// <summary>
        /// Parse as double with optional default
        /// </summary>
        public double AsFloat(double defaultValue = 0.0)
        {
            return _value switch
            {
                double doubleValue => doubleValue,
                float floatValue => (double)floatValue,
                int intValue => (double)intValue,
                long longValue => (double)longValue,
                string strValue when double.TryParse(strValue, out var parsed) => parsed,
                bool boolValue => boolValue ? 1.0 : 0.0,
                System.Text.Json.JsonElement jsonElement => ParseJsonElementAsFloat(jsonElement, defaultValue),
                _ => defaultValue
            };
        }

        /// <summary>
        /// Parse as string with optional default
        /// </summary>
        public string AsString(string defaultValue = "")
        {
            return _value switch
            {
                string strValue => strValue,
                System.Text.Json.JsonElement jsonElement => ParseJsonElementAsString(jsonElement, defaultValue),
                null => defaultValue,
                _ => _value.ToString() ?? defaultValue
            };
        }

        /// <summary>
        /// Parse as boolean with optional default
        /// </summary>
        public bool AsBool(bool defaultValue = false)
        {
            return _value switch
            {
                bool boolValue => boolValue,
                int intValue => intValue != 0,
                long longValue => longValue != 0,
                double doubleValue => doubleValue != 0.0,
                string strValue => ParseBoolFromString(strValue, defaultValue),
                System.Text.Json.JsonElement jsonElement => ParseJsonElementAsBool(jsonElement, defaultValue),
                _ => defaultValue
            };
        }

        /// <summary>
        /// Parse as array of ItemParsers
        /// </summary>
        public ArrayParser AsArray()
        {
            return new ArrayParser(_value);
        }

        /// <summary>
        /// Parse as nested object (ParamParser)
        /// </summary>
        public ParamParser AsObject()
        {
            return new ParamParser(_value);
        }

        /// <summary>
        /// Check if value exists and is not null
        /// </summary>
        public bool HasValue => _value != null;

        /// <summary>
        /// Get raw value
        /// </summary>
        public object? RawValue => _value;

        private static bool ParseBoolFromString(string value, bool defaultValue)
        {
            return value.ToLowerInvariant() switch
            {
                "true" or "1" or "yes" or "on" or "enabled" => true,
                "false" or "0" or "no" or "off" or "disabled" => false,
                _ => defaultValue
            };
        }

        private static int ParseJsonElementAsInt(System.Text.Json.JsonElement element, int defaultValue)
        {
            return element.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal :
                                                         element.TryGetInt64(out var longVal) ? (int)Math.Clamp(longVal, int.MinValue, int.MaxValue) :
                                                         element.TryGetDouble(out var doubleVal) ? (int)Math.Round(doubleVal) : defaultValue,
                System.Text.Json.JsonValueKind.String => int.TryParse(element.GetString(), out var parsed) ? parsed : defaultValue,
                System.Text.Json.JsonValueKind.True => 1,
                System.Text.Json.JsonValueKind.False => 0,
                _ => defaultValue
            };
        }

        private static double ParseJsonElementAsFloat(System.Text.Json.JsonElement element, double defaultValue)
        {
            return element.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number => element.TryGetDouble(out var doubleVal) ? doubleVal : defaultValue,
                System.Text.Json.JsonValueKind.String => double.TryParse(element.GetString(), out var parsed) ? parsed : defaultValue,
                System.Text.Json.JsonValueKind.True => 1.0,
                System.Text.Json.JsonValueKind.False => 0.0,
                _ => defaultValue
            };
        }

        private static string ParseJsonElementAsString(System.Text.Json.JsonElement element, string defaultValue)
        {
            return element.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => element.GetString() ?? defaultValue,
                System.Text.Json.JsonValueKind.Number => element.ToString(),
                System.Text.Json.JsonValueKind.True => "true",
                System.Text.Json.JsonValueKind.False => "false",
                System.Text.Json.JsonValueKind.Null => defaultValue,
                _ => element.ToString()
            };
        }

        private static bool ParseJsonElementAsBool(System.Text.Json.JsonElement element, bool defaultValue)
        {
            return element.ValueKind switch
            {
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Number => element.TryGetDouble(out var num) && num != 0.0,
                System.Text.Json.JsonValueKind.String => ParseBoolFromString(element.GetString() ?? "", defaultValue),
                _ => defaultValue
            };
        }
    }
}
