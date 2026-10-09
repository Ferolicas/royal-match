using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using RoyalMatch.Rendering;

namespace RoyalMatch.EditorTools
{
    public static class ObstaclePresentationSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Obstacle.prefab";
        private const string MaterialFolder = "Assets/Materials/Obstacles/";
        private const string ArtFolder = "Assets/Art/Obstacles/";

        [MenuItem("Royal Match/Upgrade Obstacles")]
        public static void UpgradePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("All four obstacle types upgraded with premium jewel art and damage stages.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void Apply(GameObject root)
        {
            var view = root.GetComponent<ObstacleView>();
            if (view == null) view = root.AddComponent<ObstacleView>();
            view.MeshRenderer = root.GetComponent<MeshRenderer>();

            var darkWood = Material("DarkWood", new Color(0.16f, 0.045f, 0.28f), 0.55f, 0.08f);
            var warmWood = Material("WarmWood", new Color(0.32f, 0.08f, 0.52f), 0.64f, 0.08f);
            var lightWood = Material("LightWood", new Color(0.44f, 0.14f, 0.68f), 0.70f, 0.08f);
            var gold = Material("ChestGold", new Color(0.87f, 0.58f, 0.14f), 0.82f, 0.72f);
            var teal = Material("ChestGem", new Color(0.05f, 0.80f, 0.78f), 0.86f, 0.24f,
                new Color(0.01f, 0.23f, 0.22f));
            var red = Material("CageLock", new Color(0.72f, 0.13f, 0.23f), 0.60f, 0.34f);
            var ice = Material("IceFacet", new Color(0.62f, 0.93f, 1f), 0.87f, 0.04f,
                new Color(0.06f, 0.22f, 0.28f));
            var stone = Material("StoneFacet", new Color(0.57f, 0.58f, 0.68f), 0.22f, 0.05f);
            var crack = Material("DamageCrack", new Color(0.14f, 0.12f, 0.18f), 0.12f, 0f);

            RemoveOld(root.transform, "BoxDecor");
            RemoveOld(root.transform, "CageDecor");
            RemoveOld(root.transform, "IceDecor");
            RemoveOld(root.transform, "StoneDecor");
            RemoveOld(root.transform, "DamageMarks");

            var box = Group(root.transform, "BoxDecor");
            Part(box, "DarkEdge", Vector3.up * 0.515f, new Vector3(1.04f, 0.055f, 1.04f), darkWood);
            var shards = new Transform[5];
            shards[0] = Part(box, "PlankLeft", new Vector3(-0.30f, 0.565f, 0f),
                new Vector3(0.27f, 0.055f, 0.86f), warmWood);
            shards[1] = Part(box, "PlankCenter", new Vector3(0f, 0.565f, 0f),
                new Vector3(0.27f, 0.055f, 0.86f), lightWood);
            shards[2] = Part(box, "PlankRight", new Vector3(0.30f, 0.565f, 0f),
                new Vector3(0.27f, 0.055f, 0.86f), warmWood);
            shards[3] = Part(box, "GoldBandLeft", new Vector3(-0.47f, 0.61f, 0f),
                new Vector3(0.07f, 0.055f, 1.05f), gold);
            shards[4] = Part(box, "GoldBandRight", new Vector3(0.47f, 0.61f, 0f),
                new Vector3(0.07f, 0.055f, 1.05f), gold);
            Part(box, "GoldRimFront", new Vector3(0f, 0.61f, -0.47f),
                new Vector3(1.04f, 0.055f, 0.07f), gold);
            Part(box, "GoldRimBack", new Vector3(0f, 0.61f, 0.47f),
                new Vector3(1.04f, 0.055f, 0.07f), gold);
            var jewel = Part(box, "TurquoiseSeal", new Vector3(0f, 0.65f, 0f),
                new Vector3(0.21f, 0.07f, 0.21f), teal);
            jewel.localRotation = Quaternion.Euler(0f, 45f, 0f);
            PremiumFace(box, "PremiumJewelBoxFace", "premium-jewel-box.png", "PremiumBoxFace");
            view.BoxDecor = box.gameObject;
            view.BreakPieces = shards;

            var cage = Group(root.transform, "CageDecor");
            for (int i = -1; i <= 1; i++)
                Part(cage, "VerticalBar" + i, new Vector3(i * 0.31f, 0.56f, 0f),
                    new Vector3(0.075f, 0.08f, 1.05f), gold);
            for (int i = -1; i <= 1; i += 2)
                Part(cage, "CrossBar" + i, new Vector3(0f, 0.59f, i * 0.33f),
                    new Vector3(1.05f, 0.08f, 0.075f), gold);
            Part(cage, "RedLock", new Vector3(0f, 0.65f, 0f),
                new Vector3(0.24f, 0.10f, 0.24f), red);
            PremiumFace(cage, "PremiumJewelCageFace", "premium-jewel-cage.png", "PremiumCageFace");
            view.CageDecor = cage.gameObject;

            var iceGroup = Group(root.transform, "IceDecor");
            for (int i = -1; i <= 1; i++)
            {
                var facet = Part(iceGroup, "Facet" + i, new Vector3(i * 0.30f, 0.55f, 0f),
                    new Vector3(0.23f, 0.08f, 0.75f), ice);
                facet.localRotation = Quaternion.Euler(0f, i * 16f, 0f);
            }
            PremiumFace(iceGroup, "PremiumIceFace", "premium-ice-block.png", "PremiumIceFace");
            view.IceDecor = iceGroup.gameObject;

            var stoneGroup = Group(root.transform, "StoneDecor");
            Part(stoneGroup, "StonePlate", new Vector3(0f, 0.54f, 0f),
                new Vector3(0.90f, 0.07f, 0.90f), stone);
            Part(stoneGroup, "DarkSeam", new Vector3(0f, 0.59f, 0f),
                new Vector3(0.72f, 0.018f, 0.055f), crack).localRotation = Quaternion.Euler(0f, 28f, 0f);
            PremiumFace(stoneGroup, "PremiumStoneFace", "premium-amethyst-stone.png", "PremiumStoneFace");
            view.StoneDecor = stoneGroup.gameObject;

            var marks = Group(root.transform, "DamageMarks");
            var first = Part(marks, "CrackOne", new Vector3(-0.13f, 0.755f, 0.04f),
                new Vector3(0.055f, 0.022f, 0.68f), crack);
            first.localRotation = Quaternion.Euler(0f, 33f, 0f);
            var second = Part(marks, "CrackTwo", new Vector3(0.18f, 0.765f, -0.07f),
                new Vector3(0.055f, 0.022f, 0.53f), crack);
            second.localRotation = Quaternion.Euler(0f, -47f, 0f);
            view.HealthStages = new[] { first.gameObject, second.gameObject };
            first.gameObject.SetActive(false);
            second.gameObject.SetActive(false);

            box.gameObject.SetActive(false);
            cage.gameObject.SetActive(false);
            iceGroup.gameObject.SetActive(false);
            stoneGroup.gameObject.SetActive(false);
        }

        private static Material Material(string name, Color color, float smoothness, float metallic,
            Color? emission = null)
        {
            string path = MaterialFolder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetColor("_EmissionColor", emission ?? Color.black);
            if (emission.HasValue) material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void PremiumFace(Transform parent, string name, string textureName, string materialName)
        {
            var face = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            face.transform.SetParent(parent, false);
            face.transform.localPosition = new Vector3(0f, 0.735f, 0f);
            face.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                "Assets/Meshes/PremiumGemQuad.asset");
            face.GetComponent<MeshRenderer>().sharedMaterial = PremiumFaceMaterial(textureName, materialName);
        }

        private static Material PremiumFaceMaterial(string textureName, string materialName)
        {
            string texturePath = ArtFolder + textureName;
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Premium obstacle texture is missing: " + texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            string path = MaterialFolder + materialName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.name = materialName;
                AssetDatabase.CreateAsset(material, path);
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Part(Transform parent, string name, Vector3 position,
            Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        private static void RemoveOld(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
    }
}
