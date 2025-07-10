using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;

namespace Softgraph.Stocks.Editor
{
    public static class StocksPreferences
    {
        private const string PrefKey = "Stocks_Symbols_Json";

        public static List<string> GetSymbols()
        {
            if (!EditorPrefs.HasKey(PrefKey))
                return new List<string> { "U" };

            var json = EditorPrefs.GetString(PrefKey);
            return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
        }

        public static void SetSymbols(List<string> symbols)
        {
            string json = JsonConvert.SerializeObject(symbols);
            EditorPrefs.SetString(PrefKey, json);
        }
    }
}
