using System.Collections;
using Newtonsoft.Json.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Networking;

namespace Softgraph.Stocks.Editor
{
    [InitializeOnLoad]
    public class StocksDropdown : Stocks
    {
        private const string kElementPath = "Stocks Dropdown";
        
        static StocksDropdown()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(AutoFetchLoop());
        }

        [MainToolbarElement(kElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement StockPriceButton()
        {
            string tooltip = "Stock Price";
            MainToolbarContent content = new MainToolbarContent(stockPrice, tooltip);
            return new MainToolbarDropdown(content, ShowDropdownMenu);
        }

        static void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add new stocks..."), false, OpenSettingsWindow);
            menu.AddItem(new GUIContent("Display next stock"), false, OnClickButton);
        
            menu.DropDown(dropDownRect);
        }
    
        private static void OpenSettingsWindow()
        {
            SettingsService.OpenUserPreferences("Preferences/Stocks");
        }

        private static void OnClickButton()
        {
            if (currentRequest != null && !currentRequest.isDone)
                return;

            currentSymbolIndex = (currentSymbolIndex + 1) % Symbols.Count;
            lastUpdateTime = 0f;

            EditorCoroutineUtility.StartCoroutineOwnerless(FetchStockPrice());
        }

        private static IEnumerator AutoFetchLoop()
        {
            while (true)
            {
                bool autoUpdate = EditorPrefs.GetBool("Stocks.AutoUpdate", false);

                if (autoUpdate && Symbols.Count > 0)
                    currentSymbolIndex = (currentSymbolIndex + 1) % Symbols.Count;
            
                yield return FetchStockPrice();
                yield return new EditorWaitForSeconds(updateInterval);
            }
        }

        public static IEnumerator FetchStockPrice()
        {
            if (EditorApplication.timeSinceStartup - lastUpdateTime < updateInterval && currentRequest != null)
                yield break;

            string apiKey = EditorPrefs.GetString(ApiKeyPrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("Finnhub API key is not set. Please enter it in Preferences > Stocks.");
                stockPrice = "API key missing. Set it in Preferences.";
                MainToolbar.Refresh(kElementPath); // Ensure the toolbar updates
                yield break;
            }
        
            string queryUrl = $"{ApiUrl}?symbol={CurrentSymbol}&token={apiKey}";
            currentRequest = UnityWebRequest.Get(queryUrl);

            yield return currentRequest.SendWebRequest();

            if (currentRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = currentRequest.downloadHandler.text;
                try
                {
                    JObject data = JObject.Parse(jsonResponse);

                    if (float.TryParse(data["c"]?.ToString(), out float currentPrice) &&
                        float.TryParse(data["pc"]?.ToString(), out float previousClosePrice) &&
                        previousClosePrice > 0f)
                    {
                        float changePercent = ((currentPrice - previousClosePrice) / previousClosePrice) * 100f;
                        string color = changePercent >= 0 ? "#65C466" : "#EB4E3D";
                        string changeStr = $"<color={color}>({changePercent:+0.00;-0.00}%)</color>";
                        stockPrice = $"{CurrentSymbol}: ${currentPrice:F2} {changeStr}";
                    }
                    else
                    {
                        stockPrice = $"{CurrentSymbol}: Invalid data.";
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"JSON Parse Error: {ex.Message}");
                    stockPrice = $"{CurrentSymbol}: Error parsing data.";
                }
            }
            else
            {
                Debug.LogError($"WebRequest Error: {currentRequest.error}");
                stockPrice = $"{CurrentSymbol}: Request failed.";
            }

            lastUpdateTime = (float)EditorApplication.timeSinceStartup;
            currentRequest = null;

            MainToolbar.Refresh(kElementPath); // Force toolbar refresh
        }
    }
}
