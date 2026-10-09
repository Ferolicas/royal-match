using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RoyalMatch.Rendering;

namespace RoyalMatch.Tests
{
    public class ObstaclePresentationTests
    {
        [Test]
        public void EveryObstacleTypeHasItsOwnPremiumTexture()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacle.prefab");
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<ObstacleView>();
            AssertPremiumFace(view.BoxDecor, "PremiumJewelBoxFace");
            AssertPremiumFace(view.CageDecor, "PremiumJewelCageFace");
            AssertPremiumFace(view.IceDecor, "PremiumIceFace");
            AssertPremiumFace(view.StoneDecor, "PremiumStoneFace");
        }

        private static void AssertPremiumFace(GameObject group, string name)
        {
            Assert.That(group, Is.Not.Null);
            var face = group.transform.Find(name);
            Assert.That(face, Is.Not.Null, name);
            var renderer = face.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null, name);
            Assert.That(renderer.sharedMaterial, Is.Not.Null, name);
            Assert.That(renderer.sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null, name);
        }
    }
}
