using RoyalMatch.Data;
// AnimationPlayer.cs — Handles Unity-side animation of gem movements, destructions, spawns
// Bridges GameManager animation steps to Unity coroutines

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoyalMatch.Types;
using RoyalMatch.Core;
using RoyalMatch.Rendering;
using RoyalMatch.Audio;

namespace RoyalMatch.Rendering
{
    public class AnimationPlayer : MonoBehaviour
    {
        [Header("References")]
        public BoardView BoardView;
        public VFXManager VFXManager;

        [Header("Settings")]
        public float MoveDuration = 0.25f;
        public float DestroyDuration = 0.3f;
        public float SpawnDuration = 0.25f;
        public float SpecialDuration = 0.35f;

        private bool _isPlaying = false;
        private System.Action _onComplete;
        private int _runningCommands;

        public bool IsPlaying => _isPlaying;
        public System.Action<int> OnMatch;

        public void PlaySteps(List<AnimationStep> steps, System.Action onComplete = null)
        {
            if (steps == null || steps.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (_isPlaying)
                StopAllCoroutines();
            _onComplete = onComplete;
            StartCoroutine(PlayTimeline(steps));
        }

        private IEnumerator PlayTimeline(List<AnimationStep> steps)
        {
            _isPlaying = true;
            _runningCommands = steps.Count;

            // Command delays are absolute positions on one cascade timeline.
            // Running them serially added every delay together and kept input
            // locked long after the board was already stable.
            foreach (var step in steps)
                StartCoroutine(RunScheduledCommand(step.Command));

            while (_runningCommands > 0)
                yield return null;

            _onComplete?.Invoke();
            _isPlaying = false;
        }

        private IEnumerator RunScheduledCommand(AnimationCommand command)
        {
            if (command.Delay > 0)
                yield return new WaitForSeconds(command.Delay / 1000f);
            yield return StartCoroutine(ExecuteCommand(command));
            _runningCommands--;
        }

        private IEnumerator ExecuteCommand(AnimationCommand command)
        {
            float duration = command.Duration > 0 ? command.Duration / 1000f : 0.3f;

            switch (command.Type)
            {
                case AnimationType.DESTROY:
                    GameAudioManager.Instance?.PlayMatch(command.Targets.Count);
                    OnMatch?.Invoke(command.Targets.Count);
                    foreach (string target in command.Targets)
                    {
                        ParseTarget(target, out int row, out int col);
                        string cellId = BoardView.GetCellId(row, col);
                        if (BoardView.HasGemView(cellId, out GemView view))
                        {
                            view?.PlayCollectVFX();
                            VFXManager?.Emit(VFXType.MatchPop, BoardView.BoardToWorld(row, col),
                                view != null ? GemColors.GetColor(view.Color) : (Color?)null);
                        }
                        else
                        {
                            VFXManager?.Emit(VFXType.MatchPop, BoardView.BoardToWorld(row, col));
                        }
                    }
                    if (command.Delay > 0 && command.Targets.Count > 0)
                    {
                        ParseTarget(command.Targets[0], out int cascadeRow, out int cascadeCol);
                        VFXManager?.Emit(VFXType.CascadeSparkle,
                            BoardView.BoardToWorld(cascadeRow, cascadeCol));
                    }
                    yield return new WaitForSeconds(duration);
                    foreach (string target in command.Targets)
                    {
                        ParseTarget(target, out int row, out int col);
                        BoardView.RemoveGemView(row, col);
                    }
                    yield break;

                case AnimationType.MOVE:
                    foreach (var move in command.Moves)
                    {
                        BoardView.MoveGemView(move, duration);
                    }
                    break;

                case AnimationType.SPAWN:
                    for (int i = 0; i < command.Appearances.Count; i++)
                    {
                        var appearance = command.Appearances[i];
                        BoardView.SpawnGemView(appearance, duration);
                        if (i % 3 == 0)
                            VFXManager?.Emit(VFXType.GemSpawn,
                                BoardView.BoardToWorld(appearance.Position.Row, appearance.Position.Col),
                                GemColors.GetColor(appearance.Color));
                    }
                    break;

                case AnimationType.SPECIAL_ACTIVATE:
                    GameAudioManager.Instance?.PlaySpecial();
                    OnMatch?.Invoke(Mathf.Max(command.Targets.Count, 4));
                    foreach (var flight in command.Flights)
                        BoardView.PlayFlyingPropeller(flight, duration);
                    if (command.Targets.Count > 0)
                    {
                        ParseTarget(command.Targets[0], out int centerRow, out int centerCol);
                        VFXManager?.Emit(VFXType.SpecialExplosion,
                            BoardView.BoardToWorld(centerRow, centerCol));
                    }
                    foreach (string target in command.Targets)
                    {
                        ParseTarget(target, out int row, out int col);
                        if (BoardView.HasGemView(BoardView.GetCellId(row, col), out GemView view))
                        {
                            view.PlayCollectVFX();
                            VFXManager?.Emit(VFXType.MatchPop, BoardView.BoardToWorld(row, col),
                                GemColors.GetColor(view.Color));
                        }
                    }
                    yield return new WaitForSeconds(duration);
                    foreach (string target in command.Targets)
                    {
                        ParseTarget(target, out int row, out int col);
                        BoardView.RemoveGemView(row, col);
                    }
                    yield break;

                case AnimationType.OBSTACLE_HIT:
                    foreach (var hit in command.ObstacleHits)
                    {
                        BoardView.PlayObstacleHit(hit, duration);
                        Color color = hit.Content switch
                        {
                            CellContent.OBSTACLE_BOX => new Color(1f, 0.68f, 0.25f),
                            CellContent.OBSTACLE_ICE => new Color(0.48f, 0.90f, 1f),
                            CellContent.OBSTACLE_CAGE => new Color(1f, 0.44f, 0.55f),
                            _ => new Color(0.80f, 0.79f, 0.91f),
                        };
                        VFXManager?.Emit(hit.Destroyed ? VFXType.SpecialExplosion : VFXType.MatchPop,
                            BoardView.BoardToWorld(hit.Position.Row, hit.Position.Col), color);
                    }
                    yield return new WaitForSeconds(duration);
                    yield break;

                case AnimationType.SWAP:
                    // Swap animation is handled by GameController directly
                    break;

                case AnimationType.SWAP_BACK:
                    // Swap-back is handled by GameController directly
                    break;
            }

            yield return new WaitForSeconds(duration);
        }

        private void ParseTarget(string target, out int row, out int col)
        {
            string[] parts = target.Split(',');
            row = int.TryParse(parts[0], out int r) ? r : 0;
            col = int.TryParse(parts[1], out int c) ? c : 0;
        }

        public void PlaySwapAnimation(GemView fromView, GemView toView, Vector3 fromTarget, Vector3 toTarget, float duration, System.Action onComplete)
        {
            StartCoroutine(SwapCoroutine(fromView, toView, fromTarget, toTarget, duration, onComplete));
        }

        private IEnumerator SwapCoroutine(GemView fromView, GemView toView, Vector3 fromTarget, Vector3 toTarget, float duration, System.Action onComplete)
        {
            if (fromView != null) fromView.MoveTo(fromTarget, duration);
            if (toView != null) toView.MoveTo(toTarget, duration);

            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }

        public void PlaySwapBackAnimation(GemView fromView, GemView toView, float duration, System.Action onComplete)
        {
            StartCoroutine(SwapBackCoroutine(fromView, toView, duration, onComplete));
        }

        private IEnumerator SwapBackCoroutine(GemView fromView, GemView toView, float duration, System.Action onComplete)
        {
            if (fromView != null) fromView.MoveTo(BoardView.BoardToWorld(fromView.Row, fromView.Col), duration);
            if (toView != null) toView.MoveTo(BoardView.BoardToWorld(toView.Row, toView.Col), duration);

            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }
    }
}
