using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Softgraph.Stocks.Editor
{
    public class StocksPreferencesProvider : SettingsProvider
    {
        private List<string> symbolList;
        private ReorderableList reorderableList;
    
        private const string ApiKeyPrefKey = "Stocks.ApiKey"; 
        private const string AutoUpdatePrefKey = "Stocks.AutoUpdate";
        private bool autoUpdateStockList;
        private bool lastAutoUpdateValue = false;
        private StocksPreferencesProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            symbolList = StocksPreferences.GetSymbols();
            autoUpdateStockList = EditorPrefs.GetBool(AutoUpdatePrefKey, false);
            lastAutoUpdateValue = autoUpdateStockList;
        
            reorderableList = new ReorderableList(symbolList, typeof(string), true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "Stock Symbols");
                },

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    // Add vertical padding
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;

                    // Add left padding so the drag handle works
                    Rect textRect = new Rect(rect.x + 10, rect.y, rect.width - 10, rect.height);
                
                    // Force uppercase live while typing
                    string input = EditorGUI.TextField(textRect, symbolList[index]).ToUpperInvariant();
                    symbolList[index] = input;
                },

                elementHeight = EditorGUIUtility.singleLineHeight + 4  // Add some padding
            };
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("API Settings", EditorStyles.boldLabel);

            string currentApiKey = EditorPrefs.GetString(ApiKeyPrefKey, string.Empty);
            GUIContent apiKeyLabel = new GUIContent("Finnhub API Key", "Enter your personal API key from finnhub.io");
            string newApiKey = EditorGUILayout.TextField(apiKeyLabel, currentApiKey);
            EditorGUILayout.Space();

            if (string.IsNullOrWhiteSpace(newApiKey))
            {
                EditorGUILayout.HelpBox("A valid Finnhub API key is required to fetch stock data. Visit https://finnhub.io to obtain the API Key.", MessageType.Warning);
            }
        
            if (newApiKey != currentApiKey)
            {
                EditorPrefs.SetString(ApiKeyPrefKey, newApiKey);
                Debug.Log("Updated API key.");
            }
        
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Stock Symbols", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();

            reorderableList.DoLayoutList();

            autoUpdateStockList = GUILayout.Toggle(autoUpdateStockList, "Auto-Update stock symbols");
        
            EditorGUILayout.Space();
            if (GUILayout.Button("Save"))
            {
                StocksPreferences.SetSymbols(symbolList);
                EditorPrefs.SetBool(AutoUpdatePrefKey, autoUpdateStockList);
            
                CloseSettingsWindow();
            
                Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(StocksButton.FetchStockPrice());
                Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(StocksDropdown.FetchStockPrice());
                Debug.Log("Saved preferences");
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new StocksPreferencesProvider("Preferences/Stocks");
        }

        private void CloseSettingsWindow()
        {
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var windowType = editorAssembly.GetType("UnityEditor.SettingsWindow");

            if (windowType != null)
            {
                var windows = Resources.FindObjectsOfTypeAll(windowType);
                foreach (var window in windows)
                {
                    (window as EditorWindow)?.Close();
                }
            }
        }
    }
}
