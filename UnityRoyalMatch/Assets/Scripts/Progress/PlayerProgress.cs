using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RoyalMatch.Levels;
using RoyalMatch.Types;
using UnityEngine;

namespace RoyalMatch.Progress
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int Schema = 2;
        public int HighestCompleted;
        public int Coins = 500;
        public int Stars;
        public int Lives = 5;
        public long NextLifeUtcTicks;
        public long UnlimitedLivesUntilUtcTicks;
        public int WinStreak;
        public int SuperLightBallCharge;
        public int AreaId = 1;
        public int AreaTask;
        public int PassSeason;
        public int PassKeys;
        public int FreePassClaimed;
        public int PremiumPassClaimed;
        public bool PremiumPassOwned;
        public int WeeklySeason;
        public Dictionary<string, int> WeeklyCounters = new Dictionary<string, int>();
        public HashSet<string> ClaimedMissions = new HashSet<string>();
        public Dictionary<string, int> Boosters = new Dictionary<string, int>();
        public HashSet<string> EquippedStartingBoosters = new HashSet<string>();
        public Dictionary<int, int> BestStars = new Dictionary<int, int>();
        public HashSet<string> ProcessedAttempts = new HashSet<string>();
        public HashSet<string> ProcessedPurchases = new HashSet<string>();
        public HashSet<int> FailedLevels = new HashSet<int>();
        public string PendingAttemptId;
        public int PendingAttemptLevel;
    }

    [Serializable]
    internal sealed class ProfileEnvelope
    {
        public long Revision;
        public string Payload;
        public string Checksum;
    }

    public sealed class WeeklyMission
    {
        public readonly string Id;
        public readonly string Label;
        public readonly int Target;
        public readonly int RewardCoins;

        public WeeklyMission(string id, string label, int target, int rewardCoins)
        {
            Id = id; Label = label; Target = target; RewardCoins = rewardCoins;
        }
    }

    public static class PlayerProgress
    {
        private const string SlotA = "mil_gemas_profile_a";
        private const string SlotB = "mil_gemas_profile_b";
        private const string LegacyMigrationKey = "mil_gemas_profile_migrated";
        private static readonly long LifeInterval = TimeSpan.FromMinutes(30).Ticks;
        private static readonly JsonSerializerSettings JsonOptions = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };
        private static PlayerProfile _profile;
        private static long _revision;
        public static event Action Changed;

        public static readonly WeeklyMission[] WeeklyMissions =
        {
            new WeeklyMission("wins", "Supera niveles", 5, 100),
            new WeeklyMission("first_try", "Gana al primer intento", 3, 120),
            new WeeklyMission("hard", "Supera niveles difíciles", 2, 150),
            new WeeklyMission("super_hard", "Supera un superdifícil", 1, 200),
            new WeeklyMission("coins", "Consigue monedas", 100, 100),
            new WeeklyMission("rockets", "Crea cohetes", 10, 100),
            new WeeklyMission("tnt", "Crea TNT", 5, 120),
            new WeeklyMission("propellers", "Crea hélices", 10, 100),
            new WeeklyMission("lightballs", "Crea bolas de luz", 3, 160),
            new WeeklyMission("combos", "Combina potenciadores", 5, 180),
        };

        public static PlayerProfile Data
        {
            get { EnsureLoaded(); RefreshTime(); return _profile; }
        }

        public static int CurrentLevel => Math.Min(DifficultySystem.TotalLevels, Data.HighestCompleted + 1);
        public static int MaxLives => Data.PremiumPassOwned ? 8 : 5;
        public static int Lives => Data.UnlimitedLivesUntilUtcTicks > DateTime.UtcNow.Ticks ? MaxLives : Data.Lives;
        public static bool IsUnlimitedLives => Data.UnlimitedLivesUntilUtcTicks > DateTime.UtcNow.Ticks;

        public static bool IsUnlocked(int level) => level >= 1 && level <= DifficultySystem.TotalLevels &&
            level <= Data.HighestCompleted + 1;

        public static int GetLevelStars(int level)
        {
            if (level < 1 || level > Data.HighestCompleted) return 0;
            return Data.BestStars.TryGetValue(level, out var stars) ? stars : 1;
        }

        public static bool CanStart(int level) => IsUnlocked(level) && Lives > 0;

        public static void MarkAttemptStarted(string attemptId, int level)
        {
            if (string.IsNullOrEmpty(attemptId) || Data.PendingAttemptId == attemptId) return;
            Data.PendingAttemptId = attemptId;
            Data.PendingAttemptLevel = level;
            Save();
        }

        public static bool RecordWin(string attemptId, int level, Difficulty difficulty, int rating,
            int movesLeft, int rockets, int tnt, int propellers, int lightballs, int combos)
        {
            if (string.IsNullOrEmpty(attemptId) || !IsUnlocked(level) || !Data.ProcessedAttempts.Add(attemptId))
                return false;

            bool firstClear = level > Data.HighestCompleted;
            if (firstClear)
            {
                Data.HighestCompleted = level;
                Data.Stars += 1;
                int earnedCoins = RewardCoins(difficulty, level);
                Data.Coins += earnedCoins;
                Data.PassKeys += difficulty == Difficulty.SUPER_HARD ? 3 : difficulty == Difficulty.HARD ? 2 : 1;
                Increment("wins", 1);
                Increment("coins", earnedCoins);
                if (difficulty == Difficulty.HARD) Increment("hard", 1);
                if (difficulty == Difficulty.SUPER_HARD) Increment("super_hard", 1);
                if (!Data.FailedLevels.Contains(level)) Increment("first_try", 1);
                Increment("rockets", rockets);
                Increment("tnt", tnt);
                Increment("propellers", propellers);
                Increment("lightballs", lightballs);
                Increment("combos", combos);
            }
            Data.BestStars[level] = Math.Max(GetLevelStars(level), Math.Clamp(rating, 1, 3));
            ClearPendingAttempt(attemptId);
            Data.WinStreak = Math.Min(3, Data.WinStreak + 1);
            Data.SuperLightBallCharge = Math.Min(10, Data.SuperLightBallCharge + 1);
            ClaimAvailablePassRewards();
            Save();
            return true;
        }

        public static bool RecordLoss(string attemptId, int level)
        {
            if (string.IsNullOrEmpty(attemptId) || !Data.ProcessedAttempts.Add(attemptId)) return false;
            if (!IsUnlimitedLives && Data.Lives > 0)
            {
                if (Data.Lives == MaxLives) Data.NextLifeUtcTicks = DateTime.UtcNow.Ticks + LifeInterval;
                Data.Lives--;
            }
            Data.WinStreak = 0;
            Data.SuperLightBallCharge = 0;
            Data.FailedLevels.Add(level);
            ClearPendingAttempt(attemptId);
            Save();
            return true;
        }

        private static void ClearPendingAttempt(string attemptId)
        {
            if (Data.PendingAttemptId != attemptId) return;
            Data.PendingAttemptId = null;
            Data.PendingAttemptLevel = 0;
        }

        public static bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || Data.Coins < amount) return false;
            Data.Coins -= amount;
            Save();
            return true;
        }

        public static bool TryRefillLives(int cost = 900)
        {
            if (cost <= 0 || Lives >= MaxLives || Data.Coins < cost) return false;
            Data.Coins -= cost;
            Data.Lives = MaxLives;
            Data.NextLifeUtcTicks = 0;
            Save();
            return true;
        }

        public static int SecondsUntilNextLife
        {
            get
            {
                if (Lives >= MaxLives || Data.NextLifeUtcTicks <= 0) return 0;
                return (int)Math.Max(0, Math.Ceiling((Data.NextLifeUtcTicks - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond));
            }
        }

        public static bool ClaimAreaTask()
        {
            int cost = 1 + (Data.AreaTask % 5 == 4 ? 1 : 0);
            if (Data.Stars < cost) return false;
            Data.Stars -= cost;
            Data.AreaTask++;
            if (Data.AreaTask >= 20)
            {
                Data.AreaTask = 0;
                Data.AreaId++;
                Data.Coins += 150;
            }
            Save();
            return true;
        }

        public static int GetMissionProgress(string id) => Data.WeeklyCounters.TryGetValue(id, out var value) ? value : 0;
        public static bool IsMissionClaimed(string id) => Data.ClaimedMissions.Contains(id);

        public static bool ClaimMission(string id)
        {
            foreach (var mission in WeeklyMissions)
            {
                if (mission.Id != id || GetMissionProgress(id) < mission.Target ||
                    !Data.ClaimedMissions.Add(id)) continue;
                Data.Coins += mission.RewardCoins;
                Save();
                return true;
            }
            return false;
        }

        public static int GetBoosterCount(string id) => Data.Boosters.TryGetValue(id, out var count) ? count : 0;

        public static bool IsStartingBoosterEquipped(string id) => Data.EquippedStartingBoosters.Contains(id);

        public static bool ToggleStartingBooster(string id)
        {
            if (id != "rocket" && id != "tnt" && id != "lightball") return false;
            if (!Data.EquippedStartingBoosters.Remove(id))
            {
                if (GetBoosterCount(id) <= 0) return false;
                Data.EquippedStartingBoosters.Add(id);
            }
            Save();
            return true;
        }

        public static List<SpecialType> ConsumeStartingBoosters()
        {
            var specials = new List<SpecialType>();
            bool hadSelection = Data.EquippedStartingBoosters.Count > 0;
            foreach (var id in new[] { "rocket", "tnt", "lightball" })
            {
                if (!Data.EquippedStartingBoosters.Contains(id) || GetBoosterCount(id) <= 0) continue;
                Data.Boosters[id]--;
                specials.Add(id == "rocket" ? SpecialType.ROCKET_HORIZONTAL :
                    id == "tnt" ? SpecialType.BOMB : SpecialType.COLOR_BOMB);
            }
            Data.EquippedStartingBoosters.Clear();
            if (hadSelection) Save();
            return specials;
        }

        public static bool TryBuyBooster(string id, int cost)
        {
            if (!IsBooster(id) || cost <= 0 || Data.Coins < cost) return false;
            Data.Coins -= cost;
            Data.Boosters[id] = GetBoosterCount(id) + 1;
            Save();
            return true;
        }

        public static bool TryUseBooster(string id)
        {
            if (!IsBooster(id) || GetBoosterCount(id) <= 0) return false;
            Data.Boosters[id]--;
            Save();
            return true;
        }

        private static bool IsBooster(string id) => id == "hammer" || id == "arrow" ||
            id == "cannon" || id == "jester" || id == "rocket" || id == "tnt" || id == "lightball";

        public static void AddBooster(string id, int amount)
        {
            if (!IsBooster(id) || amount <= 0) return;
            Data.Boosters[id] = GetBoosterCount(id) + amount;
            Save();
        }

        public static int RewardCoins(Difficulty difficulty, int level) =>
            (difficulty == Difficulty.SUPER_HARD ? 40 : difficulty == Difficulty.HARD ? 25 : 15) +
            (level % 100 == 0 ? 85 : 0);

        public static bool GrantVerifiedPurchase(string transactionId, string productId)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || !Data.ProcessedPurchases.Add(transactionId)) return false;
            switch (productId)
            {
                case "milgemas.coins.small": Data.Coins += 1000; break;
                case "milgemas.coins.medium": Data.Coins += 5500; break;
                case "milgemas.coins.large": Data.Coins += 12000; break;
                case "milgemas.pass.monthly":
                    Data.PremiumPassOwned = true;
                    Data.Lives = 8;
                    Data.NextLifeUtcTicks = 0;
                    break;
                default: Data.ProcessedPurchases.Remove(transactionId); return false;
            }
            ClaimAvailablePassRewards();
            Save();
            return true;
        }

        private static void Increment(string id, int value)
        {
            Data.WeeklyCounters[id] = GetMissionProgress(id) + value;
        }

        private static void ClaimAvailablePassRewards()
        {
            int unlocked = Math.Min(30, Data.PassKeys / 5);
            while (Data.FreePassClaimed < unlocked)
            {
                Data.FreePassClaimed++;
                Data.Coins += 50;
                if (Data.FreePassClaimed % 5 == 0) Data.Boosters["hammer"] = GetBoosterCount("hammer") + 1;
            }
            if (Data.PremiumPassOwned)
                while (Data.PremiumPassClaimed < unlocked)
                {
                    Data.PremiumPassClaimed++;
                    Data.Coins += 100;
                    if (Data.PremiumPassClaimed % 5 == 0) Data.Boosters["lightball"] = GetBoosterCount("lightball") + 1;
                }
        }

        private static void RefreshTime()
        {
            if (_profile == null) return;
            bool changed = false;
            long now = DateTime.UtcNow.Ticks;
            int max = _profile.PremiumPassOwned ? 8 : 5;
            if (_profile.Lives < max && _profile.NextLifeUtcTicks > 0 && now >= _profile.NextLifeUtcTicks)
            {
                long restored = 1 + (now - _profile.NextLifeUtcTicks) / LifeInterval;
                _profile.Lives = (int)Math.Min(max, _profile.Lives + restored);
                _profile.NextLifeUtcTicks = _profile.Lives >= max ? 0 :
                    _profile.NextLifeUtcTicks + restored * LifeInterval;
                changed = true;
            }
            int season = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
            if (_profile.PassSeason != season)
            {
                _profile.PassSeason = season;
                _profile.PassKeys = 0;
                _profile.FreePassClaimed = 0;
                _profile.PremiumPassClaimed = 0;
                _profile.PremiumPassOwned = false;
                _profile.Lives = Math.Min(5, _profile.Lives);
                changed = true;
            }
            var today = DateTime.UtcNow.Date;
            int week = (int)(today.AddDays(-((int)today.DayOfWeek + 6) % 7).Ticks / TimeSpan.TicksPerDay);
            if (_profile.WeeklySeason != week)
            {
                _profile.WeeklySeason = week;
                _profile.WeeklyCounters.Clear();
                _profile.ClaimedMissions.Clear();
                changed = true;
            }
            if (changed) Save();
        }

        private static void EnsureLoaded()
        {
            if (_profile != null) return;
            var a = ReadSlot(SlotA);
            var b = ReadSlot(SlotB);
            var selected = a == null ? b : b == null || a.Revision >= b.Revision ? a : b;
            _revision = selected?.Revision ?? 0;
            _profile = selected == null ? new PlayerProfile() :
                JsonConvert.DeserializeObject<PlayerProfile>(selected.Payload, JsonOptions);
            if (_profile == null) _profile = new PlayerProfile();
            _profile.WeeklyCounters ??= new Dictionary<string, int>();
            _profile.ClaimedMissions ??= new HashSet<string>();
            _profile.Boosters ??= new Dictionary<string, int>();
            _profile.EquippedStartingBoosters ??= new HashSet<string>();
            _profile.BestStars ??= new Dictionary<int, int>();
            _profile.ProcessedAttempts ??= new HashSet<string>();
            _profile.ProcessedPurchases ??= new HashSet<string>();
            _profile.FailedLevels ??= new HashSet<int>();
            if (PlayerPrefs.GetInt(LegacyMigrationKey, 0) == 0)
            {
                for (int level = 1; level <= 1000; level++)
                {
                    int stars = PlayerPrefs.GetInt($"level_{level}_stars", 0);
                    if (stars <= 0) break;
                    _profile.BestStars[level] = Math.Max(1, stars);
                    _profile.HighestCompleted = level;
                }
                _profile.Stars = Math.Max(_profile.Stars, _profile.HighestCompleted);
                PlayerPrefs.SetInt(LegacyMigrationKey, 1);
                Save();
            }
            // The board is not serialized. An interrupted attempt that has made a
            // move counts as a loss on the next launch, exactly once.
            if (!string.IsNullOrEmpty(_profile.PendingAttemptId))
            {
                string interrupted = _profile.PendingAttemptId;
                int level = _profile.PendingAttemptLevel;
                _profile.PendingAttemptId = null;
                _profile.PendingAttemptLevel = 0;
                if (_profile.ProcessedAttempts.Add(interrupted))
                {
                    if (_profile.UnlimitedLivesUntilUtcTicks <= DateTime.UtcNow.Ticks && _profile.Lives > 0)
                    {
                        if (_profile.Lives == (_profile.PremiumPassOwned ? 8 : 5))
                            _profile.NextLifeUtcTicks = DateTime.UtcNow.Ticks + LifeInterval;
                        _profile.Lives--;
                    }
                    _profile.WinStreak = 0;
                    _profile.SuperLightBallCharge = 0;
                    _profile.FailedLevels.Add(level);
                }
                Save();
            }
            RefreshTime();
        }

        private static ProfileEnvelope ReadSlot(string key)
        {
            try
            {
                string raw = PlayerPrefs.GetString(key, "");
                if (string.IsNullOrEmpty(raw)) return null;
                var envelope = JsonConvert.DeserializeObject<ProfileEnvelope>(raw);
                if (envelope == null || string.IsNullOrEmpty(envelope.Payload) ||
                    envelope.Checksum != Hash(envelope.Revision + ":" + envelope.Payload)) return null;
                return envelope;
            }
            catch { return null; }
        }

        private static string Hash(string text)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        private static void Save()
        {
            if (_profile == null) return;
            _revision++;
            var payload = JsonConvert.SerializeObject(_profile, JsonOptions);
            var envelope = new ProfileEnvelope
            {
                Revision = _revision,
                Payload = payload,
                Checksum = Hash(_revision + ":" + payload),
            };
            PlayerPrefs.SetString(_revision % 2 == 0 ? SlotA : SlotB,
                JsonConvert.SerializeObject(envelope, JsonOptions));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
