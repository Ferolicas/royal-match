// GameController.cs — Main MonoBehaviour that ties together game logic, rendering, input, and UI
// Acts as the central orchestrator for a single gameplay session

using System.Collections;
using UnityEngine;
using RoyalMatch.Core;
using RoyalMatch.Rendering;
using RoyalMatch.State;
using RoyalMatch.Types;
using RoyalMatch.Match3;
using RoyalMatch.Levels;
using RoyalMatch.UI;
using RoyalMatch.Audio;

namespace RoyalMatch.Core
{
    public class GameController : MonoBehaviour
    {
        [Header("Rendering")]
        public BoardView BoardView;
        public AnimationPlayer AnimationPlayer;
        public VFXManager VFXManager;

        [Header("UI References")]
        public HUD Hud;
        public GameOverPanel GameOverPanel;
        public CharacterReactionView Character;

        [Header("Input")]
        public float DragThreshold = 25f;
        public LayerMask BoardLayerMask = 1;

        // Runtime state
        private GameStateMachine _stateMachine;
        private GameRenderer _renderer;
        private InputHandler _inputHandler;
        private bool _isAnimating = false;
        private bool _isGameStateAnimating = false;
        private bool _isLevelLoaded = false;
        private Point? _selectedCell = null;
        private Vector2 _dragStartScreen;
        private bool _isDragging = false;
        private Point? _dragOrigin;
        private GemView _dragView;
        private Vector3 _dragViewOrigin;

        #region Unity Lifecycle

        private void Awake()
        {
            // Find or create required components
            if (BoardView == null)
                BoardView = FindFirstObjectByType<BoardView>();
            if (AnimationPlayer == null)
                AnimationPlayer = FindFirstObjectByType<AnimationPlayer>();
            if (VFXManager == null)
                VFXManager = FindFirstObjectByType<VFXManager>();
            if (Hud == null)
                Hud = FindFirstObjectByType<HUD>(FindObjectsInactive.Include);
            if (GameOverPanel == null)
                GameOverPanel = FindFirstObjectByType<GameOverPanel>(FindObjectsInactive.Include);
            if (Character == null)
                Character = FindFirstObjectByType<CharacterReactionView>(FindObjectsInactive.Include);
            if (AnimationPlayer != null)
                AnimationPlayer.OnMatch += HandleMatchReaction;

            _inputHandler = new InputHandler();
            _renderer = new GameRenderer(BoardView);

            _stateMachine = FindFirstObjectByType<GameStateMachine>();
            if (_stateMachine == null)
            {
                var go = new GameObject("GameStateMachine");
                _stateMachine = go.AddComponent<GameStateMachine>();
            }

            _stateMachine.OnLevelLoaded += OnLevelLoaded;
            _stateMachine.OnStateChanged += OnGameStateChanged;

            // Subscribe to UI requests in Start() to ensure GameSceneManager.Instance is set
        }

        private void Start()
        {
            Debug.Log("GameController.Start: Initializing...");
            // Wait for GameSceneManager to be ready
            var sceneMgr = GameSceneManager.Instance;
            if (sceneMgr != null)
            {
                Debug.Log("GameController.Start: GameSceneManager found, subscribing to level start.");
                sceneMgr.OnLevelStartRequested += HandleLevelStartRequested;
            }
            else
            {
                Debug.LogWarning("GameController.Start: GameSceneManager.Instance is NULL!");
            }
        }

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnLevelLoaded -= OnLevelLoaded;
                _stateMachine.OnStateChanged -= OnGameStateChanged;
            }
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.OnLevelStartRequested -= HandleLevelStartRequested;
            if (AnimationPlayer != null)
                AnimationPlayer.OnMatch -= HandleMatchReaction;
        }

        #endregion

        #region Level Management

        private void HandleLevelStartRequested(int levelNumber)
        {
            LoadLevel(levelNumber);
        }

        public void LoadLevel(int levelNumber)
        {
            if (Hud != null) Hud.ShowLoading(true);
            if (GameOverPanel != null) GameOverPanel.Hide();
            VFXManager?.Clear();
            Character?.ResetForLevel();
            _isAnimating = false;
            _isGameStateAnimating = false;
            _isLevelLoaded = false;
            _stateMachine.LoadLevel(levelNumber);
        }

        private void OnLevelLoaded(object sender, System.EventArgs e)
        {
            var state = _stateMachine.CurrentState;
            if (state == null) return;

            BoardView.Initialize(state.GridRows, state.GridCols);
            _renderer = new GameRenderer(BoardView);

            // Initial render
            var engineState = _renderer.Render(state, _stateMachine.GameManagerInstance.GetBoard());
            BoardView.Render(engineState);

            // Update HUD
            if (Hud != null)
            {
                Hud.UpdateDisplay(state);
                Hud.ShowLoading(false);
            }

            // Reset input handler
            _inputHandler.Reset();
            _selectedCell = null;
            _isLevelLoaded = true;

            // Start the game
            _stateMachine.StartGame();
        }

        public void RestartCurrentLevel()
        {
            if (_stateMachine.CurrentState != null)
            {
                _stateMachine.RestartLevel();
            }
        }

        #endregion

        #region Game State Updates

        private void OnGameStateChanged(object sender, GameStateChangedArgs args)
        {
            if (Hud != null)
                Hud.UpdateDisplay(_stateMachine.CurrentState);
            Character?.SetMoves(_stateMachine.CurrentState.MovesRemaining);

            if (_stateMachine.GameManagerInstance == null) return;

            var steps = _stateMachine.GetAnimationSteps();
            if (steps.Count > 0 && AnimationPlayer != null)
            {
                // The model has already resolved every cascade. Keep the current
                // visual board alive until its timed commands have played.
                _isGameStateAnimating = true;
                AnimationPlayer.PlaySteps(steps, () =>
                {
                    _isGameStateAnimating = false;
                    RenderResolvedBoard();
                    ShowOutcome(args);
                });
            }
            else
            {
                RenderResolvedBoard();
                ShowOutcome(args);
            }
        }

        private void ShowOutcome(GameStateChangedArgs args)
        {
            if (args.Status == GameStatus.WIN)
            {
                var stars = GameState.GetStarsEarned(_stateMachine.CurrentState);
                _stateMachine.SetLevelStars(args.LevelNumber, stars);
                if (GameOverPanel != null)
                    GameOverPanel.ShowWin(stars, args.LevelNumber);
                Character?.CelebrateVictory();
                VFXManager?.Emit(VFXType.LevelComplete, Vector3.zero);
                GameSceneManager.Instance?.ShowGameOver();
                GameAudioManager.Instance?.PlayLevelComplete();
                _isAnimating = false;
                _isGameStateAnimating = false;
            }
            else if (args.Status == GameStatus.LOSE)
            {
                Character?.ShowDefeat();
                if (GameOverPanel != null)
                    GameOverPanel.ShowLose(args.LevelNumber);
                GameSceneManager.Instance?.ShowGameOver();
                _isAnimating = false;
                _isGameStateAnimating = false;
            }
        }

        private void RenderResolvedBoard()
        {
            if (_stateMachine.GameManagerInstance == null) return;
            var engineState = _renderer.Render(_stateMachine.CurrentState,
                _stateMachine.GameManagerInstance.GetBoard());
            BoardView.Render(engineState);
        }

        private void HandleMatchReaction(int matchedCount)
        {
            Character?.CelebrateMatch(matchedCount);
        }

        #endregion

        #region Input Handling

        private void Update()
        {
            if (_isAnimating || _isGameStateAnimating || _stateMachine.CurrentState == null ||
                _stateMachine.CurrentState.Status != GameStatus.WAITING_INPUT ||
                !_isLevelLoaded)
                return;

            HandleInput();
        }

        private void HandleInput()
        {
            // Determine input source
            bool mouseDown = Input.GetMouseButtonDown(0);
            bool mouseUp = Input.GetMouseButtonUp(0);
            bool mouseDrag = Input.GetMouseButton(0);

            // Touch support
            Touch? touch = null;
            if (Input.touchCount > 0)
            {
                touch = Input.GetTouch(0);
                mouseDown = touch.Value.phase == TouchPhase.Began;
                mouseUp = touch.Value.phase == TouchPhase.Ended || touch.Value.phase == TouchPhase.Canceled;
                mouseDrag = touch.Value.phase == TouchPhase.Moved || touch.Value.phase == TouchPhase.Stationary;
            }

            Vector2 screenPos = touch.HasValue ? (Vector2)touch.Value.position : (Vector2)Input.mousePosition;

            if (mouseDown)
            {
                Ray ray = Camera.main.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, BoardLayerMask))
                {
                    Vector2 boardPos = BoardView.WorldToBoard(hit.point);
                    int row = Mathf.RoundToInt(boardPos.x);
                    int col = Mathf.RoundToInt(boardPos.y);

                    if (row >= 0 && row < BoardView.GridRows && col >= 0 && col < BoardView.GridCols)
                    {
                        _dragStartScreen = screenPos;
                        _isDragging = true;
                        _dragOrigin = new Point(row, col);

                        var action = _inputHandler.HandlePointerDown(row, col);
                        if (action != null && action.Valid)
                        {
                            string cellId = BoardView.GetCellId(row, col);
                            if (BoardView.TryGetGemView(cellId, out _dragView) && _dragView != null)
                            {
                                _dragViewOrigin = BoardView.BoardToWorld(row, col);
                                _dragView.PopScale(1.08f, 0.08f);
                            }
                        }
                    }
                }
                else
                {
                    ClearSelection();
                }
            }

            if (_isDragging && mouseDrag)
            {
                Vector2 dragDelta = (Vector2)(screenPos - _dragStartScreen);
                if (_dragView != null)
                {
                    Vector2 normalized = Vector2.ClampMagnitude(dragDelta / Mathf.Max(DragThreshold * 2.2f, 1f), 1f);
                    _dragView.transform.position = _dragViewOrigin +
                        new Vector3(normalized.x, 0.18f, normalized.y) * (BoardView.CellSize * 0.42f);
                }

                if (dragDelta.magnitude > DragThreshold)
                {
                    if (_dragOrigin.HasValue)
                    {
                        Point from = _dragOrigin.Value;
                        int targetRow = from.Row;
                        int targetCol = from.Col;
                        if (Mathf.Abs(dragDelta.x) >= Mathf.Abs(dragDelta.y))
                            targetCol += dragDelta.x > 0f ? 1 : -1;
                        else
                            targetRow += dragDelta.y > 0f ? 1 : -1;

                        if (targetRow >= 0 && targetRow < BoardView.GridRows &&
                            targetCol >= 0 && targetCol < BoardView.GridCols)
                        {
                            StartCoroutine(ExecuteSwapAnimation(from, new Point(targetRow, targetCol)));
                            _isDragging = false;
                            _dragOrigin = null;
                            _dragView = null;
                        }
                        else
                            CancelDragVisual();
                    }
                }
            }

            if (mouseUp && _isDragging)
            {
                // Tap-to-select is intentionally disabled: gameplay is a direct
                // swipe on both mouse and touch.
                CancelDragVisual();
            }
        }

        private void CancelDragVisual()
        {
            if (_dragView != null)
                _dragView.MoveTo(_dragViewOrigin, 0.1f);
            _dragView = null;
            _dragOrigin = null;
            _isDragging = false;
            ClearSelection();
        }

        private void HighlightGem(int row, int col)
        {
            string cellId = BoardView.GetCellId(row, col);
            if (BoardView.HasGemView(cellId, out GemView view))
            {
                view?.PopScale(1.3f, 0.2f);
            }
        }

        private void ClearSelection()
        {
            _selectedCell = null;
            _inputHandler.Reset();
        }

        private IEnumerator ExecuteSwapAnimation(Point from, Point to)
        {
            _isAnimating = true;
            _inputHandler.Lock();
            GameAudioManager.Instance?.PlaySwipe();

            string fromId = BoardView.GetCellId(from.Row, from.Col);
            string toId = BoardView.GetCellId(to.Row, to.Col);

            if (BoardView.TryGetGemView(fromId, out GemView fromView) &&
                BoardView.TryGetGemView(toId, out GemView toView))
            {
                Vector3 fromTarget = BoardView.BoardToWorld(to.Row, to.Col);
                Vector3 toTarget = BoardView.BoardToWorld(from.Row, from.Col);

                // Animate both gems to swapped positions
                fromView.MoveTo(fromTarget, 0.25f, null, GemMotionStyle.Swap);
                toView.MoveTo(toTarget, 0.25f, null, GemMotionStyle.Swap);

                yield return new WaitForSeconds(0.25f);

                // Try the swap in game logic
                BoardView.CommitSwap(from, to);
                bool accepted = _stateMachine.TrySwap(from.Row, from.Col, to.Row, to.Col);

                if (!accepted)
                {
                    BoardView.CommitSwap(from, to);
                    GameAudioManager.Instance?.PlayInvalid();
                    // Swap back animation - gems return to original positions
                    fromView.MoveTo(BoardView.BoardToWorld(from.Row, from.Col), 0.15f,
                        null, GemMotionStyle.Return);
                    toView.MoveTo(BoardView.BoardToWorld(to.Row, to.Col), 0.15f,
                        null, GemMotionStyle.Return);
                    yield return new WaitForSeconds(0.15f);

                    // Re-render to ensure positions are correct
                    if (_stateMachine.GameManagerInstance != null)
                    {
                        var engineState = _renderer.Render(_stateMachine.CurrentState,
                            _stateMachine.GameManagerInstance.GetBoard());
                        BoardView.Render(engineState);
                    }
                }
                else
                {
                    // Wait for game state animations to complete
                    // OnGameStateChanged was called synchronously during TrySwap,
                    // which started the AnimationPlayer
                    yield return StartCoroutine(WaitForAnimations());
                }
            }

            _inputHandler.Unlock();
            _isAnimating = false;
            _selectedCell = null;
            _inputHandler.Reset();
        }

        private IEnumerator WaitForAnimations()
        {
            while (_isGameStateAnimating || (AnimationPlayer?.IsPlaying ?? false))
            {
                yield return null;
            }
        }

        #endregion
    }
}
