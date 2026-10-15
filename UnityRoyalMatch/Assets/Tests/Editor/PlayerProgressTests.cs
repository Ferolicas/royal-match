using System;
using System.Reflection;
using NUnit.Framework;
using RoyalMatch.Progress;
using RoyalMatch.Types;
using UnityEngine;

namespace RoyalMatch.Tests
{
    public class PlayerProgressTests
    {
        [Test]
        public void AttemptsRewardsAndStartingBoostersAreAppliedOnce()
        {
            const string slotA = "mil_gemas_profile_a";
            const string slotB = "mil_gemas_profile_b";
            const string migrated = "mil_gemas_profile_migrated";
            bool hadA = PlayerPrefs.HasKey(slotA), hadB = PlayerPrefs.HasKey(slotB);
            bool hadMigration = PlayerPrefs.HasKey(migrated);
            string backupA = PlayerPrefs.GetString(slotA, "");
            string backupB = PlayerPrefs.GetString(slotB, "");
            int backupMigration = PlayerPrefs.GetInt(migrated, 0);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var profileField = typeof(PlayerProgress).GetField("_profile", flags);
            var revisionField = typeof(PlayerProgress).GetField("_revision", flags);
            var previousProfile = profileField.GetValue(null);
            var previousRevision = revisionField.GetValue(null);
            try
            {
                profileField.SetValue(null, new PlayerProfile());
                revisionField.SetValue(null, 0L);
                PlayerPrefs.SetInt(migrated, 1);

                PlayerProgress.MarkAttemptStarted("test-win", 1);
                Assert.That(PlayerProgress.RecordWin("test-win", 1, Difficulty.EASY,
                    3, 12, 1, 0, 0, 0, 0), Is.True);
                Assert.That(PlayerProgress.RecordWin("test-win", 1, Difficulty.EASY,
                    3, 12, 1, 0, 0, 0, 0), Is.False);
                Assert.That(PlayerProgress.Data.HighestCompleted, Is.EqualTo(1));
                Assert.That(PlayerProgress.GetLevelStars(1), Is.EqualTo(3));
                Assert.That(PlayerProgress.Data.Stars, Is.EqualTo(1));
                Assert.That(PlayerProgress.Data.PendingAttemptId, Is.Null);

                PlayerProgress.AddBooster("rocket", 1);
                Assert.That(PlayerProgress.ToggleStartingBooster("rocket"), Is.True);
                var specials = PlayerProgress.ConsumeStartingBoosters();
                Assert.That(specials, Is.EquivalentTo(new[] { SpecialType.ROCKET_HORIZONTAL }));
                Assert.That(PlayerProgress.GetBoosterCount("rocket"), Is.Zero);

                PlayerProgress.MarkAttemptStarted("test-loss", 2);
                Assert.That(PlayerProgress.RecordLoss("test-loss", 2), Is.True);
                Assert.That(PlayerProgress.RecordLoss("test-loss", 2), Is.False);
                Assert.That(PlayerProgress.Lives, Is.EqualTo(4));
                Assert.That(PlayerProgress.Data.WinStreak, Is.Zero);
            }
            finally
            {
                if (hadA) PlayerPrefs.SetString(slotA, backupA); else PlayerPrefs.DeleteKey(slotA);
                if (hadB) PlayerPrefs.SetString(slotB, backupB); else PlayerPrefs.DeleteKey(slotB);
                if (hadMigration) PlayerPrefs.SetInt(migrated, backupMigration);
                else PlayerPrefs.DeleteKey(migrated);
                PlayerPrefs.Save();
                profileField.SetValue(null, previousProfile);
                revisionField.SetValue(null, previousRevision);
            }
        }
    }
}
