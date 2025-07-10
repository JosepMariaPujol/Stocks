using System.Collections.Generic;
using UnityEngine.Networking;

namespace Softgraph.Stocks.Editor
{
    public class Stocks
    {
        protected const string ApiKeyPrefKey = "Stocks.ApiKey"; 
    
        protected static string stockPrice = "Waiting...";
        protected const string ApiUrl = "https://finnhub.io/api/v1/quote";
    
        protected static List<string> Symbols => StocksPreferences.GetSymbols();
        protected static UnityWebRequest currentRequest = null;

        protected static int currentSymbolIndex = 0;
        protected static float lastUpdateTime = 0f;
        protected static readonly float updateInterval = 20f;

        protected static string CurrentSymbol =>
            (Symbols != null && Symbols.Count > 0) ? Symbols[currentSymbolIndex % Symbols.Count] : "U";
    }
}
