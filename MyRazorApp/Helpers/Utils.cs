using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MyRazorApp.Helpers
{
    public class Util
    {
        private static readonly Lazy<Util> _instance = new(() => new Util());

        public static Util Instance => _instance.Value;

        private Util() { }

        public string SerializeToJson<T>(IEnumerable<T> data)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(data, options);
        }
    }
}
