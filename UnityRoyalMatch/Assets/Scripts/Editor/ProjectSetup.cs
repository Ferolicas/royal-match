// ProjectSetup.cs — Editor script to create a complete, fully-wired Unity scene

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using RoyalMatch.Rendering;

namespace RoyalMatch.EditorTools
{
    public static class ProjectSetup
    {
        private static readonly string ScenesPath = "Assets/Scenes";
        private static readonly string PrefabsPath = "Assets/Prefabs";
        private static readonly string MaterialsPath = "Assets/Materials";
        private static readonly string MeshesPath = "Assets/Meshes";

        [MenuItem("Royal Match/Full Project Setup")]
        public static void SetupProject()
        {
            CreateDirectories();
            ConfigureGemTextures();
            CreateMaterials();
            CreateGemMeshes();
            CreatePrefabs();
            ConfigureAudioAssets();
            CreateURPAsset();
            CreateTMPSettings();
            CreateGameScene();
            CreateBootstrapScene();
            ConfigureBuildSettings();
            AssetDatabase.Refresh();
            Debug.Log("Full project setup complete!");
        }

        private static void CreateTMPSettings()
        {
            // TMP Essential Resources are imported to "Assets/TextMesh Pro/Resources"
            // Check if they're already present (shaders, font assets, TMP Settings)
            string tmpPackageSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            string resourcesSettingsPath = "Assets/Resources/TMP Settings.asset";
            string liberationFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

            // Check at package location first
            var existingSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(tmpPackageSettingsPath);
            if (existingSettings != null)
            {
                var checkSo = new SerializedObject(existingSettings);
                var fontProp = checkSo.FindProperty("m_defaultFontAsset");
                if (fontProp != null && fontProp.objectReferenceValue != null)
                {
                    Debug.Log($"TMP Settings found with font asset: {fontProp.objectReferenceValue.name}");
                    return;
                }
            }

            // Check at Resources location (our manual creation)
            existingSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(resourcesSettingsPath);
            if (existingSettings != null)
            {
                var checkSo = new SerializedObject(existingSettings);
                var fontProp = checkSo.FindProperty("m_defaultFontAsset");
                if (fontProp != null && fontProp.objectReferenceValue != null)
                {
                    Debug.Log($"TMP Settings found with font asset: {fontProp.objectReferenceValue.name}");
                    return;
                }
            }

            // Check if LiberationSans SDF font asset exists from package
            if (File.Exists(liberationFontAssetPath))
            {
                Debug.Log("TMP Essential Resources found (LiberationSans SDF font asset present). Shaders should be available.");
                return;
            }

            // No TMP resources found
            Debug.LogWarning("TMP Essential Resources not found. Text may not render properly.");
        }

        private static void CreateURPAsset()
        {
            string assetPath = Path.Combine("Assets/RenderPipeline", "RoyalMatchURP.asset");
            string rendererPath = Path.Combine("Assets/RenderPipeline", "RoyalMatchRenderer.asset");

            // A renderer reference alone is not enough. The previous setup added a
            // bare UniversalRendererData sub-asset whose required shader resources
            // were null, so overlay UI rendered while every 3D object disappeared.
            var existingPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            var existingRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (existingPipeline != null && existingRenderer != null)
            {
                var rendererSO = new SerializedObject(existingRenderer);
                var postProcessData = rendererSO.FindProperty("postProcessData");
                if (postProcessData?.objectReferenceValue != null &&
                    existingPipeline.rendererDataList.Length > 0 &&
                    existingPipeline.rendererDataList[0] == existingRenderer)
                {
                    GraphicsSettings.defaultRenderPipeline = existingPipeline;
                    Debug.Log("Render pipeline already assigned with populated renderer resources.");
                    return;
                }
            }

            // Build the renderer from Unity's populated package template. Creating a
            // UniversalRendererData via ScriptableObject does not populate its hidden
            // shader/resource groups in a player build.
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.DeleteAsset(rendererPath);

            const string rendererTemplate =
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRendererData.asset";
            if (!AssetDatabase.CopyAsset(rendererTemplate, rendererPath))
                throw new IOException($"Could not create renderer from {rendererTemplate}");

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
                throw new MissingReferenceException("Royal Match renderer data was not created.");
            rendererData.name = "RoyalMatchRenderer";

            var newURP = UniversalRenderPipelineAsset.Create(rendererData);
            newURP.name = "RoyalMatchURP";
            AssetDatabase.CreateAsset(newURP, assetPath);

            newURP.shadowDistance = 100f;
            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(newURP);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GraphicsSettings.defaultRenderPipeline = newURP;

            Debug.Log("URP asset created with populated renderer resources and assigned.");
        }

        private static void CreateDirectories()
        {
            string[] dirs = {
                ScenesPath, PrefabsPath, MaterialsPath,
                MeshesPath,
                Path.Combine(PrefabsPath, "UI"),
                Path.Combine(PrefabsPath, "VFX"),
                "Assets/StreamingAssets/Levels",
                "Assets/RenderPipeline",
            };
            foreach (var d in dirs)
                Directory.CreateDirectory(d);
        }

        #region Materials

        private static void ConfigureGemTextures()
        {
            foreach (string texturePath in PremiumGemTexturePaths())
            {
                if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
                    continue;

                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void CreateMaterials()
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            // Gem material
            var gemMat = new Material(litShader);
            gemMat.name = "GemMaterial";
            gemMat.SetFloat("_Smoothness", 0.9f);
            gemMat.SetFloat("_Metallic", 0.2f);
            AssetDatabase.CreateAsset(gemMat, Path.Combine(MaterialsPath, "GemMaterial.mat"));

            // Separate green crystal material for the custom red chili's stem.
            // Keeping it as a child renderer preserves the two-color reference
            // without baking a texture or multiplying the normal gem materials.
            var chiliStemMat = new Material(litShader);
            chiliStemMat.name = "ChiliStemMaterial";
            chiliStemMat.SetColor("_BaseColor", new Color(0.25f, 0.82f, 0.08f, 1f));
            chiliStemMat.SetColor("_EmissionColor", new Color(0.035f, 0.12f, 0.01f, 1f));
            chiliStemMat.SetFloat("_Smoothness", 0.88f);
            chiliStemMat.SetFloat("_Metallic", 0.16f);
            AssetDatabase.CreateAsset(chiliStemMat, Path.Combine(MaterialsPath, "ChiliStemMaterial.mat"));

            string[] premiumMaterialNames =
            {
                "PremiumRedGemMaterial", "PremiumBlueGemMaterial", "PremiumGreenGemMaterial",
                "PremiumYellowGemMaterial", "PremiumPurpleGemMaterial", "PremiumCyanGemMaterial",
            };
            string[] premiumTexturePaths = PremiumGemTexturePaths();
            for (int i = 0; i < premiumMaterialNames.Length; i++)
                CreatePremiumGemMaterial(premiumMaterialNames[i], premiumTexturePaths[i]);

            // Obstacle material
            var obsMat = new Material(litShader);
            obsMat.name = "ObstacleMaterial";
            obsMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f, 1f));
            obsMat.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(obsMat, Path.Combine(MaterialsPath, "ObstacleMaterial.mat"));

            // Board material
            var boardMat = new Material(litShader);
            boardMat.name = "BoardMaterial";
            boardMat.SetColor("_BaseColor", new Color(0.12f, 0.08f, 0.2f, 1f));
            boardMat.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(boardMat, Path.Combine(MaterialsPath, "BoardMaterial.mat"));

            // Soft circular particle texture/material. A particle renderer without
            // an authored material falls back to an opaque quad in player builds.
            string particleTexturePath = Path.Combine(MaterialsPath, "VFXSoftCircle.asset");
            string particleMaterialPath = Path.Combine(MaterialsPath, "VFXParticleMaterial.mat");
            AssetDatabase.DeleteAsset(particleTexturePath);
            AssetDatabase.DeleteAsset(particleMaterialPath);
            const int textureSize = 32;
            var particleTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, true)
            {
                name = "VFXSoftCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float nx = (x + 0.5f) / textureSize * 2f - 1f;
                    float ny = (y + 0.5f) / textureSize * 2f - 1f;
                    float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny)));
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }
            particleTexture.SetPixels(pixels);
            particleTexture.Apply(false, true);
            AssetDatabase.CreateAsset(particleTexture, particleTexturePath);

            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader != null)
            {
                var particleMaterial = new Material(particleShader) { name = "VFXParticleMaterial" };
                if (particleMaterial.HasProperty("_BaseMap")) particleMaterial.SetTexture("_BaseMap", particleTexture);
                if (particleMaterial.HasProperty("_MainTex")) particleMaterial.SetTexture("_MainTex", particleTexture);
                if (particleMaterial.HasProperty("_Surface")) particleMaterial.SetFloat("_Surface", 1f);
                if (particleMaterial.HasProperty("_Blend")) particleMaterial.SetFloat("_Blend", 0f);
                if (particleMaterial.HasProperty("_SrcBlend")) particleMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (particleMaterial.HasProperty("_DstBlend")) particleMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (particleMaterial.HasProperty("_ZWrite")) particleMaterial.SetFloat("_ZWrite", 0f);
                particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                particleMaterial.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(particleMaterial, particleMaterialPath);
            }

            AssetDatabase.SaveAssets();
        }

        #endregion

        private static Sprite CreateTempSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        private static void CreateGemPrefab()
        {
            string prefabPath = Path.Combine(PrefabsPath, "Gem.prefab");
            AssetDatabase.DeleteAsset(prefabPath);

            var go = new GameObject("Gem");
            var meshFilter = go.AddComponent<MeshFilter>();
            var colorMeshes = new[]
            {
                AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "GemChili.asset")),
                AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "GemDiamond.asset")),
                AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "GemEmerald.asset")),
                AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "GemHex.asset")),
                AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "GemPear.asset")),
                AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "GemOval.asset")),
            };
            meshFilter.sharedMesh = colorMeshes[0];

            var meshRenderer = go.AddComponent<MeshRenderer>();
            var gemMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MaterialsPath, "GemMaterial.mat"));
            if (gemMat != null) meshRenderer.sharedMaterial = gemMat;

            go.AddComponent<SphereCollider>().isTrigger = true;

            var gemView = go.AddComponent<RoyalMatch.Rendering.GemView>();
            gemView.MeshRenderer = meshRenderer;
            gemView.MeshFilter = meshFilter;
            gemView.ColorMeshes = colorMeshes;

            var accent = new GameObject("ChiliStem", typeof(MeshFilter), typeof(MeshRenderer));
            accent.transform.SetParent(go.transform, false);
            accent.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            accent.GetComponent<MeshFilter>().sharedMesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "GemChiliStem.asset"));
            accent.GetComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MaterialsPath, "ChiliStemMaterial.mat"));
            gemView.AccentRenderer = accent.GetComponent<MeshRenderer>();

            string[] premiumNames = { "Red", "Blue", "Green", "Yellow", "Purple", "Cyan" };
            string[] premiumMaterialNames =
            {
                "PremiumRedGemMaterial", "PremiumBlueGemMaterial", "PremiumGreenGemMaterial",
                "PremiumYellowGemMaterial", "PremiumPurpleGemMaterial", "PremiumCyanGemMaterial",
            };
            var premiumRenderers = new MeshRenderer[premiumNames.Length];
            var premiumMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(MeshesPath, "PremiumGemQuad.asset"));
            for (int i = 0; i < premiumNames.Length; i++)
            {
                var premium = new GameObject($"Premium{premiumNames[i]}Visual", typeof(MeshFilter), typeof(MeshRenderer));
                premium.transform.SetParent(go.transform, false);
                premium.transform.localPosition = new Vector3(0f, 0.36f, 0f);
                premium.GetComponent<MeshFilter>().sharedMesh = premiumMesh;
                premium.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    Path.Combine(MaterialsPath, premiumMaterialNames[i] + ".mat"));
                premium.GetComponent<MeshRenderer>().enabled = false;
                premiumRenderers[i] = premium.GetComponent<MeshRenderer>();
            }
            gemView.PremiumRenderers = premiumRenderers;
            SpecialPresentationSetup.Apply(go);

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("Gem prefab created.");
        }

        private static void CreateObstaclePrefab()
        {
            string prefabPath = Path.Combine(PrefabsPath, "Obstacle.prefab");
            AssetDatabase.DeleteAsset(prefabPath);

            var go = new GameObject("Obstacle");
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetOrCreatePrimitiveMesh(PrimitiveType.Cube, "BoardCube");

            var meshRenderer = go.AddComponent<MeshRenderer>();
            var obsMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MaterialsPath, "ObstacleMaterial.mat"));
            if (obsMat != null) meshRenderer.sharedMaterial = obsMat;

            go.AddComponent<BoxCollider>();
            var obsView = go.AddComponent<RoyalMatch.Rendering.ObstacleView>();
            obsView.MeshRenderer = meshRenderer;
            ObstaclePresentationSetup.Apply(go);

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("Obstacle prefab created.");
        }

        private static void CreateUIPrefabs()
        {
            string uiDir = Path.Combine(PrefabsPath, "UI");

            // Star Icon
            string starPath = Path.Combine(uiDir, "StarIcon.prefab");
            AssetDatabase.DeleteAsset(starPath);
            var starGo = new GameObject("StarIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            var starImg = starGo.GetComponent<Image>();
            starImg.sprite = CreateTempSprite();
            starImg.color = new Color(1f, 0.84f, 0f);
            var starRt = starGo.GetComponent<RectTransform>();
            starRt.sizeDelta = new Vector2(22, 22);
            starRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var starLayout = starGo.GetComponent<LayoutElement>();
            starLayout.preferredWidth = 22f;
            starLayout.preferredHeight = 22f;
            PrefabUtility.SaveAsPrefabAsset(starGo, starPath);
            Object.DestroyImmediate(starGo);

            // Level Button
            string lbPath = Path.Combine(uiDir, "LevelButton.prefab");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(lbPath) == null)
            {
                var go = new GameObject("LevelButton");
                var img = go.AddComponent<Image>();
                img.sprite = CreateTempSprite();
                img.color = new Color(0.15f, 0.3f, 0.6f, 0.9f);
                var btn = go.AddComponent<Button>();

                var numLabel = new GameObject("NumberLabel").AddComponent<TextMeshProUGUI>();
                numLabel.transform.SetParent(go.transform);
                numLabel.alignment = TextAlignmentOptions.Center;
                numLabel.fontSize = 24;

                var starsLabel = new GameObject("StarsLabel").AddComponent<TextMeshProUGUI>();
                starsLabel.transform.SetParent(go.transform);
                starsLabel.alignment = TextAlignmentOptions.TopRight;
                starsLabel.fontSize = 14;

                var levelSelectBtn = go.AddComponent<RoyalMatch.UI.LevelSelectButton>();
                levelSelectBtn.Button = btn;
                levelSelectBtn.NumberLabel = numLabel;
                levelSelectBtn.StarsLabel = starsLabel;

                PrefabUtility.SaveAsPrefabAsset(go, lbPath);
                Object.DestroyImmediate(go);
            }

            // Objective Item. Rebuild it on every setup so UI layout fixes are
            // reliably propagated to generated scenes and standalone players.
            string objPath = Path.Combine(uiDir, "ObjectiveItem.prefab");
            AssetDatabase.DeleteAsset(objPath);
            var objectiveGo = new GameObject("ObjectiveItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            var objectiveRt = objectiveGo.GetComponent<RectTransform>();
            objectiveRt.sizeDelta = new Vector2(230f, 64f);
            var objectiveRootLayout = objectiveGo.GetComponent<LayoutElement>();
            objectiveRootLayout.preferredWidth = 230f;
            objectiveRootLayout.preferredHeight = 64f;
            var objectiveImage = objectiveGo.GetComponent<Image>();
            objectiveImage.sprite = CreateTempSprite();
            objectiveImage.color = new Color(0.12f, 0.04f, 0.24f, 0.84f);

            var iconLabel = new GameObject("Icon", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            iconLabel.transform.SetParent(objectiveGo.transform, false);
            iconLabel.fontSize = 25f;
            iconLabel.alignment = TextAlignmentOptions.Center;
            var iconRt = iconLabel.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(10f, 0f);
            iconRt.sizeDelta = new Vector2(34f, 46f);

            var descLabel = new GameObject("Description", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            descLabel.transform.SetParent(objectiveGo.transform, false);
            descLabel.fontSize = 14f;
            descLabel.enableWordWrapping = true;
            descLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var descRt = descLabel.rectTransform;
            descRt.anchorMin = descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(50f, 0f);
            descRt.sizeDelta = new Vector2(112f, 48f);

            var progressLabel = new GameObject("Progress", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            progressLabel.transform.SetParent(objectiveGo.transform, false);
            progressLabel.fontSize = 16f;
            progressLabel.fontStyle = FontStyles.Bold;
            progressLabel.alignment = TextAlignmentOptions.Center;
            var progressRt = progressLabel.rectTransform;
            progressRt.anchorMin = progressRt.anchorMax = new Vector2(0f, 0.5f);
            progressRt.pivot = new Vector2(0f, 0.5f);
            progressRt.anchoredPosition = new Vector2(168f, 0f);
            progressRt.sizeDelta = new Vector2(54f, 46f);

            var item = objectiveGo.AddComponent<RoyalMatch.Rendering.ObjectiveItemView>();
            item.IconLabel = iconLabel;
            item.DescriptionLabel = descLabel;
            item.ProgressLabel = progressLabel;

            PrefabUtility.SaveAsPrefabAsset(objectiveGo, objPath);
            Object.DestroyImmediate(objectiveGo);

            AssetDatabase.SaveAssets();
        }

        private static void CreatePrefabs()
        {
            CreateGemPrefab();
            CreateObstaclePrefab();
            CreateUIPrefabs();
        }

        private static Mesh GetOrCreatePrimitiveMesh(PrimitiveType primitiveType, string assetName)
        {
            string meshPath = Path.Combine(MeshesPath, $"{assetName}.asset");
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
                return existing;

            var primitive = GameObject.CreatePrimitive(primitiveType);
            var source = primitive.GetComponent<MeshFilter>()?.sharedMesh;
            if (source == null)
            {
                Object.DestroyImmediate(primitive);
                throw new MissingReferenceException($"Unity did not provide the {primitiveType} primitive mesh.");
            }

            var mesh = Object.Instantiate(source);
            mesh.name = assetName;
            Object.DestroyImmediate(primitive);
            AssetDatabase.CreateAsset(mesh, meshPath);
            return mesh;
        }

        private static void CreateGemMeshes()
        {
            SavePremiumGemQuad();
            // Custom red crystal chili reconstructed from gema_roja.png. The
            // visible silhouette is modelled in the board plane and receives
            // real depth/faceted normals instead of using the source as a card.
            SaveFacetedGem("GemChili", new[]
            {
                new Vector2(-0.55f, -0.22f), new Vector2(-0.48f, -0.36f),
                new Vector2(-0.30f, -0.43f), new Vector2(-0.08f, -0.42f),
                new Vector2(0.16f, -0.34f), new Vector2(0.34f, -0.18f),
                new Vector2(0.43f, 0.04f), new Vector2(0.40f, 0.25f),
                new Vector2(0.28f, 0.40f), new Vector2(0.10f, 0.47f),
                new Vector2(-0.05f, 0.43f), new Vector2(-0.13f, 0.28f),
                new Vector2(-0.19f, 0.09f), new Vector2(-0.31f, -0.06f),
                new Vector2(-0.46f, -0.14f),
            }, 0.46f);
            SaveFacetedGem("GemChiliStem", new[]
            {
                new Vector2(-0.06f, 0.34f), new Vector2(0.06f, 0.45f),
                new Vector2(0.16f, 0.54f), new Vector2(0.29f, 0.57f),
                new Vector2(0.39f, 0.52f), new Vector2(0.34f, 0.45f),
                new Vector2(0.25f, 0.47f), new Vector2(0.17f, 0.40f),
                new Vector2(0.10f, 0.29f),
            }, 0.27f);
            SaveFacetedGem("GemRuby", new[]
            {
                new Vector2(-0.32f, 0.48f), new Vector2(0.32f, 0.48f),
                new Vector2(0.49f, 0.26f), new Vector2(0.49f, -0.26f),
                new Vector2(0.32f, -0.48f), new Vector2(-0.32f, -0.48f),
                new Vector2(-0.49f, -0.26f), new Vector2(-0.49f, 0.26f),
            }, 0.48f);
            SaveFacetedGem("GemDiamond", new[]
            {
                new Vector2(0f, 0.57f), new Vector2(0.51f, 0f),
                new Vector2(0f, -0.57f), new Vector2(-0.51f, 0f),
            }, 0.58f);
            SaveFacetedGem("GemEmerald", new[]
            {
                new Vector2(-0.29f, 0.54f), new Vector2(0.29f, 0.54f),
                new Vector2(0.46f, 0.36f), new Vector2(0.46f, -0.36f),
                new Vector2(0.29f, -0.54f), new Vector2(-0.29f, -0.54f),
                new Vector2(-0.46f, -0.36f), new Vector2(-0.46f, 0.36f),
            }, 0.42f);
            SaveFacetedGem("GemHex", RegularOutline(6, 0.53f, 30f), 0.48f);
            SaveFacetedGem("GemPear", new[]
            {
                new Vector2(0f, 0.59f), new Vector2(0.23f, 0.31f),
                new Vector2(0.42f, 0.04f), new Vector2(0.40f, -0.25f),
                new Vector2(0.24f, -0.48f), new Vector2(0f, -0.55f),
                new Vector2(-0.24f, -0.48f), new Vector2(-0.40f, -0.25f),
                new Vector2(-0.42f, 0.04f), new Vector2(-0.23f, 0.31f),
            }, 0.5f);
            var oval = RegularOutline(12, 0.5f, 15f);
            for (int i = 0; i < oval.Length; i++) oval[i].y *= 1.1f;
            SaveFacetedGem("GemOval", oval, 0.44f);
        }

        private static void SavePremiumGemQuad()
        {
            string meshPath = Path.Combine(MeshesPath, "PremiumGemQuad.asset");
            AssetDatabase.DeleteAsset(meshPath);
            var mesh = new Mesh { name = "PremiumGemQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.54f, 0f, -0.54f),
                new Vector3(-0.54f, 0f,  0.54f),
                new Vector3( 0.54f, 0f,  0.54f),
                new Vector3( 0.54f, 0f, -0.54f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 0f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        private static string[] PremiumGemTexturePaths()
        {
            return new[]
            {
                "Assets/Art/Gems/gema-roja-cristal-premium.png",
                "Assets/Art/Gems/gema-azul-cristal-premium.png",
                "Assets/Art/Gems/gema-verde-cristal-premium.png",
                "Assets/Art/Gems/gema-amarilla-cristal-premium.png",
                "Assets/Art/Gems/gema-morada-cristal-premium.png",
                "Assets/Art/Gems/gema-cian-cristal-premium.png",
            };
        }

        private static void CreatePremiumGemMaterial(string materialName, string texturePath)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            var material = new Material(shader) { name = materialName };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, Path.Combine(MaterialsPath, materialName + ".mat"));
        }

        private static Vector2[] RegularOutline(int sides, float radius, float rotationDegrees)
        {
            var points = new Vector2[sides];
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < sides; i++)
            {
                // Clockwise in X/Z when viewed by the top-down camera so the
                // crown triangles face +Y instead of being back-face culled.
                float angle = rotation - i * Mathf.PI * 2f / sides;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        private static void SaveFacetedGem(string assetName, Vector2[] outline, float height)
        {
            string meshPath = Path.Combine(MeshesPath, $"{assetName}.asset");
            AssetDatabase.DeleteAsset(meshPath);

            // The crown winding must face +Y for the board camera. Custom
            // silhouettes can arrive in either direction, so normalize them
            // here instead of relying on every outline being authored clockwise.
            float signedArea = 0f;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i];
                Vector2 b = outline[(i + 1) % outline.Length];
                signedArea += a.x * b.y - b.x * a.y;
            }
            if (signedArea > 0f)
                System.Array.Reverse(outline);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float bottomY = -height * 0.42f;
            float shoulderY = 0f;
            float crownY = height * 0.38f;
            Vector2 centroid = Vector2.zero;
            foreach (Vector2 point in outline) centroid += point;
            centroid /= outline.Length;
            Vector3 topCenter = new Vector3(centroid.x, height * 0.56f, centroid.y);
            Vector3 bottomCenter = new Vector3(centroid.x, bottomY, centroid.y);

            void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            }

            for (int i = 0; i < outline.Length; i++)
            {
                int next = (i + 1) % outline.Length;
                Vector3 shoulderA = new Vector3(outline[i].x, shoulderY, outline[i].y);
                Vector3 shoulderB = new Vector3(outline[next].x, shoulderY, outline[next].y);
                Vector3 lowerA = new Vector3(outline[i].x * 0.72f, bottomY, outline[i].y * 0.72f);
                Vector3 lowerB = new Vector3(outline[next].x * 0.72f, bottomY, outline[next].y * 0.72f);
                Vector3 crownA = new Vector3(outline[i].x * 0.61f, crownY, outline[i].y * 0.61f);
                Vector3 crownB = new Vector3(outline[next].x * 0.61f, crownY, outline[next].y * 0.61f);

                AddTriangle(lowerA, shoulderB, shoulderA);
                AddTriangle(lowerA, lowerB, shoulderB);
                AddTriangle(shoulderA, shoulderB, crownB);
                AddTriangle(shoulderA, crownB, crownA);
                AddTriangle(crownA, crownB, topCenter);
                AddTriangle(lowerB, lowerA, bottomCenter);
            }

            var mesh = new Mesh { name = assetName };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        #region Scene Creation

        private static void CreateGameScene()
        {
            string scenePath = Path.Combine(ScenesPath, "GameScene.unity");
            var scene = EditorSceneManager.NewScene(new NewSceneSetup(), NewSceneMode.Single);

            // ===== Main Camera =====
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            cam.farClipPlane = 100f;
            cam.nearClipPlane = 0.1f;
            cam.fieldOfView = 45f;
            cam.backgroundColor = new Color(0.15f, 0.1f, 0.3f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(4.2f, 4.2f, -5.5f);
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");

            // ===== Directional Light =====
            var lightGO = new GameObject("Directional Light");
            var dirLight = lightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1.2f;
            dirLight.color = Color.white;
            dirLight.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            Undo.RegisterCreatedObjectUndo(lightGO, "Create Directional Light");

            // ===== Canvas =====
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            // UI must not sit on the camera far clip plane. That caused TMP glyphs
            // and controls to be clipped unpredictably in standalone builds.
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            var canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.anchorMin = Vector2.zero;
            canvasRT.anchorMax = Vector2.one;
            canvasRT.offsetMin = Vector2.zero;
            canvasRT.offsetMax = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

            // ===== EventSystem =====
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");

            // Load prefabs
            var gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(PrefabsPath, "Gem.prefab"));
            var obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(PrefabsPath, "Obstacle.prefab"));
            var boardMat = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(MaterialsPath, "BoardMaterial.mat"));
            var starIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(PrefabsPath, "UI", "StarIcon.prefab"));
            var levelButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(PrefabsPath, "UI", "LevelButton.prefab"));
            var objectiveItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(PrefabsPath, "UI", "ObjectiveItem.prefab"));
            var palaceBackground = LoadUiSprite("Assets/Art/UI/palace-menu-background.png");

            // ===== BoardView =====
            var boardViewGO = new GameObject("BoardView");
            var boardView = boardViewGO.AddComponent<RoyalMatch.Rendering.BoardView>();
            boardView.GemPrefab = gemPrefab;
            boardView.ObstaclePrefab = obstaclePrefab;
            boardView.BoardCamera = cam;
            boardView.BoardMaterial = boardMat;
            boardView.CellSize = 1.2f;
            boardView.GridRows = Levels.DifficultySystem.GridRows;
            boardView.GridCols = Levels.DifficultySystem.GridCols;
            boardView.CameraOffset = new Vector3(0f, 4.2f, 5.5f);

            // Board background
            var bgGO = new GameObject("BoardBackground", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            var bgMF = bgGO.GetComponent<MeshFilter>();
            bgMF.sharedMesh = GetOrCreatePrimitiveMesh(PrimitiveType.Cube, "BoardCube");
            var bgMR = bgGO.GetComponent<MeshRenderer>();
            if (boardMat != null) bgMR.sharedMaterial = boardMat;
            var bgRT = bgGO.transform;
            bgRT.SetParent(boardViewGO.transform);
            bgRT.localPosition = new Vector3(0f, -0.18f, 0f);
            bgRT.localScale = new Vector3(10f, 0.2f, 10f);
            bgGO.AddComponent<Rigidbody>().isKinematic = true;
            boardView.BoardBackground = bgGO;

            Undo.RegisterCreatedObjectUndo(boardViewGO, "Create BoardView");

            // ===== VFX Manager =====
            var vfxGO = new GameObject("VFXManager");
            var vfxManager = vfxGO.AddComponent<RoyalMatch.Rendering.VFXManager>();

            CreateVFXChild(vfxGO, "MatchPopVFX", VFXType.MatchPop);
            CreateVFXChild(vfxGO, "CascadeSparkleVFX", VFXType.CascadeSparkle);
            CreateVFXChild(vfxGO, "SpecialExplosionVFX", VFXType.SpecialExplosion);
            CreateVFXChild(vfxGO, "LevelCompleteVFX", VFXType.LevelComplete);
            CreateVFXChild(vfxGO, "GemSpawnVFX", VFXType.GemSpawn);

            vfxManager.MatchPopVFX = vfxGO.transform.Find("MatchPopVFX").GetComponent<ParticleSystem>();
            vfxManager.CascadeSparkleVFX = vfxGO.transform.Find("CascadeSparkleVFX").GetComponent<ParticleSystem>();
            vfxManager.SpecialExplosionVFX = vfxGO.transform.Find("SpecialExplosionVFX").GetComponent<ParticleSystem>();
            vfxManager.LevelCompleteVFX = vfxGO.transform.Find("LevelCompleteVFX").GetComponent<ParticleSystem>();
            vfxManager.GemSpawnVFX = vfxGO.transform.Find("GemSpawnVFX").GetComponent<ParticleSystem>();

            Undo.RegisterCreatedObjectUndo(vfxGO, "Create VFXManager");

            // ===== Audio =====
            var audioGO = new GameObject("GameAudio");
            var audio = audioGO.AddComponent<RoyalMatch.Audio.GameAudioManager>();
            audio.MusicLoop = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/palace-loop.wav");
            audio.VictoryFanfareClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/victory-fanfare.wav");
            audio.ApplauseClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/royal-applause.wav");
            audio.VictoryVoiceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/victory-wow.wav");
            var wow = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/wow.wav");
            var fabulous = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/fabulous.wav");
            audio.VoiceReactions = new[] { wow, fabulous }.Where(clip => clip != null).ToArray();
            Undo.RegisterCreatedObjectUndo(audioGO, "Create Game Audio");

            // ===== Animation Player =====
            var animPlayerGO = new GameObject("AnimationPlayer");
            var animPlayer = animPlayerGO.AddComponent<RoyalMatch.Rendering.AnimationPlayer>();
            animPlayer.BoardView = boardView;
            animPlayer.VFXManager = vfxManager;
            Undo.RegisterCreatedObjectUndo(animPlayerGO, "Create AnimationPlayer");

            // ===== Game State Machine =====
            var gsmGO = new GameObject("GameStateMachine");
            gsmGO.AddComponent<RoyalMatch.State.GameStateMachine>();
            Undo.RegisterCreatedObjectUndo(gsmGO, "Create GameStateMachine");

            // ===== Game Controller =====
            var gcGO = new GameObject("GameController");
            var gameController = gcGO.AddComponent<RoyalMatch.Core.GameController>();
            gameController.BoardView = boardView;
            gameController.AnimationPlayer = animPlayer;
            gameController.VFXManager = vfxManager;
            Undo.RegisterCreatedObjectUndo(gcGO, "Create GameController");

            // GameController will find HUD and GameOverPanel via FindFirstObjectByType in Awake

            // ===== UI Panels within Canvas =====
            CreateUIInCanvas(canvasGO.transform, cam, boardView, animPlayer, vfxManager,
                starIconPrefab, levelButtonPrefab, objectiveItemPrefab, palaceBackground);

            GamePresentationSetup.ApplyToOpenScene();

            // Save
            EditorSceneManager.SaveScene(scene, scenePath, false);
            Debug.Log($"GameScene saved to {scenePath}");
        }

        private static void CreateVFXChild(GameObject parent, string name, VFXType type)
        {
            var childGO = new GameObject(name);
            childGO.layer = LayerMask.NameToLayer("Ignore Raycast");
            childGO.transform.SetParent(parent.transform);
            var ps = childGO.AddComponent<ParticleSystem>();
            PremiumVFXSetup.Configure(ps, type);
        }

        private static void ConfigureAudioAssets()
        {
            string[] audioPaths =
            {
                "Assets/Audio/Music/palace-loop.wav",
                "Assets/Audio/SFX/victory-fanfare.wav",
                "Assets/Audio/SFX/royal-applause.wav",
                "Assets/Audio/Voice/victory-wow.wav",
                "Assets/Audio/Voice/wow.wav",
                "Assets/Audio/Voice/fabulous.wav",
            };

            foreach (string audioPath in audioPaths)
            {
                if (AssetImporter.GetAtPath(audioPath) is not AudioImporter importer)
                    continue;

                var settings = importer.defaultSampleSettings;
                settings.loadType = audioPath.Contains("/Music/")
                    ? AudioClipLoadType.Streaming
                    : AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.82f;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = false;
                importer.forceToMono = true;
                importer.ambisonic = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreateUIInCanvas(
            Transform canvasTransform, Camera cam,
            RoyalMatch.Rendering.BoardView boardView,
            RoyalMatch.Rendering.AnimationPlayer animPlayer,
            RoyalMatch.Rendering.VFXManager vfxManager,
            GameObject starIconPrefab,
            GameObject levelButtonPrefab,
            GameObject objectiveItemPrefab,
            Sprite palaceBackground)
        {
            // ===== Splash Panel =====
            var splashPanel = CreatePanel(canvasTransform, "SplashPanel", new Color(0.08f, 0.03f, 0.16f, 0.22f));
            CreateFullscreenImage(splashPanel.transform, "PalaceBackground", palaceBackground, Color.white, 0);
            CreateFullscreenImage(splashPanel.transform, "Atmosphere", null, new Color(0.08f, 0.02f, 0.18f, 0.3f), 1);
            var splash = splashPanel.AddComponent<RoyalMatch.UI.SplashScreenUI>();
            splash.Duration = 3.8f;

            var developerRoot = CreateUiRoot(splashPanel.transform, "DeveloperMark", Vector2.zero, new Vector2(520, 220));
            var developerGroup = developerRoot.gameObject.AddComponent<CanvasGroup>();
            var developerTitle = CreateText(developerRoot, "DeveloperLabel", "OLCAS", new Vector2(0, 24), new Vector2(500, 96), 72);
            developerTitle.color = new Color(1f, 0.85f, 0.34f);
            AddTextOutline(developerTitle, new Color(0.12f, 0.04f, 0.22f), new Vector2(3f, -3f));
            var developerSubtitle = CreateText(developerRoot, "StudioLabel", "G A M E S", new Vector2(0, -48), new Vector2(360, 44), 24);
            developerSubtitle.color = new Color(0.95f, 0.91f, 0.78f);

            var gameTitleRoot = CreateUiRoot(splashPanel.transform, "GameTitleMark", Vector2.zero, new Vector2(760, 260));
            var gameTitleGroup = gameTitleRoot.gameObject.AddComponent<CanvasGroup>();
            var smallTitle = CreateText(gameTitleRoot, "TitleTop", "EL PALACIO DE LAS", new Vector2(0, 64), new Vector2(700, 60), 34);
            smallTitle.color = new Color(1f, 0.95f, 0.78f);
            AddTextOutline(smallTitle, new Color(0.14f, 0.04f, 0.25f), new Vector2(2f, -2f));
            var bigTitle = CreateText(gameTitleRoot, "TitleMain", "MIL GEMAS", new Vector2(0, -8), new Vector2(740, 110), 78);
            bigTitle.color = new Color(1f, 0.8f, 0.2f);
            AddTextOutline(bigTitle, new Color(0.18f, 0.04f, 0.3f), new Vector2(4f, -4f));
            var titleTag = CreateText(gameTitleRoot, "TitleTag", "UNA AVENTURA MATCH-3", new Vector2(0, -84), new Vector2(500, 42), 20);
            titleTag.color = new Color(0.9f, 0.86f, 1f);

            splash.TitleLabel = developerTitle;
            splash.SubtitleLabel = developerSubtitle;
            splash.DeveloperGroup = developerGroup;
            splash.GameTitleGroup = gameTitleGroup;
            splash.DeveloperMark = developerRoot;
            splash.GameTitleMark = gameTitleRoot;

            // ===== Main Menu Panel =====
            var mainMenuPanel = CreatePanel(canvasTransform, "MainMenuPanel", new Color(0.08f, 0.02f, 0.16f, 0.18f));
            CreateFullscreenImage(mainMenuPanel.transform, "PalaceBackground", palaceBackground, Color.white, 0);
            CreateFullscreenImage(mainMenuPanel.transform, "Atmosphere", null, new Color(0.07f, 0.015f, 0.15f, 0.26f), 1);
            var mainMenu = mainMenuPanel.AddComponent<RoyalMatch.UI.MainMenuUI>();
            mainMenu.MenuGroup = mainMenuPanel.AddComponent<CanvasGroup>();

            var menuCard = CreateUiPanel(mainMenuPanel.transform, "RoyalCard", new Vector2(0, -8), new Vector2(530, 610), new Color(0.14f, 0.055f, 0.28f, 0.82f));
            var cardOutline = menuCard.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.95f, 0.74f, 0.23f, 0.9f);
            cardOutline.effectDistance = new Vector2(4f, -4f);

            var titleRoot = CreateUiRoot(menuCard, "TitleRoot", new Vector2(0, 165), new Vector2(480, 210));
            var titleTop = CreateText(titleRoot, "TitleTop", "EL PALACIO DE LAS", new Vector2(0, 45), new Vector2(460, 48), 28);
            titleTop.color = new Color(1f, 0.95f, 0.78f);
            var titleMain = CreateText(titleRoot, "TitleMain", "MIL GEMAS", new Vector2(0, -20), new Vector2(480, 100), 64);
            titleMain.color = new Color(1f, 0.8f, 0.2f);
            AddTextOutline(titleMain, new Color(0.16f, 0.03f, 0.27f), new Vector2(4f, -4f));
            var subtitle = CreateText(titleRoot, "Subtitle", "COMBINA  •  CREA PODERES  •  RESTAURA", new Vector2(0, -84), new Vector2(470, 34), 15);
            subtitle.color = new Color(0.87f, 0.82f, 0.96f);

            var actionsRoot = CreateUiRoot(menuCard, "ActionsRoot", new Vector2(0, -105), new Vector2(440, 230));
            var playBtn = CreateButton(actionsRoot, "PlayButton", new Vector2(0, 58), new Vector2(330, 76), "JUGAR");
            mainMenu.PlayButton = playBtn.GetComponent<Button>();
            StylePrimaryButton(playBtn);

            var levelSelectBtn = CreateButton(actionsRoot, "LevelSelectButton", new Vector2(-88, -30), new Vector2(160, 56), "NIVELES");
            mainMenu.LevelSelectButton = levelSelectBtn.GetComponent<Button>();
            StyleSecondaryButton(levelSelectBtn);

            var settingsBtn = CreateButton(actionsRoot, "SettingsButton", new Vector2(88, -30), new Vector2(160, 56), "AJUSTES");
            mainMenu.SettingsButton = settingsBtn.GetComponent<Button>();
            StyleSecondaryButton(settingsBtn);

            mainMenu.VersionLabel = CreateText(menuCard, "VersionLabel", "OLCAS GAMES  •  2026", new Vector2(0, -278), new Vector2(320, 28), 13);
            mainMenu.VersionLabel.color = new Color(0.76f, 0.7f, 0.86f);
            mainMenu.TitleRoot = titleRoot;
            mainMenu.ActionsRoot = actionsRoot;

            // ===== Level Select Panel =====
            var levelSelectPanel = CreatePanel(canvasTransform, "LevelSelectPanel");
            var levelSelect = levelSelectPanel.AddComponent<RoyalMatch.UI.LevelSelectUI>();
            CreateText(levelSelectPanel.transform, "TitleLabel", "SELECT LEVEL", new Vector2(0, 250), new Vector2(300, 50), 28);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.SetParent(levelSelectPanel.transform);
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
            contentRT.anchoredPosition = new Vector2(0, 50);
            contentRT.sizeDelta = new Vector2(500, 600);
            var vScroll = contentGO.AddComponent<RectTransform>();

            levelSelect.ContentParent = contentGO.transform;
            levelSelect.LevelButtonPrefab = levelButtonPrefab;

            var backBtn = CreateButton(levelSelectPanel.transform, "BackButton", new Vector2(0, -320), new Vector2(120, 40), "BACK");
            levelSelect.BackButton = backBtn.GetComponent<Button>();

            // World tabs - not setting up buttons for now, LevelSelectUI works without them
            levelSelect.WorldTabs = new RoyalMatch.UI.WorldTab[0];

            // ===== HUD Panel =====
            var hudPanel = CreatePanel(canvasTransform, "HUDPanel", new Color(0, 0, 0, 0));
            hudPanel.AddComponent<CanvasGroup>();
            var hud = hudPanel.AddComponent<RoyalMatch.Rendering.HUD>();

            var topBar = new GameObject("TopBar", typeof(RectTransform));
            var topBarRT = topBar.GetComponent<RectTransform>();
            topBarRT.SetParent(hudPanel.transform);
            topBarRT.anchorMin = new Vector2(0, 1);
            topBarRT.anchorMax = new Vector2(1, 1);
            topBarRT.pivot = new Vector2(0.5f, 1f);
            topBarRT.anchoredPosition = new Vector2(0, -18);
            topBarRT.sizeDelta = new Vector2(0, 64);

            var topBarImage = topBar.AddComponent<Image>();
            topBarImage.sprite = CreateTempSprite();
            topBarImage.color = new Color(0.08f, 0.02f, 0.18f, 0.78f);

            CreateText(topBar.transform, "LevelLabel", "LEVEL", new Vector2(-360, 0), new Vector2(80, 30), 16);
            hud.LevelValue = CreateText(topBar.transform, "LevelValue", "1", new Vector2(-290, 0), new Vector2(60, 30), 16);
            CreateText(topBar.transform, "MovesLabel", "MOVES", new Vector2(-170, 0), new Vector2(80, 30), 16);
            hud.MovesValue = CreateText(topBar.transform, "MovesValue", "0/0", new Vector2(-90, 0), new Vector2(70, 30), 16);
            CreateText(topBar.transform, "ScoreLabel", "SCORE", new Vector2(140, 0), new Vector2(80, 30), 16);
            hud.ScoreValue = CreateText(topBar.transform, "ScoreValue", "0", new Vector2(230, 0), new Vector2(120, 30), 16);

            // Stars container
            var starsGO = new GameObject("StarsContainer", typeof(RectTransform));
            var starsRT = starsGO.GetComponent<RectTransform>();
            starsRT.SetParent(hudPanel.transform);
            starsRT.anchorMin = new Vector2(1, 1);
            starsRT.anchorMax = new Vector2(1, 1);
            starsRT.anchoredPosition = new Vector2(-105, -49);
            starsRT.sizeDelta = new Vector2(120, 36);
            var starRow = starsGO.AddComponent<HorizontalLayoutGroup>();
            starRow.spacing = 10f;
            starRow.childAlignment = TextAnchor.MiddleCenter;
            starRow.childForceExpandWidth = false;
            starRow.childForceExpandHeight = false;
            hud.StarsContainer = starsGO.transform;
            hud.StarIconPrefab = starIconPrefab;

            // Objectives
            var objGO = new GameObject("ObjectivesContainer", typeof(RectTransform));
            var objRT = objGO.GetComponent<RectTransform>();
            objRT.SetParent(hudPanel.transform);
            objRT.anchorMin = new Vector2(0, 0.5f);
            objRT.anchorMax = new Vector2(0, 0.5f);
            objRT.pivot = new Vector2(0, 0.5f);
            objRT.anchoredPosition = new Vector2(24, 0);
            objRT.sizeDelta = new Vector2(240, 250);
            var vlayout = objGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlayout.spacing = 10f;
            vlayout.childAlignment = TextAnchor.MiddleLeft;
            vlayout.childForceExpandWidth = false;
            vlayout.childForceExpandHeight = false;
            hud.ObjectivesContainer = objGO.transform;
            hud.ObjectiveItemPrefab = objectiveItemPrefab;

            // Pause button
            var pauseBtn = CreateButton(hudPanel.transform, "PauseButton", Vector2.zero, new Vector2(54, 54), "II");
            var pauseRT = pauseBtn.GetComponent<RectTransform>();
            pauseRT.anchorMin = Vector2.one;
            pauseRT.anchorMax = Vector2.one;
            pauseRT.pivot = Vector2.one;
            pauseRT.anchoredPosition = new Vector2(-22, -22);
            var pauseImg = pauseBtn.GetComponent<Image>();
            pauseImg.color = new Color(0.2f, 0.2f, 0.3f, 0.7f);

            // Loading overlay
            var loadingGO = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(Image));
            var loadingRT = loadingGO.GetComponent<RectTransform>();
            loadingRT.SetParent(hudPanel.transform);
            loadingRT.anchorMin = Vector2.zero;
            loadingRT.anchorMax = Vector2.one;
            loadingRT.offsetMin = Vector2.zero;
            loadingRT.offsetMax = Vector2.zero;
            var loadingImg = loadingGO.GetComponent<Image>();
            loadingImg.color = new Color(0, 0, 0, 0.7f);
            loadingGO.SetActive(false);
            hud.LoadingOverlay = loadingGO;

            var loadingBar = new GameObject("LoadingBar", typeof(RectTransform)).AddComponent<Slider>();
            loadingBar.transform.SetParent(loadingGO.transform);
            loadingRT = loadingBar.GetComponent<RectTransform>();
            loadingBar.fillRect = new GameObject("FillArea").AddComponent<RectTransform>();
            loadingBar.fillRect.gameObject.AddComponent<Image>().color = Color.yellow;
            loadingBar.fillRect.SetParent(loadingBar.transform);
            hud.LoadingBar = loadingBar;

            // ===== Game Over Panel =====
            var gameOverPanel = CreatePanel(canvasTransform, "GameOverPanel", new Color(0, 0, 0, 0.7f));
            gameOverPanel.AddComponent<CanvasGroup>();
            var gameOver = gameOverPanel.AddComponent<RoyalMatch.Rendering.GameOverPanel>();
            gameOver.PanelRoot = gameOverPanel;
            gameOver.TitleLabel = CreateText(gameOverPanel.transform, "TitleLabel", "LEVEL COMPLETE", new Vector2(0, 80), new Vector2(300, 50), 28);

            // Stars container for game over
            var goStarsGO = new GameObject("StarsContainer", typeof(RectTransform));
            var goStarsRT = goStarsGO.GetComponent<RectTransform>();
            goStarsRT.SetParent(gameOverPanel.transform);
            goStarsRT.anchorMin = new Vector2(0.5f, 0.5f);
            goStarsRT.anchorMax = new Vector2(0.5f, 0.5f);
            goStarsRT.anchoredPosition = new Vector2(0, 0);
            goStarsRT.sizeDelta = new Vector2(200, 50);
            gameOver.StarsContainer = goStarsGO.transform;
            gameOver.StarIconPrefab = starIconPrefab;

            gameOver.RewardLabel = CreateText(gameOverPanel.transform, "RewardLabel", "", new Vector2(0, -40), new Vector2(200, 30), 16);

            var nextBtn = CreateButton(gameOverPanel.transform, "NextLevelButton", new Vector2(-130, -100), new Vector2(120, 50), "NEXT");
            gameOver.NextLevelButton = nextBtn.GetComponent<Button>();

            var retryBtn = CreateButton(gameOverPanel.transform, "RetryButton", new Vector2(0, -100), new Vector2(120, 50), "RETRY");
            gameOver.RetryButton = retryBtn.GetComponent<Button>();

            var mainMenuBtn = CreateButton(gameOverPanel.transform, "MainMenuButton", new Vector2(130, -100), new Vector2(120, 50), "MENU");
            gameOver.MainMenuButton = mainMenuBtn.GetComponent<Button>();

            // ===== Settings Panel =====
            var settingsPanel = CreatePanel(canvasTransform, "SettingsPanel", new Color(0, 0, 0, 0.8f));
            settingsPanel.AddComponent<CanvasGroup>();
            var settings = settingsPanel.AddComponent<RoyalMatch.UI.SettingsMenu>();

            CreateText(settingsPanel.transform, "TitleLabel", "SETTINGS", new Vector2(0, 80), new Vector2(200, 50), 28);

            settings.SoundToggle = CreateToggle(settingsPanel.transform, "SoundToggle", new Vector2(0, 20), "Sound", true);
            settings.MusicToggle = CreateToggle(settingsPanel.transform, "MusicToggle", new Vector2(0, -10), "Music", true);
            settings.VibrationToggle = CreateToggle(settingsPanel.transform, "VibrationToggle", new Vector2(0, -40), "Vibration", true);

            var settingsBackBtn = CreateButton(settingsPanel.transform, "BackButton", new Vector2(0, -100), new Vector2(120, 40), "BACK");
            settings.BackButton = settingsBackBtn.GetComponent<Button>();

            // ===== Game Scene Manager =====
            var gsmGO = new GameObject("GameSceneManager");
            var gsm = gsmGO.AddComponent<RoyalMatch.UI.GameSceneManager>();
            gsm.SplashPanel = splashPanel;
            gsm.MainMenuPanel = mainMenuPanel;
            gsm.LevelSelectPanel = levelSelectPanel;
            gsm.SettingsPanel = settingsPanel;
            gsm.GameOverPanel = gameOverPanel;
            gsm.HUDRoot = hudPanel;
            gsm.SplashDuration = 0.8f;
            gsm.BootstrapScene = "Bootstrap";
            gsm.GameScene = "GameScene";
            Undo.RegisterCreatedObjectUndo(gsmGO, "Create GameSceneManager");

            // ===== Safe Area =====
            var safeAreaGO = new GameObject("SafeAreaContainer", typeof(RectTransform));
            var safeAreaRT = safeAreaGO.GetComponent<RectTransform>();
            safeAreaRT.SetParent(canvasTransform);
            safeAreaRT.anchorMin = Vector2.zero;
            safeAreaRT.anchorMax = Vector2.one;
            safeAreaRT.offsetMin = Vector2.zero;
            safeAreaRT.offsetMax = Vector2.zero;
            safeAreaGO.AddComponent<RoyalMatch.UI.SafeArea>();
            safeAreaGO.transform.SetParent(canvasTransform, false);

            // Initial state: hide all panels except splash
            splashPanel.SetActive(true);
            mainMenuPanel.SetActive(false);
            levelSelectPanel.SetActive(false);
            settingsPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            hudPanel.SetActive(false);
        }

        private static GameObject CreatePanel(Transform parent, string name, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color ?? new Color(0, 0, 0, 0.85f);
            return go;
        }

        private static Sprite LoadUiSprite(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"UI artwork not found at {assetPath}; using a generated color background.");
                return null;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer &&
                importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Image CreateFullscreenImage(Transform parent, string name, Sprite sprite, Color color, int siblingIndex)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(siblingIndex);
            var image = go.GetComponent<Image>();
            image.sprite = sprite ?? CreateTempSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateUiRoot(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform CreateUiPanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var rt = CreateUiRoot(parent, name, anchoredPos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = CreateTempSprite();
            image.color = color;
            return rt;
        }

        private static void AddTextOutline(TextMeshProUGUI text, Color color, Vector2 distance)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void StylePrimaryButton(GameObject button)
        {
            var image = button.GetComponent<Image>();
            image.color = new Color(0.96f, 0.63f, 0.08f, 1f);
            var outline = button.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.16f, 0.02f, 1f);
            outline.effectDistance = new Vector2(0f, -5f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Bold;
            AddTextOutline(label, new Color(0.35f, 0.08f, 0.02f, 0.8f), new Vector2(2f, -2f));
            ConfigureButtonColors(button.GetComponent<Button>(), image.color);
        }

        private static void StyleSecondaryButton(GameObject button)
        {
            var image = button.GetComponent<Image>();
            image.color = new Color(0.34f, 0.16f, 0.62f, 0.96f);
            var outline = button.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.63f, 1f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            ConfigureButtonColors(button.GetComponent<Button>(), image.color);
        }

        private static void ConfigureButtonColors(Button button, Color normal)
        {
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = CreateTempSprite();
            img.color = new Color(0.2f, 0.3f, 0.6f, 0.9f);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.SetParent(go.transform, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private static Toggle CreateToggle(Transform parent, string name, Vector2 anchoredPos, string label, bool defaultValue)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(250, 40);

            var bgImg = go.GetComponent<Image>();
            bgImg.sprite = CreateTempSprite();
            bgImg.color = new Color(0.15f, 0.15f, 0.25f, 0.7f);

            // Checkmark child
            var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var cmRT = checkmarkGO.GetComponent<RectTransform>();
            cmRT.SetParent(go.transform, false);
            cmRT.anchorMin = Vector2.zero;
            cmRT.anchorMax = Vector2.one;
            cmRT.offsetMin = new Vector2(4, 4);
            cmRT.offsetMax = new Vector2(-4, -4);
            var cmImg = checkmarkGO.GetComponent<Image>();
            cmImg.sprite = CreateTempSprite();
            cmImg.color = Color.white;
            cmImg.enabled = false;

            var toggle = go.AddComponent<Toggle>();
            toggle.graphic = cmImg;
            toggle.isOn = defaultValue;

            return toggle;
        }

        #endregion

        private static void CreateBootstrapScene()
        {
            string scenePath = Path.Combine(ScenesPath, "Bootstrap.unity");
            EditorSceneManager.NewScene(new NewSceneSetup(), NewSceneMode.Single);
            var scene = EditorSceneManager.GetActiveScene();

            var camGO = new GameObject("Main Camera");
            camGO.AddComponent<Camera>();

            var lightGO = new GameObject("Directional Light");
            lightGO.AddComponent<Light>().type = LightType.Directional;

            var bootstrapGO = new GameObject("Bootstrap");
            var bootstrap = bootstrapGO.AddComponent<RoyalMatch.Core.Bootstrap>();
            bootstrap.SplashDelay = 0.5f;

            EditorSceneManager.SaveScene(scene, scenePath, false);
            Debug.Log($"Bootstrap scene saved to {scenePath}");
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new[] {
                Path.Combine(ScenesPath, "Bootstrap.unity"),
                Path.Combine(ScenesPath, "GameScene.unity"),
            };

            var editorBuildSettings = new List<EditorBuildSettingsScene>();
            foreach (var scenePath in scenes)
            {
                if (File.Exists(scenePath))
                {
                    editorBuildSettings.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }
            EditorBuildSettings.scenes = editorBuildSettings.ToArray();
            Debug.Log($"Build settings configured with {editorBuildSettings.Count} scenes.");
        }
    }
}
