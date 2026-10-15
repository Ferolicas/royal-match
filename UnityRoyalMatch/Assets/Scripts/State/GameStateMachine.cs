// GameStateMachine.cs — Replaces the React/Zustand store with Unity-native state management
// Manages game state, level loading, and transitions between game screens

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using RoyalMatch.Core;
using RoyalMatch.Levels;
using RoyalMatch.Types;
using RoyalMatch.Match3;
using RoyalMatch.Progress;

namespace RoyalMatch.State
{
    [Serializable]
    public class GameStateChangedArgs : EventArgs
    {
        public GameStatus Status;
        public int Score;
        public int MovesRemaining;
        public int LevelNumber;
    }

    public class GameStateMachine : MonoBehaviour
    {
        [Header("Debug")]
        public bool EnableLogging = false;

        // Events
        public event EventHandler<GameStateChangedArgs> OnStateChanged;
        public event EventHandler OnLevelLoaded;
        public event EventHandler<string> OnLevelFailed;
        public event Action OnGameStarted;
        public event Action OnGameEnded;

        // Properties
        public GameStateData CurrentState { get; private set; }
        public GameManager GameManagerInstance { get; private set; }
        public LevelLoader LevelLoader { get; private set; } = new LevelLoader();
        public bool IsLevelLoading => _isLevelLoading;

        private bool _isLevelLoading = false;
        private List<AnimationStep> _animationSteps = new List<AnimationStep>();

        private string _attemptId;
        private bool _outcomeRecorded;

        #region Unity Lifecycle

        private void Awake()
        {
            if (FindAnyObjectByType<GameStateMachine>() != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Level Management

        public async Task LoadLevelAsync(int levelNumber)
        {
            if (_isLevelLoading) return;
            _isLevelLoading = true;

            try
            {
                if (!PlayerProgress.CanStart(levelNumber))
                {
                    OnLevelFailed?.Invoke(this, $"Level {levelNumber} is locked or no lives remain");
                    return;
                }

                var levelData = await LevelLoader.LoadLevel(levelNumber);
                var state = GameState.Create(levelData);
                if (levelNumber >= 32)
                {
                    int streak = PlayerProgress.Data.WinStreak;
                    if (streak >= 1)
                    {
                        state.StarterSpecials.Add(SpecialType.ROCKET_HORIZONTAL);
                        state.StarterSpecials.Add(SpecialType.PROPELLER);
                    }
                    if (streak >= 2) state.StarterSpecials.Add(SpecialType.BOMB);
                    if (streak >= 3) state.StarterSpecials.Add(SpecialType.COLOR_BOMB);
                }
                state.SuperLightBallActive = levelNumber >= 292 &&
                    PlayerProgress.Data.SuperLightBallCharge >= 10;
                state.StarterSpecials.AddRange(PlayerProgress.ConsumeStartingBoosters());
                GameManagerInstance = new GameManager(state);
                CurrentState = state;
                _attemptId = Guid.NewGuid().ToString("N");
                _outcomeRecorded = false;
                _animationSteps.Clear();

                OnLevelLoaded?.Invoke(this, EventArgs.Empty);
                if (EnableLogging) Debug.Log($"Level {levelNumber} loaded. Moves: {state.MovesRemaining}, Stars: {state.Star1}/{state.Star2}/{state.Star3}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load level {levelNumber}: {ex.Message}");
                OnLevelFailed?.Invoke(this, ex.Message);
            }
            finally
            {
                _isLevelLoading = false;
            }
        }

        public void LoadLevel(int levelNumber)
        {
            // Fire and forget - the OnLevelLoaded event will handle the rest
            _ = LoadLevelAsync(levelNumber);
        }

        #endregion

        #region Game Flow

        public void StartGame()
        {
            if (GameManagerInstance == null || CurrentState == null) return;

            CurrentState.Status = GameStatus.PLAYING;
            CurrentState.Status = GameStatus.WAITING_INPUT;
            OnGameStarted?.Invoke();

            EmitStateChanged();
        }

        public bool TrySwap(int r1, int c1, int r2, int c2)
        {
            if (GameManagerInstance == null || CurrentState.Status != GameStatus.WAITING_INPUT) return false;

            bool result = GameManagerInstance.TrySwap(r1, c1, r2, c2);
            if (result)
            {
                PlayerProgress.MarkAttemptStarted(_attemptId, CurrentState.LevelNumber);
                _animationSteps = new List<AnimationStep>(GameManagerInstance.GetAnimationSteps());
                SyncState();
            }
            return result;
        }

        public bool TryUseBooster(string id, int row, int col)
        {
            if (GameManagerInstance == null || CurrentState?.Status != GameStatus.WAITING_INPUT ||
                PlayerProgress.GetBoosterCount(id) <= 0) return false;
            if (!GameManagerInstance.TryUseBooster(id, row, col)) return false;
            if (!PlayerProgress.TryUseBooster(id)) return false;
            PlayerProgress.MarkAttemptStarted(_attemptId, CurrentState.LevelNumber);
            _animationSteps = new List<AnimationStep>(GameManagerInstance.GetAnimationSteps());
            SyncState();
            return true;
        }

        public void EndTurn()
        {
            // Turn is already resolved by TrySwap in this architecture
            // The animation steps are used to drive visual feedback
        }

        public void RestartLevel()
        {
            if (CurrentState != null)
            {
                var levelNumber = CurrentState.LevelNumber;
                LoadLevel(levelNumber);
            }
        }

        public void NextLevel()
        {
            if (CurrentState != null && CurrentState.LevelNumber < DifficultySystem.TotalLevels)
            {
                LoadLevel(CurrentState.LevelNumber + 1);
            }
        }

        #endregion

        #region State Helpers

        private void SyncState()
        {
            if (GameManagerInstance == null) return;
            CurrentState = GameManagerInstance.GetState();
            EmitStateChanged();
        }

        private void EmitStateChanged()
        {
            OnStateChanged?.Invoke(this, new GameStateChangedArgs
            {
                Status = CurrentState.Status,
                Score = CurrentState.Score,
                MovesRemaining = CurrentState.MovesRemaining,
                LevelNumber = CurrentState.LevelNumber,
            });
        }

        #endregion

        #region Player Progress

        public int GetLevelStars(int levelNumber) => PlayerProgress.GetLevelStars(levelNumber);
        public bool IsLevelUnlocked(int levelNumber) => PlayerProgress.IsUnlocked(levelNumber);

        public void CommitOutcome()
        {
            if (_outcomeRecorded || CurrentState == null) return;
            if (CurrentState.Status == GameStatus.WIN)
            {
                _outcomeRecorded = PlayerProgress.RecordWin(_attemptId, CurrentState.LevelNumber,
                    CurrentState.Difficulty, GameState.GetStarsEarned(CurrentState), CurrentState.MovesRemaining,
                    CurrentState.RocketsCreated, CurrentState.TntCreated, CurrentState.PropellersCreated,
                    CurrentState.LightballsCreated, CurrentState.CombosMade);
            }
            else if (CurrentState.Status == GameStatus.LOSE)
            {
                _outcomeRecorded = PlayerProgress.RecordLoss(_attemptId, CurrentState.LevelNumber);
            }
        }

        public void CommitAbandonment()
        {
            if (_outcomeRecorded || CurrentState == null ||
                CurrentState.Status == GameStatus.WIN) return;
            if (CurrentState.Status == GameStatus.LOSE ||
                CurrentState.MovesRemaining < CurrentState.MoveLimit)
            {
                _outcomeRecorded = PlayerProgress.RecordLoss(_attemptId, CurrentState.LevelNumber);
            }
        }

        private void OnApplicationQuit()
        {
            if (CurrentState?.Status == GameStatus.LOSE) CommitOutcome();
        }

        public bool TryBuyExtraMoves(int cost = 900, int moves = 5)
        {
            if (CurrentState == null || CurrentState.Status != GameStatus.LOSE ||
                _outcomeRecorded || !PlayerProgress.TrySpendCoins(cost)) return false;
            CurrentState.MovesRemaining += moves;
            CurrentState.MoveLimit += moves;
            CurrentState.Status = GameStatus.WAITING_INPUT;
            _animationSteps.Clear();
            EmitStateChanged();
            return true;
        }

        #endregion

        #region Animation Access

        public List<AnimationStep> GetAnimationSteps()
        {
            return _animationSteps;
        }

        #endregion

        #region Public API for UI

        public int GetStarsEarned()
        {
            if (CurrentState == null) return 0;
            return GameState.GetStarsEarned(CurrentState);
        }

        public bool IsWin => CurrentState?.Status == GameStatus.WIN;
        public bool IsLose => CurrentState?.Status == GameStatus.LOSE;

        #endregion
    }
}
