using UnityEngine;

namespace RoyalMatch.Commerce
{
    [CreateAssetMenu(menuName = "Mil Gemas/Store Settings", fileName = "StoreSettings")]
    public sealed class StoreSettings : ScriptableObject
    {
        [Tooltip("Enable only after registering all product IDs in Google Play and App Store Connect.")]
        public bool EnableRealMoneyPurchases;

        [Tooltip("HTTPS service that verifies the store receipt and returns a signed/authorized grant decision.")]
        public string VerificationUrl = "";
    }
}
