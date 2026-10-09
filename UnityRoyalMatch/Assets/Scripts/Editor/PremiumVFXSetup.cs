using System;
using System.IO;
using RoyalMatch.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalMatch.EditorTools
{
    public static class PremiumVFXSetup
    {
        private const string AssetFolder = "Assets/Materials/VFX";

        [MenuItem("Royal Match/Upgrade Premium Effects")]
        public static void UpgradeOpenScene()
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<VFXManager>();
            if (manager == null)
                throw new InvalidOperationException("Open GameScene before upgrading effects.");

            Configure(manager.MatchPopVFX, VFXType.MatchPop);
            Configure(manager.CascadeSparkleVFX, VFXType.CascadeSparkle);
            Configure(manager.SpecialExplosionVFX, VFXType.SpecialExplosion);
            Configure(manager.LevelCompleteVFX, VFXType.LevelComplete);
            Configure(manager.GemSpawnVFX, VFXType.GemSpawn);

            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Premium layered VFX saved in GameScene.");
        }

        public static void Configure(ParticleSystem root, VFXType type)
        {
            if (root == null) return;

            var diamond = EnsureMaterial("Diamond", false);
            var glow = EnsureMaterial("Glow", true);
            var ring = EnsureMaterial("Ring", true);
            var star = EnsureMaterial("Star", true);

            int count;
            float speed, shardSize, haloSize;
            Color tint;
            switch (type)
            {
                case VFXType.CascadeSparkle:
                    count = 7; speed = 0.9f; shardSize = 0.14f; haloSize = 0.85f;
                    tint = new Color(1f, 0.84f, 0.35f); break;
                case VFXType.SpecialExplosion:
                    count = 18; speed = 2.8f; shardSize = 0.30f; haloSize = 2.1f;
                    tint = new Color(1f, 0.43f, 0.78f); break;
                case VFXType.LevelComplete:
                    count = 24; speed = 2.5f; shardSize = 0.27f; haloSize = 2.5f;
                    tint = new Color(1f, 0.87f, 0.35f); break;
                case VFXType.GemSpawn:
                    count = 5; speed = 0.7f; shardSize = 0.12f; haloSize = 0.65f;
                    tint = new Color(0.65f, 0.86f, 1f); break;
                default:
                    count = 10; speed = 1.65f; shardSize = 0.21f; haloSize = 1.2f;
                    tint = Color.white; break;
            }

            ConfigureBurst(root, diamond, tint, count, speed, shardSize, 0.42f);
            ConfigurePulse(Child(root, "WhiteFlash"), glow, Color.white,
                haloSize * 0.78f, 0.18f, 1);
            ConfigurePulse(Child(root, "TintRing"), ring, tint,
                haloSize, 0.34f, 1);
            ConfigureBurst(Child(root, "GoldGlints"), star,
                new Color(1f, 0.91f, 0.62f), Mathf.Max(3, count / 2),
                speed * 0.55f, shardSize * 0.75f, 0.55f);
        }

        private static ParticleSystem Child(ParticleSystem root, string name)
        {
            var child = root.transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.layer = LayerMask.NameToLayer("Ignore Raycast");
                go.transform.SetParent(root.transform, false);
                child = go.transform;
            }
            var system = child.GetComponent<ParticleSystem>();
            if (system == null) system = child.gameObject.AddComponent<ParticleSystem>();
            return system;
        }

        private static void ConfigureBase(ParticleSystem ps, Material material,
            Color tint, float size, float lifetime, int count)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSize = size;
            main.startColor = tint;
            main.maxParticles = Mathf.Max(count + 2, 8);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.enabled = false;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 10;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.8f, 0.35f),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = gradient;
        }

        private static void ConfigureBurst(ParticleSystem ps, Material material,
            Color tint, int count, float speed, float size, float lifetime)
        {
            ConfigureBase(ps, material, tint, size, lifetime, count);
            var main = ps.main;
            main.startSpeed = 0f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-speed, speed);
            velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.65f);
            velocity.z = new ParticleSystem.MinMaxCurve(-speed, speed);
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.45f),
                    new Keyframe(0.18f, 1f), new Keyframe(1f, 0.08f)));
        }

        private static void ConfigurePulse(ParticleSystem ps, Material material,
            Color tint, float size, float lifetime, int count)
        {
            ConfigureBase(ps, material, tint, size, lifetime, count);
            var main = ps.main;
            main.startSpeed = 0f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.32f),
                    new Keyframe(0.35f, 1f), new Keyframe(1f, 1.45f)));
        }

        private static Material EnsureMaterial(string kind, bool additive)
        {
            Directory.CreateDirectory(AssetFolder);
            string path = $"{AssetFolder}/Premium{kind}.mat";
            var texture = EnsureTexture(kind);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP particle shader is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = material == null;
            if (isNew) material = new Material(shader) { name = $"Premium{kind}" };
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Surface", 1f);
            // URP uses 1 for premultiply, which ignores transparent texel RGB and
            // renders our white procedural textures as opaque squares.
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            if (isNew) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D EnsureTexture(string kind)
        {
            string path = $"{AssetFolder}/Premium{kind}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int side = 64;
            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false, true)
            {
                name = $"Premium{kind}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color[side * side];
            for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                float px = ((x + 0.5f) / side - 0.5f) * 2f;
                float py = ((y + 0.5f) / side - 0.5f) * 2f;
                float radius = Mathf.Sqrt(px * px + py * py);
                float alpha;
                switch (kind)
                {
                    case "Ring":
                        alpha = Mathf.Exp(-Mathf.Pow((radius - 0.67f) / 0.075f, 2f));
                        break;
                    case "Diamond":
                        alpha = Mathf.Clamp01((0.78f - Mathf.Abs(px) - Mathf.Abs(py)) * 13f);
                        alpha *= Mathf.Clamp01((1f - radius) * 5f);
                        break;
                    case "Star":
                        float cross = Mathf.Min(Mathf.Abs(px) * 0.65f + Mathf.Abs(py) * 4f,
                            Mathf.Abs(py) * 0.65f + Mathf.Abs(px) * 4f);
                        alpha = Mathf.Pow(Mathf.Clamp01(1f - cross), 1.5f);
                        break;
                    default:
                        alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 2.3f);
                        break;
                }
                pixels[y * side + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }
    }
}
