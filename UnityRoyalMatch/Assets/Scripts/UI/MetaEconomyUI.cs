using System;
using System.Collections.Generic;
using RoyalMatch.Commerce;
using RoyalMatch.Core;
using RoyalMatch.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalMatch.UI
{
    public sealed class MetaEconomyUI : MonoBehaviour
    {
        private static readonly Color Violet = new Color(0.14f, 0.055f, 0.28f, 0.96f);
        private static readonly Color Dark = new Color(0.08f, 0.02f, 0.16f, 0.94f);
        private static readonly Color Gold = new Color(1f, 0.76f, 0.24f);
        private static readonly Color Light = new Color(0.95f, 0.91f, 1f);
        private GameSceneManager _manager;
        private GameController _controller;
        private IapStoreBridge _store;
        private GameObject _modal;
        private RectTransform _content;
        private TextMeshProUGUI _modalTitle;
        private TextMeshProUGUI _balance;
        private TextMeshProUGUI _feedback;
        private TextMeshProUGUI _menuBalance;
        private TextMeshProUGUI _hudBalance;
        private readonly List<TextMeshProUGUI> _boosterLabels = new List<TextMeshProUGUI>();
        private TMP_FontAsset _font;
        private string _tab;
        private float _nextRefresh;

        private void Start()
        {
            _manager = GetComponent<GameSceneManager>();
            _controller = FindFirstObjectByType<GameController>();
            _store = GetComponent<IapStoreBridge>();
            if (_manager?.MainMenuPanel == null) return;
            var menu = _manager.MainMenuPanel.GetComponent<MainMenuUI>();
            if (menu?.SettingsButton == null || menu.ActionsRoot == null) return;
            _font = menu.SettingsButton.GetComponentInChildren<TextMeshProUGUI>()?.font;

            CloneMenuButton(menu.SettingsButton, menu.ActionsRoot, "ShopButton", "TIENDA",
                new Vector2(-88, -103), OpenShop);
            CloneMenuButton(menu.SettingsButton, menu.ActionsRoot, "MissionsButton", "MISIONES",
                new Vector2(88, -103), OpenMissions);

            _menuBalance = Label(_manager.MainMenuPanel.transform, "EconomySummary", "", 18,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f),
                new Vector2(490f, 36f));
            BuildModal(_manager.MainMenuPanel.transform.parent);
            BuildHudBar(menu.SettingsButton);
            PlayerProgress.Changed += RefreshSummary;
            _manager.OnLevelStartDenied += ShowDenied;
            if (_store != null) _store.Changed += RefreshSummary;
            RefreshSummary();
        }

        private void OnDestroy()
        {
            PlayerProgress.Changed -= RefreshSummary;
            if (_manager != null) _manager.OnLevelStartDenied -= ShowDenied;
            if (_store != null) _store.Changed -= RefreshSummary;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 1f;
            RefreshSummary();
        }

        private void ShowDenied(string message)
        {
            OpenShop();
            if (_feedback != null) _feedback.text = message;
        }

        private void RefreshSummary()
        {
            var profile = PlayerProgress.Data;
            string life = PlayerProgress.IsUnlimitedLives ? "ILIM." :
                $"{PlayerProgress.Lives}/{PlayerProgress.MaxLives}";
            string summary = $"VIDAS {life}   MON. {profile.Coins}   EST. {profile.Stars}";
            if (_menuBalance != null) _menuBalance.text = summary;
            if (_hudBalance != null) _hudBalance.text = $"MON. {profile.Coins}";
            if (_balance != null) _balance.text = summary +
                (PlayerProgress.SecondsUntilNextLife > 0 ?
                    $"   •   próxima vida {PlayerProgress.SecondsUntilNextLife / 60:D2}:{PlayerProgress.SecondsUntilNextLife % 60:D2}" : "");
            string[] ids = { "hammer", "arrow", "cannon", "jester" };
            string[] names = { "MARTILLO", "FLECHA", "CAÑÓN", "SOMBRERO" };
            for (int i = 0; i < _boosterLabels.Count; i++)
                _boosterLabels[i].text = $"{names[i]} {PlayerProgress.GetBoosterCount(ids[i])}";
        }

        private void CloneMenuButton(Button source, Transform parent, string name, string title,
            Vector2 position, Action onClick)
        {
            var clone = Instantiate(source.gameObject, parent);
            clone.name = name;
            var rect = clone.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(160f, 52f);
            var button = clone.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
            var text = clone.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = title;
        }

        private void BuildHudBar(Button template)
        {
            if (_manager.HUDRoot == null) return;
            var bar = Rect(_manager.HUDRoot.transform, "EconomyBoosterBar",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(120f, 100f),
                new Vector2(220f, 150f));
            _hudBalance = Label(bar, "HudCoins", "", 17, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(190f, 35f));
            string[] ids = { "hammer", "arrow", "cannon", "jester" };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                var clone = Instantiate(template.gameObject, bar);
                clone.name = "Booster_" + id;
                var rect = clone.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(i % 2 == 0 ? -52f : 52f,
                    i < 2 ? 11f : -38f);
                rect.sizeDelta = new Vector2(101f, 43f);
                var button = clone.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    _controller ??= FindFirstObjectByType<GameController>();
                    if (_controller == null || !_controller.ChooseBooster(id))
                        Debug.Log("Potenciador no disponible o monedas insuficientes");
                    RefreshSummary();
                });
                var label = clone.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) { label.fontSize = 12; _boosterLabels.Add(label); }
            }
        }

        private void BuildModal(Transform parent)
        {
            _modal = Rect(parent, "EconomyModal", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var full = _modal.GetComponent<RectTransform>();
            full.offsetMin = Vector2.zero;
            full.offsetMax = Vector2.zero;
            _modal.AddComponent<Image>().color = new Color(0.025f, 0.008f, 0.06f, 0.86f);
            var card = Rect(full, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(610f, 670f));
            card.gameObject.AddComponent<Image>().color = Violet;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);
            _modalTitle = Label(card, "Title", "", 32, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 292f), new Vector2(430f, 52f));
            _modalTitle.color = Gold;
            _balance = Label(card, "Balance", "", 17, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 245f), new Vector2(540f, 40f));
            var close = Button(card, "Close", "CERRAR", new Vector2(0f, -292f), new Vector2(190f, 48f),
                () => _modal.SetActive(false));
            close.GetComponent<Image>().color = new Color(0.41f, 0.22f, 0.69f);
            _feedback = Label(card, "Feedback", "", 16, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(540f, 36f));

            var viewport = Rect(card, "ListViewport", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(550f, 425f));
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = Rect(viewport, "ListContent", new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0f, 425f));
            _content.pivot = new Vector2(0.5f, 1f);
            var layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            _modal.SetActive(false);
        }

        public void OpenShop()
        {
            _tab = "shop";
            _modal.SetActive(true);
            _modal.transform.SetAsLastSibling();
            Rebuild();
        }

        public void OpenMissions()
        {
            _tab = "missions";
            _modal.SetActive(true);
            _modal.transform.SetAsLastSibling();
            Rebuild();
        }

        private void Rebuild()
        {
            if (_content == null) return;
            foreach (Transform child in _content) Destroy(child.gameObject);
            RefreshSummary();
            _feedback.text = "";
            if (_tab == "shop")
            {
                _modalTitle.text = "TIENDA";
                Row("Recargar vidas", "900 MON.", PlayerProgress.Lives < PlayerProgress.MaxLives,
                    () => Act(PlayerProgress.TryRefillLives(), "Vidas recargadas"));
                SoftBooster("Martillo", "hammer", 300);
                SoftBooster("Flecha", "arrow", 400);
                SoftBooster("Cañón", "cannon", 400);
                SoftBooster("Sombrero", "jester", 500);
                StartingBooster("Cohete inicial", "rocket", 350);
                StartingBooster("TNT inicial", "tnt", 450);
                StartingBooster("Bola de luz inicial", "lightball", 600);
                Row("1.000 monedas", Price("milgemas.coins.small"), Ready("milgemas.coins.small"),
                    () => Buy("milgemas.coins.small"));
                Row("5.500 monedas", Price("milgemas.coins.medium"), Ready("milgemas.coins.medium"),
                    () => Buy("milgemas.coins.medium"));
                Row("12.000 monedas", Price("milgemas.coins.large"), Ready("milgemas.coins.large"),
                    () => Buy("milgemas.coins.large"));
                Row("Pase real del mes", Price("milgemas.pass.monthly"),
                    Ready("milgemas.pass.monthly") && !PlayerProgress.Data.PremiumPassOwned,
                    () => Buy("milgemas.pass.monthly"));
                Row("Restaurar compras", "REST.", _store?.IsReady ?? false, () => _store.Restore());
                _feedback.text = _store?.Status ?? "Tienda pendiente de configurar";
            }
            else
            {
                _modalTitle.text = "MISIONES Y PROGRESO";
                var profile = PlayerProgress.Data;
                Row($"Área {profile.AreaId} · tarea {profile.AreaTask + 1}/20", "USAR EST.",
                    profile.Stars >= 1 + (profile.AreaTask % 5 == 4 ? 1 : 0),
                    () => Act(PlayerProgress.ClaimAreaTask(), "Tarea completada"));
                Row($"Pase real · {Math.Min(30, profile.PassKeys / 5)}/30", profile.PremiumPassOwned ? "ACTIVO" : "GRATIS",
                    false, null);
                Row($"Racha del mayordomo · {profile.WinStreak}/3", "REGALO", false, null);
                Row($"Superbola de luz · {profile.SuperLightBallCharge}/10", "CARGA", false, null);
                foreach (var mission in PlayerProgress.WeeklyMissions)
                {
                    string id = mission.Id;
                    bool claimed = PlayerProgress.IsMissionClaimed(id);
                    int progress = Math.Min(mission.Target, PlayerProgress.GetMissionProgress(id));
                    Row($"{mission.Label} · {progress}/{mission.Target}", claimed ? "HECHO" : $"+{mission.RewardCoins} MON.",
                        !claimed && progress >= mission.Target,
                        () => Act(PlayerProgress.ClaimMission(id), "Recompensa cobrada"));
                }
            }
            Canvas.ForceUpdateCanvases();
            _content.anchoredPosition = Vector2.zero;
        }

        private void SoftBooster(string label, string id, int cost)
        {
            Row($"{label} · tienes {PlayerProgress.GetBoosterCount(id)}", $"{cost} MON.",
                PlayerProgress.Data.Coins >= cost,
                () => Act(PlayerProgress.TryBuyBooster(id, cost), "Potenciador añadido"));
        }

        private void StartingBooster(string label, string id, int cost)
        {
            SoftBooster(label, id, cost);
            Row($"{label} · preparados {PlayerProgress.GetBoosterCount(id)}",
                PlayerProgress.IsStartingBoosterEquipped(id) ? "QUITAR" : "EQUIPAR",
                PlayerProgress.IsStartingBoosterEquipped(id) || PlayerProgress.GetBoosterCount(id) > 0,
                () => Act(PlayerProgress.ToggleStartingBooster(id), "Selección actualizada"));
        }

        private string Price(string id) => _store?.Price(id) is string price && price.Length > 0 ? price : "PENDIENTE";
        private bool Ready(string id) => _store?.IsReady == true && !string.IsNullOrEmpty(_store.Price(id));

        private void Buy(string id)
        {
            if (_store == null || !_store.Buy(id)) _feedback.text = "Tienda aún no disponible";
        }

        private void Act(bool success, string message)
        {
            Rebuild();
            _feedback.text = success ? message : "No se pudo completar la operación";
        }

        private void Row(string label, string action, bool enabled, Action callback)
        {
            var root = Rect(_content, "Row", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(520f, 58f));
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            root.gameObject.AddComponent<Image>().color = Dark;
            var text = Label(root, "RowLabel", label, 17, new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(13f, 0f), new Vector2(335f, 52f), TextAlignmentOptions.Left);
            text.rectTransform.pivot = new Vector2(0f, 0.5f);
            var button = Button(root, "RowAction", action, new Vector2(207f, 0f), new Vector2(125f, 43f), callback);
            button.interactable = enabled;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private TextMeshProUGUI Label(Transform parent, string name, string text, int size,
            Vector2 min, Vector2 max, Vector2 position, Vector2 dimensions,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rect = Rect(parent, name, min, max, position, dimensions);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.fontSize = size;
            label.text = text;
            label.color = Light;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private Button Button(Transform parent, string name, string title, Vector2 position,
            Vector2 size, Action callback)
        {
            var rect = Rect(parent, name, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), position, size);
            rect.gameObject.AddComponent<Image>().color = Gold;
            var button = rect.gameObject.AddComponent<Button>();
            if (callback != null) button.onClick.AddListener(() => callback());
            var label = Label(rect, "Label", title, 15, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.color = Violet;
            return button;
        }
    }
}
