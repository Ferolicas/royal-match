using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using RoyalMatch.Rendering;

namespace RoyalMatch.EditorTools
{
    public static class SpecialPresentationSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Gem.prefab";
        private const string ArtFolder = "Assets/Art/Specials/";
        private const string MaterialFolder = "Assets/Materials/Specials/";

        [MenuItem("Royal Match/Upgrade Special Pieces")]
        public static void UpgradePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void Apply(GameObject root)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials/Specials"))
                AssetDatabase.CreateFolder("Assets/Materials", "Specials");
            var old = root.transform.Find("SpecialVisuals");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var group = new GameObject("SpecialVisuals").transform;
            group.SetParent(root.transform, false);

            var renderers = new MeshRenderer[5];
            renderers[0] = Face(group, "RocketHorizontal", "line-rocket.png", "Rocket");
            renderers[1] = Face(group, "RocketVertical", "line-rocket.png", "Rocket");
            renderers[1].transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            renderers[2] = Face(group, "Bomb", "jewel-bomb.png", "Bomb");
            renderers[3] = Face(group, "RainbowBall", "rainbow-ball.png", "RainbowBall");
            renderers[4] = Face(group, "FlyingPropeller", "flying-propeller.png", "Propeller");
            root.GetComponent<GemView>().SpecialRenderers = renderers;
        }

        private static MeshRenderer Face(Transform parent, string name, string textureName,
            string materialName)
        {
            var face = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            face.transform.SetParent(parent, false);
            face.transform.localPosition = new Vector3(0f, 0.39f, 0f);
            face.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                "Assets/Meshes/PremiumGemQuad.asset");
            var renderer = face.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Material(textureName, materialName);
            renderer.enabled = false;
            return renderer;
        }

        private static Material Material(string textureName, string name)
        {
            string texturePath = ArtFolder + textureName;
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Missing special texture: " + texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            string path = MaterialFolder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
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
    }
}
