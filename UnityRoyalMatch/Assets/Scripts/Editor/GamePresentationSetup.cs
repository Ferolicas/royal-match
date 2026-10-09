using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalMatch.Core;
using RoyalMatch.Rendering;

namespace RoyalMatch.EditorTools
{
    public static class GamePresentationSetup
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private static readonly Color Gold = new Color(1f, 0.76f, 0.24f);
        private static readonly Color Violet = new Color(0.17f, 0.07f, 0.31f);

        [MenuItem("Royal Match/Upgrade Game Presentation")]
        public static void UpgradeScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            ApplyToOpenScene();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Game presentation upgraded: HUD, palace backdrop, guardian reactions and result card.");
        }

        public static void ApplyToOpenScene()
        {
            var board = Object.FindFirstObjectByType<BoardView>();
            var controller = Object.FindFirstObjectByType<GameController>();
            var hud = Object.FindFirstObjectByType<HUD>(FindObjectsInactive.Include);
            var result = Object.FindFirstObjectByType<GameOverPanel>(FindObjectsInactive.Include);
            if (board == null || controller == null || hud == null || result == null)
                throw new System.InvalidOperationException("GameScene must contain its board, controller, HUD and result panel.");

            ConfigureBackdrop(board);
            ConfigureHud(hud, controller);
            ConfigureResult(result);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void ConfigureBackdrop(BoardView board)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/palace-menu-background.png");
            if (texture == null) throw new System.InvalidOperationException("Palace background is missing.");
            const string materialPath = "Assets/Materials/PalaceBackdrop.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.name = "PalaceBackdrop";
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(0.72f, 0.71f, 0.85f, 1f));
            EditorUtility.SetDirty(material);

            var go = GameObject.Find("AnimatedPalaceBackdrop");
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "AnimatedPalaceBackdrop";
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            // Unity's built-in Quad points along -Z; +90 degrees faces the top-down camera.
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.position = new Vector3(0f, -0.65f, 1.25f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            var motion = go.GetComponent<AnimatedPalaceBackdrop>();
            if (motion == null) motion = go.AddComponent<AnimatedPalaceBackdrop>();
            motion.BoardCamera = board.BoardCamera;

            var sparkle = GameObject.Find("PalaceAmbientGlints");
            if (sparkle == null)
            {
                sparkle = new GameObject("PalaceAmbientGlints");
                sparkle.transform.position = new Vector3(0f, -0.44f, -0.82f);
                var ps = sparkle.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.25f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.77f, 0.33f, 0.55f),
                    new Color(0.47f, 0.86f, 1f, 0.5f));
                main.maxParticles = 35;
                var emission = ps.emission;
                emission.rateOverTime = 5f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(22f, 0f, 11f);
                var color = ps.colorOverLifetime;
                color.enabled = true;
                var fade = new Gradient();
                fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f),
                            new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
                color.color = fade;
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/PremiumStar.mat");
            }
        }

        private static void ConfigureHud(HUD hud, GameController controller)
        {
            var root = hud.GetComponent<RectTransform>();
            var safe = GameObject.Find("SafeAreaContainer")?.GetComponent<RectTransform>();
            if (safe != null)
            {
                root.SetParent(safe, false);
                Stretch(root);
            }
            var bar = hud.transform.Find("TopBar")?.GetComponent<RectTransform>();
            if (bar != null)
            {
                hud.LevelLabel = bar.Find("LevelLabel")?.GetComponent<TextMeshProUGUI>();
                hud.MovesLabel = bar.Find("MovesLabel")?.GetComponent<TextMeshProUGUI>();
                hud.ScoreLabel = bar.Find("ScoreLabel")?.GetComponent<TextMeshProUGUI>();
                bar.anchoredPosition = new Vector2(0f, -10f);
                bar.sizeDelta = new Vector2(0f, 82f);
                var image = bar.GetComponent<Image>();
                image.color = new Color(0.10f, 0.035f, 0.21f, 0.93f);
                var outline = bar.GetComponent<Outline>() ?? bar.gameObject.AddComponent<Outline>();
                outline.effectColor = Gold;
                outline.effectDistance = new Vector2(0f, -3f);
                SetText(bar, "LevelLabel", "NIVEL", -285f);
                SetText(bar, "MovesLabel", "MOVIMIENTOS", -105f);
                SetText(bar, "ScoreLabel", "PUNTOS", 105f);
                SetValue(hud.LevelValue, -285f);
                SetValue(hud.MovesValue, -105f);
                SetValue(hud.ScoreValue, 105f);
            }

            var portraitRoot = hud.transform.Find("GuardianPortrait")?.GetComponent<RectTransform>();
            if (portraitRoot == null)
            {
                portraitRoot = new GameObject("GuardianPortrait", typeof(RectTransform), typeof(Image))
                    .GetComponent<RectTransform>();
                portraitRoot.SetParent(hud.transform, false);
            }
            portraitRoot.anchorMin = portraitRoot.anchorMax = new Vector2(0f, 1f);
            portraitRoot.pivot = new Vector2(0f, 1f);
            portraitRoot.anchoredPosition = new Vector2(15f, -12f);
            portraitRoot.sizeDelta = new Vector2(154f, 154f);
            var frame = portraitRoot.GetComponent<Image>();
            frame.color = new Color(0.15f, 0.055f, 0.29f, 0.97f);
            frame.raycastTarget = false;
            var border = portraitRoot.GetComponent<Outline>() ?? portraitRoot.gameObject.AddComponent<Outline>();
            border.effectColor = Gold;
            border.effectDistance = new Vector2(3f, -3f);

            var raw = portraitRoot.GetComponentInChildren<RawImage>();
            if (raw == null)
            {
                var portrait = new GameObject("Expressions", typeof(RectTransform), typeof(RawImage));
                portrait.transform.SetParent(portraitRoot, false);
                raw = portrait.GetComponent<RawImage>();
            }
            raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Characters/gem-guardian-expressions.png");
            raw.uvRect = new Rect(0f, 0.5f, 0.5f, 0.5f);
            raw.raycastTarget = false;
            raw.rectTransform.anchorMin = Vector2.zero;
            raw.rectTransform.anchorMax = Vector2.one;
            raw.rectTransform.offsetMin = Vector2.zero;
            raw.rectTransform.offsetMax = Vector2.zero;
            var reactions = portraitRoot.GetComponent<CharacterReactionView>() ??
                portraitRoot.gameObject.AddComponent<CharacterReactionView>();
            reactions.Portrait = raw;
            reactions.PortraitTransform = raw.rectTransform;
            controller.Character = reactions;
            var responsive = hud.GetComponent<HudResponsiveLayout>() ??
                hud.gameObject.AddComponent<HudResponsiveLayout>();
            responsive.TopBar = bar;
            responsive.Portrait = portraitRoot;
            responsive.Hud = hud;
        }

        private static void ConfigureResult(GameOverPanel result)
        {
            var root = result.GetComponent<RectTransform>();
            var safe = GameObject.Find("SafeAreaContainer")?.GetComponent<RectTransform>();
            if (safe != null)
            {
                root.SetParent(safe, false);
                Stretch(root);
            }
            root.GetComponent<Image>().color = new Color(0.045f, 0.015f, 0.10f, 0.88f);
            var card = result.transform.Find("VictoryCard")?.GetComponent<RectTransform>();
            if (card == null)
            {
                card = new GameObject("VictoryCard", typeof(RectTransform), typeof(Image))
                    .GetComponent<RectTransform>();
                card.SetParent(result.transform, false);
            }
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(535f, 505f);
            var image = card.GetComponent<Image>();
            image.color = new Color(0.17f, 0.065f, 0.34f, 0.98f);
            image.raycastTarget = false;
            var outline = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(5f, -5f);
            result.CardRoot = card;
            result.RatingStarSprite = EnsureRatingStar();

            var crest = card.Find("GuardianCrest")?.GetComponent<RawImage>();
            if (crest == null)
            {
                crest = new GameObject("GuardianCrest", typeof(RectTransform), typeof(RawImage))
                    .GetComponent<RawImage>();
                crest.transform.SetParent(card, false);
            }
            crest.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Characters/gem-guardian-expressions.png");
            crest.uvRect = new Rect(0.5f, 0f, 0.5f, 0.5f);
            crest.raycastTarget = false;
            Place(crest.rectTransform, new Vector2(0f, 154f), new Vector2(135f, 135f));
            result.GuardianCrest = crest;

            Move(result.TitleLabel.transform, card, new Vector2(0f, 67f), new Vector2(490f, 62f));
            result.TitleLabel.fontSize = 38f;
            result.TitleLabel.fontStyle = FontStyles.Bold;
            result.TitleLabel.color = Gold;
            Move(result.StarsContainer, card, new Vector2(0f, 0f), new Vector2(260f, 58f));
            var starLayout = result.StarsContainer.GetComponent<HorizontalLayoutGroup>() ??
                result.StarsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            starLayout.childAlignment = TextAnchor.MiddleCenter;
            starLayout.spacing = 16f;
            starLayout.childForceExpandWidth = false;
            starLayout.childForceExpandHeight = false;
            Move(result.RewardLabel.transform, card, new Vector2(0f, -60f), new Vector2(450f, 42f));
            result.RewardLabel.fontSize = 21f;
            result.RewardLabel.color = new Color(0.97f, 0.91f, 1f);

            StyleButton(result.NextLevelButton, card, "CONTINUAR", new Vector2(0f, -132f),
                new Vector2(360f, 70f), Gold, 28f);
            StyleButton(result.RetryButton, card, "REPETIR", new Vector2(-104f, -206f),
                new Vector2(195f, 51f), new Color(0.41f, 0.22f, 0.69f), 20f);
            StyleButton(result.MainMenuButton, card, "MENÚ", new Vector2(104f, -206f),
                new Vector2(195f, 51f), new Color(0.41f, 0.22f, 0.69f), 20f);
        }

        private static void StyleButton(Button button, RectTransform parent, string caption,
            Vector2 position, Vector2 size, Color color, float fontSize)
        {
            Move(button.transform, parent, position, size);
            button.GetComponent<Image>().color = color;
            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = color == Gold ? new Color(0.49f, 0.24f, 0.04f) : Gold;
            outline.effectDistance = new Vector2(0f, -4f);
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            text.text = caption;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.77f);
            colors.pressedColor = new Color(0.85f, 0.76f, 0.89f);
            button.colors = colors;
        }

        private static void SetText(RectTransform bar, string name, string text, float x)
        {
            var label = bar.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (label == null) return;
            label.text = text;
            label.rectTransform.anchoredPosition = new Vector2(x, 0f);
            label.rectTransform.anchoredPosition = new Vector2(x, 19f);
            label.rectTransform.sizeDelta = new Vector2(140f, 38f);
            label.fontSize = 19f;
            label.color = new Color(1f, 0.87f, 0.56f);
        }

        private static void SetValue(TextMeshProUGUI label, float x)
        {
            if (label == null) return;
            label.rectTransform.anchoredPosition = new Vector2(x, -15f);
            label.rectTransform.sizeDelta = new Vector2(145f, 40f);
            label.fontSize = 27f;
            label.fontStyle = FontStyles.Bold;
        }

        private static void Move(Transform item, RectTransform parent, Vector2 position, Vector2 size)
        {
            item.SetParent(parent, false);
            Place((RectTransform)item, position, size);
        }

        private static void Place(RectTransform item, Vector2 position, Vector2 size)
        {
            item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
            item.anchoredPosition = position;
            item.sizeDelta = size;
        }

        private static void Stretch(RectTransform item)
        {
            item.anchorMin = Vector2.zero;
            item.anchorMax = Vector2.one;
            item.offsetMin = Vector2.zero;
            item.offsetMax = Vector2.zero;
        }

        private static Sprite EnsureRatingStar()
        {
            const string path = "Assets/Art/UI/victory-star.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var vertices = new Vector2[10];
            for (int i = 0; i < vertices.Length; i++)
            {
                float angle = Mathf.PI * (i / 5f - 0.5f);
                float radius = (i & 1) == 0 ? 57f : 26f;
                vertices[i] = new Vector2(64f + Mathf.Cos(angle) * radius,
                    64f + Mathf.Sin(angle) * radius);
            }
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                    if (InsideStar(new Vector2(x + (sx + 0.5f) / 4f,
                        y + (sy + 0.5f) / 4f), vertices)) inside++;
                float highlight = Mathf.Clamp01((y - 25f) / 100f);
                pixels[y * size + x] = new Color(1f, 0.60f + highlight * 0.28f,
                    0.08f + highlight * 0.20f, inside / 16f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool InsideStar(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
