using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RoyalMatch.Progress;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace RoyalMatch.Commerce
{
    [Serializable]
    internal sealed class ReceiptRequest
    {
        public string transactionId;
        public string productId;
        public string receipt;
        public string platform;
    }

    [Serializable]
    internal sealed class ReceiptDecision
    {
        public bool valid;
        public string transactionId;
        public string productId;
    }

    public sealed class IapStoreBridge : MonoBehaviour
    {
        public static IapStoreBridge Instance { get; private set; }
        public static readonly string[] ProductIds =
        {
            "milgemas.coins.small",
            "milgemas.coins.medium",
            "milgemas.coins.large",
            "milgemas.pass.monthly",
        };

        private StoreSettings _settings;
        private StoreController _controller;
        private readonly Dictionary<string, Product> _products = new Dictionary<string, Product>();
        public event Action Changed;
        public string Status { get; private set; } = "Compras en preparación";
        public bool IsReady => _controller != null && _products.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _settings = Resources.Load<StoreSettings>("StoreSettings");
        }

        private async void Start()
        {
            if (_settings == null || !_settings.EnableRealMoneyPurchases ||
                string.IsNullOrWhiteSpace(_settings.VerificationUrl) ||
                !_settings.VerificationUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Status = "Google Play y App Store pendientes de configurar";
                Changed?.Invoke();
                return;
            }
#if !UNITY_ANDROID && !UNITY_IOS
            Status = "Compras reales disponibles solo en Android/iOS";
            Changed?.Invoke();
            return;
#else
            try
            {
                _controller = UnityIAPServices.StoreController();
                _controller.OnProductsFetched += OnProductsFetched;
                _controller.OnPurchasePending += OnPurchasePending;
                _controller.OnPurchaseFailed += OnPurchaseFailed;
                _controller.OnStoreDisconnected += failure => { Status = "Tienda desconectada"; Changed?.Invoke(); };
                await _controller.Connect();
                var catalog = new CatalogProvider();
                foreach (string id in ProductIds)
                    catalog.AddProduct(id, ProductType.Consumable);
                catalog.FetchProducts(definitions => _controller.FetchProducts(definitions));
            }
            catch (Exception ex)
            {
                Status = "No se pudo conectar con la tienda";
                Debug.LogException(ex);
                Changed?.Invoke();
            }
#endif
        }

        private void OnProductsFetched(List<Product> products)
        {
            foreach (var product in products)
                if (product != null && !string.IsNullOrEmpty(product.definition?.id))
                    _products[product.definition.id] = product;
            Status = _products.Count == ProductIds.Length ? "Tienda lista" : "Productos incompletos en la tienda";
            Changed?.Invoke();
            _controller.FetchPurchases();
        }

        public string Price(string productId) => _products.TryGetValue(productId, out var product)
            ? product.metadata?.localizedPriceString ?? "" : "";

        public bool Buy(string productId)
        {
            if (!IsReady || !_products.TryGetValue(productId, out var product)) return false;
            if (productId == "milgemas.pass.monthly" && PlayerProgress.Data.PremiumPassOwned) return false;
            _controller.PurchaseProduct(product);
            return true;
        }

        private void OnPurchasePending(PendingOrder order)
        {
            StartCoroutine(VerifyAndFulfill(order));
        }

        private IEnumerator VerifyAndFulfill(PendingOrder order)
        {
            var items = order.CartOrdered.Items();
            if (items == null || items.Count != 1) yield break;
            string productId = items[0].Product.definition?.id;
            string transactionId = order.Info.TransactionID;
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(transactionId)) yield break;

            var body = new ReceiptRequest
            {
                transactionId = transactionId,
                productId = productId,
                receipt = order.Info.Receipt,
                platform = Application.platform.ToString(),
            };
            byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
            using var request = new UnityWebRequest(_settings.VerificationUrl, "POST")
            {
                uploadHandler = new UploadHandlerRaw(payload),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 20,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Status = "Compra pendiente de verificación";
                Changed?.Invoke();
                yield break;
            }
            ReceiptDecision decision;
            try { decision = JsonConvert.DeserializeObject<ReceiptDecision>(request.downloadHandler.text); }
            catch (Exception ex) { Debug.LogException(ex); yield break; }
            if (decision == null || !decision.valid || decision.transactionId != transactionId ||
                decision.productId != productId)
            {
                Status = "La compra no se ha verificado";
                Changed?.Invoke();
                yield break;
            }

            // The profile records transaction IDs before the store order is confirmed.
            // A retried PendingOrder cannot grant the same consumable twice.
            if (PlayerProgress.Data.ProcessedPurchases.Contains(transactionId) ||
                PlayerProgress.GrantVerifiedPurchase(transactionId, productId))
            {
                _controller.ConfirmPurchase(order);
                Status = "Compra entregada";
                Changed?.Invoke();
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            Status = "Compra cancelada o fallida";
            Changed?.Invoke();
        }

        public void Restore() => _controller?.FetchPurchases();
    }
}
