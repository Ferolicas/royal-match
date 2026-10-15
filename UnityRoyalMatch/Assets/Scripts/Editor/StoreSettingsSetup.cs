using RoyalMatch.Commerce;
using UnityEditor;
using UnityEngine;

namespace RoyalMatch.EditorTools
{
    public static class StoreSettingsSetup
    {
        [MenuItem("Mil Gemas/Crear configuración de compras")]
        public static void Create()
        {
            const string path = "Assets/Resources/StoreSettings.asset";
            var current = AssetDatabase.LoadAssetAtPath<StoreSettings>(path);
            if (current != null) return;
            var settings = ScriptableObject.CreateInstance<StoreSettings>();
            settings.EnableRealMoneyPurchases = false;
            settings.VerificationUrl = "";
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            Debug.Log("StoreSettings created with real-money purchases disabled until stores and verifier are configured.");
        }
    }
}
