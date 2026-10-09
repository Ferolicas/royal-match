// DeterministicRNGTests.cs — Ported from utils/DeterministicRNG.test.ts equivalent
// Tests for deterministic random number generation matching the TypeScript LCG

using NUnit.Framework;
using RoyalMatch.Utils;

namespace RoyalMatch.Tests
{
    [TestFixture]
    public class DeterministicRNGTests
    {
        [Test]
        public void NextInRange_ShouldBeDeterministic()
        {
            var rng1 = new DeterministicRNG(42);
            var rng2 = new DeterministicRNG(42);

            int a1 = rng1.NextInRange(0, 100);
            int a2 = rng2.NextInRange(0, 100);

            Assert.That(a1, Is.EqualTo(a2));
        }

        [Test]
        public void NextInRange_ShouldProduceDifferentSequencesWithDifferentSeeds()
        {
            var rng1 = new DeterministicRNG(42);
            var rng2 = new DeterministicRNG(99);

            int a1 = rng1.NextInRange(0, 100);
            int a2 = rng2.NextInRange(0, 100);

            Assert.That(a1, Is.Not.EqualTo(a2));
        }

        [Test]
        public void NextInRange_ShouldStayWithinRange()
        {
            var rng = new DeterministicRNG(1);

            for (int i = 0; i < 1000; i++)
            {
                int value = rng.NextInRange(5, 10);
                Assert.That(value, Is.GreaterThanOrEqualTo(5));
                Assert.That(value, Is.LessThanOrEqualTo(10));
            }
        }

        [Test]
        public void NextInRange_ShouldBeReproducible()
        {
            // Generate sequence from seed 123
            var rng = new DeterministicRNG(123);
            int[] sequence = new int[100];
            for (int i = 0; i < 100; i++)
            {
                sequence[i] = rng.NextInRange(0, 1000);
            }

            // Generate again from same seed
            var rng2 = new DeterministicRNG(123);
            for (int i = 0; i < 100; i++)
            {
                int expected = rng2.NextInRange(0, 1000);
                Assert.That(expected, Is.EqualTo(sequence[i]));
            }
        }

        [Test]
        public void PickOne_ShouldReturnValidElement()
        {
            var rng = new DeterministicRNG(777);
            var items = new string[] { "A", "B", "C", "D" };

            for (int i = 0; i < 100; i++)
            {
                var item = rng.PickOne(items);
                Assert.That(item, Is.Not.Null);
                Assert.That(items, Contains.Item(item));
            }
        }
    }
}
