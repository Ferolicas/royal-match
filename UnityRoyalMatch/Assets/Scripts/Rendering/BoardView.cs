// BoardView.cs — MonoBehaviour that renders the entire match-3 board in the 2.5D scene

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoyalMatch.Core;
using RoyalMatch.Types;
using RoyalMatch.Match3;
using RoyalMatch.Rendering;

namespace RoyalMatch.Rendering
{
    public class BoardView : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject GemPrefab;
        public GameObject ObstaclePrefab;

        [Header("Board Settings")]
        public float CellSize = 1.2f;
        public int GridRows = 11;
        public int GridCols = 9;

        [Header("Camera")]
        public Camera BoardCamera;
        public Vector3 CameraOffset = new Vector3(4.2f, 4.2f, 5.5f);

        [Header("Board Surface")]
        public GameObject BoardBackground;
        public Material BoardMaterial;

        // Runtime
        private Dictionary<string, GemView> _gemViews = new Dictionary<string, GemView>();
        private Dictionary<string, ObstacleView> _obstacleViews = new Dictionary<string, ObstacleView>();
        private Transform _gemParent;
        private Transform _obstacleParent;
        private GemPool _gemPool;
        private GameRenderer _renderer;
        private bool _isInitialized = false;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            CreatePools();
        }

        private void CreatePools()
        {
            _gemParent = new GameObject("Gems").transform;
            _gemParent.SetParent(transform);

            _obstacleParent = new GameObject("Obstacles").transform;
            _obstacleParent.SetParent(transform);
            _gemPool = new GemPool(GemPrefab, _gemParent);
        }

        public void Initialize(int rows, int cols)
        {
            GridRows = rows;
            GridCols = cols;
            SetupCamera();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            CreateBoardSurface();
            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!_isInitialized || (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight))
                return;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            SetupCamera();
        }

        private void SetupCamera()
        {
            if (BoardCamera == null)
            {
                BoardCamera = Camera.main;
            }

            if (BoardCamera != null)
            {
                // A deterministic top-down camera keeps the complete grid visible
                // at every supported aspect ratio and makes pointer raycasts line up
                // exactly with the board cells.
                BoardCamera.orthographic = true;
                // Reserve a real strip above the grid for the HUD/character.
                BoardCamera.orthographicSize = Mathf.Max(
                    Mathf.Max(GridRows, GridCols) * CellSize * 0.60f,
                    GridCols * CellSize * 0.55f / Mathf.Max(BoardCamera.aspect, 0.2f));
                BoardCamera.transform.SetPositionAndRotation(
                    new Vector3(0f, 12f, 1.25f),
                    Quaternion.Euler(90f, 0f, 0f));
                BoardCamera.nearClipPlane = 0.1f;
                BoardCamera.farClipPlane = 30f;
            }
        }

        private void CreateBoardSurface()
        {
            if (BoardBackground == null) return;

            BoardBackground.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            BoardBackground.transform.localRotation = Quaternion.identity;
            BoardBackground.transform.localScale = new Vector3(
                GridCols * CellSize + 0.45f,
                0.2f,
                GridRows * CellSize + 0.45f);

            if (BoardMaterial != null)
            {
                BoardBackground.GetComponent<MeshRenderer>().material = BoardMaterial;
            }
        }

        public Vector3 BoardToWorld(int row, int col)
        {
            return new Vector3(
                col * CellSize - (GridCols - 1) * CellSize / 2f,
                0,
                row * CellSize - (GridRows - 1) * CellSize / 2f
            );
        }

        public Vector2 WorldToBoard(Vector3 worldPos)
        {
            int row = Mathf.RoundToInt((worldPos.z + (GridRows - 1) * CellSize / 2f) / CellSize);
            int col = Mathf.RoundToInt((worldPos.x + (GridCols - 1) * CellSize / 2f) / CellSize);
            return new Vector2(row, col);
        }

        public void Render(EngineState state)
        {
            if (!_isInitialized) return;

            SyncGems(state.Gems);
            SyncObstacles(state.Obstacles);
        }

        private void SyncGems(RenderableGem[] gems)
        {
            var visibleCells = new HashSet<string>();

            foreach (var gem in gems)
            {
                string cellId = GetCellId(gem.Row, gem.Col);

                if (!gem.Visible) continue;
                visibleCells.Add(cellId);

                if (!_gemViews.ContainsKey(cellId))
                {
                    _gemViews[cellId] = CreateGemView(gem.Row, gem.Col);
                }

                var view = _gemViews[cellId];
                view.SetData(gem.Color, gem.Special, gem.Visible, gem.Scale);
                view.transform.position = BoardToWorld(gem.Row, gem.Col);
            }

            // Hide gems not in the visible set
            var keysToRemove = new List<string>();
            foreach (var kvp in _gemViews)
            {
                if (!visibleCells.Contains(kvp.Key))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (string key in keysToRemove)
            {
                _gemViews[key].ResetVisual();
                _gemPool.Release(_gemViews[key].gameObject);
                _gemViews.Remove(key);
            }
        }

        private void SyncObstacles(ObstacleRenderable[] obstacles)
        {
            var visibleCells = new HashSet<string>();

            foreach (var obs in obstacles)
            {
                string cellId = GetCellId(obs.Row, obs.Col);
                visibleCells.Add(cellId);

                if (!_obstacleViews.ContainsKey(cellId))
                {
                    _obstacleViews[cellId] = CreateObstacleView(obs.Row, obs.Col);
                }

                _obstacleViews[cellId].SetData(obs.Content, obs.Health);
                _obstacleViews[cellId].transform.position = BoardToWorld(obs.Row, obs.Col);
                _obstacleViews[cellId].gameObject.SetActive(true);
            }

            var keysToRemove = new List<string>();
            foreach (var kvp in _obstacleViews)
            {
                if (!visibleCells.Contains(kvp.Key))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (string key in keysToRemove)
            {
                _obstacleViews[key].gameObject.SetActive(false);
                _obstacleViews.Remove(key);
            }
        }

        public GemView CreateGemView(int row, int col)
        {
            var go = _gemPool.Acquire(BoardToWorld(row, col), Quaternion.identity);
            var view = go.GetComponent<GemView>();
            if (view == null)
                view = go.AddComponent<GemView>();
            view.ResetVisual();
            view.Initialize(row, col);
            _gemViews[GetCellId(row, col)] = view;
            return view;
        }

        public void CommitSwap(Point from, Point to)
        {
            string fromId = GetCellId(from.Row, from.Col);
            string toId = GetCellId(to.Row, to.Col);
            if (!_gemViews.TryGetValue(fromId, out var fromView) ||
                !_gemViews.TryGetValue(toId, out var toView)) return;

            _gemViews[fromId] = toView;
            _gemViews[toId] = fromView;
            fromView.Initialize(to.Row, to.Col);
            toView.Initialize(from.Row, from.Col);
        }

        public void MoveGemView(GemMove move, float duration)
        {
            string source = GetCellId(move.From.Row, move.From.Col);
            if (!_gemViews.TryGetValue(source, out var view)) return;
            _gemViews.Remove(source);
            _gemViews[GetCellId(move.To.Row, move.To.Col)] = view;
            view.Initialize(move.To.Row, move.To.Col);
            view.MoveTo(BoardToWorld(move.To.Row, move.To.Col), duration, null, GemMotionStyle.Drop);
        }

        public GemView SpawnGemView(GemAppearance appearance, float duration)
        {
            var p = appearance.Position;
            string cellId = GetCellId(p.Row, p.Col);
            if (_gemViews.ContainsKey(cellId)) return null;

            var view = CreateGemView(p.Row, p.Col);
            view.SetData(appearance.Color, appearance.Special, true,
                appearance.Special == SpecialType.NONE ? 1f : 1.2f);
            Vector3 target = BoardToWorld(p.Row, p.Col);
            if (appearance.Special != SpecialType.NONE)
            {
                view.transform.position = target;
                view.transform.localScale *= 0.55f;
                view.PopScale(1.38f, duration);
            }
            else
            {
                // The camera's screen-up axis is world +Z; new pieces enter
                // above the top row and travel toward decreasing row indices.
                view.transform.position = BoardToWorld(GridRows + 1, p.Col) + Vector3.up * 0.12f;
                view.transform.localScale *= 0.7f;
                view.MoveTo(target, duration, null, GemMotionStyle.Spawn);
            }
            return view;
        }

        public void PlayFlyingPropeller(GemMove flight, float duration)
        {
            var go = Object.Instantiate(GemPrefab,
                BoardToWorld(flight.From.Row, flight.From.Col) + Vector3.up * 0.55f,
                Quaternion.identity, transform);
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var view = go.GetComponent<GemView>();
            view.Initialize(flight.From.Row, flight.From.Col);
            view.SetData(PieceType.CYAN, SpecialType.PROPELLER, true, 1.12f);
            view.MoveTo(BoardToWorld(flight.To.Row, flight.To.Col) + Vector3.up * 0.55f,
                duration, () => Object.Destroy(go), GemMotionStyle.Fly);
        }

        public void RemoveGemView(int row, int col)
        {
            string id = GetCellId(row, col);
            if (!_gemViews.TryGetValue(id, out var view)) return;
            _gemViews.Remove(id);
            view.ResetVisual();
            _gemPool.Release(view.gameObject);
        }

        public void PlayObstacleHit(ObstacleHit hit, float duration)
        {
            string id = GetCellId(hit.Position.Row, hit.Position.Col);
            if (!_obstacleViews.TryGetValue(id, out var view) || view == null) return;
            view.PlayHit(hit.HealthAfter, duration);
            if (hit.Destroyed)
                StartCoroutine(RemoveObstacleAfter(id, view, duration));
        }

        private IEnumerator RemoveObstacleAfter(string id, ObstacleView view, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (view == null) yield break;
            if (_obstacleViews.TryGetValue(id, out var current) && current == view)
                _obstacleViews.Remove(id);
            Destroy(view.gameObject);
        }

        private ObstacleView CreateObstacleView(int row, int col)
        {
            var go = Object.Instantiate(ObstaclePrefab, BoardToWorld(row, col), Quaternion.identity, _obstacleParent);
            var view = go.GetComponent<ObstacleView>();
            if (view == null)
                view = go.AddComponent<ObstacleView>();
            view.Initialize(row, col);
            return view;
        }

        public string GetCellId(int row, int col)
        {
            return $"gem_{row}_{col}";
        }

        public bool HasGemView(string cellId, out GemView view)
        {
            if (_gemViews.TryGetValue(cellId, out view))
                return view != null;
            view = null;
            return false;
        }

        public bool TryGetGemView(string cellId, out GemView view) => HasGemView(cellId, out view);

        public void PlayAnimation(AnimationCommand command)
        {
            StartCoroutine(ExecuteAnimation(command));
        }

        private System.Collections.IEnumerator ExecuteAnimation(AnimationCommand command)
        {
            yield return new WaitForSeconds(command.Delay / 1000f);

            switch (command.Type)
            {
                case AnimationType.DESTROY:
                    foreach (string target in command.Targets)
                    {
                        string[] parts = target.Split(',');
                        if (int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
                        {
                            string cellId = GetCellId(row, col);
                            if (_gemViews.ContainsKey(cellId))
                            {
                                _gemViews[cellId].PlayCollectVFX();
                                _gemViews[cellId].gameObject.SetActive(false);
                                _gemViews.Remove(cellId);
                            }
                        }
                    }
                    break;

                case AnimationType.SPAWN:
                    foreach (string target in command.Targets)
                    {
                        string[] parts = target.Split(',');
                        if (int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
                        {
                            string cellId = GetCellId(row, col);
                            if (!_gemViews.ContainsKey(cellId))
                            {
                                var view = CreateGemView(row, col);
                                view.gameObject.SetActive(true);
                                view.PopScale(1f, command.Duration / 1000f);
                            }
                        }
                    }
                    break;
            }

            yield return new WaitForSeconds(command.Duration / 1000f);
        }
    }
}
