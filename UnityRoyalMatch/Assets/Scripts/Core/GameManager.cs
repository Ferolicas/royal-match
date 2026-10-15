// GameManager.cs — Top-level orchestrator: state machine, level flow, scoring
// Ported from src/game/GameManager.ts

using System.Collections.Generic;
using System.Text;
using RoyalMatch.Types;
using RoyalMatch.Levels;
using RoyalMatch.Match3;

namespace RoyalMatch.Core
{
    public class GameManager
    {
        private GameStateData _state;
        private Board _board;
        private List<AnimationStep> _animationSteps;
        private int _animationIdCounter;

        public GameManager(GameStateData state)
        {
            _state = state;
            _board = new Board(new BoardConfig
            {
                Rows = state.GridRows,
                Cols = state.GridCols,
                GemTypes = state.GemTypes > 0 ? state.GemTypes : 6,
                MinMatch = 3,
            });
            _animationSteps = new List<AnimationStep>();
            _animationIdCounter = 0;
            _board.InitializeFromLayout(state.BoardLayout);
            if (state.StarterSpecials != null)
            {
                int placed = 0;
                for (int r = 0; r < state.GridRows && placed < state.StarterSpecials.Count; r++)
                    for (int c = 0; c < state.GridCols && placed < state.StarterSpecials.Count; c++)
                        if (_board.GetPiece(r, c) != null && (r * state.GridCols + c) % 13 == 6)
                            _board.SetSpecial(r, c, state.StarterSpecials[placed++]);
            }
        }

        public GameStateData GetState() => _state;
        public Board GetBoard() => _board;
        public List<AnimationStep> GetAnimationSteps() => _animationSteps;

        public bool TryUseBooster(string id, int row, int col)
        {
            if (_state.Status != GameStatus.WAITING_INPUT) return false;
            if (id == "jester")
            {
                var shuffled = _board.ShufflePieces();
                if (shuffled.Count == 0) return false;
                _animationSteps = new List<AnimationStep>();
                var command = new AnimationCommand(NextAnimationId(), AnimationType.MOVE, 300f, 0f,
                    MapPointsToTargets(shuffled.ConvertAll(move => move.To)));
                command.Moves = shuffled;
                _animationSteps.Add(new AnimationStep(command, 300f));
                return true;
            }
            if (row < 0 || row >= _board.Config.Rows || col < 0 || col >= _board.Config.Cols)
                return false;
            var targets = new List<Point>();
            if (id == "hammer") targets.Add(new Point(row, col));
            else if (id == "arrow")
                for (int c = 0; c < _board.Config.Cols; c++) targets.Add(new Point(row, c));
            else if (id == "cannon")
                for (int r = 0; r < _board.Config.Rows; r++) targets.Add(new Point(r, col));
            else return false;

            bool useful = false;
            foreach (var point in targets)
            {
                var cell = _board.Grid.GetCell(point.Row, point.Col);
                if (_board.GetPiece(point.Row, point.Col) != null ||
                    (cell != null && CellContentExtensions.IsObstacle(cell.Content))) useful = true;
            }
            if (!useful) return false;

            _board.DestroyedThisTurn.Clear();
            _animationSteps = new List<AnimationStep>();
            var colors = new Dictionary<string, int>();
            foreach (var point in targets)
            {
                var piece = _board.GetPiece(point.Row, point.Col);
                if (piece == null) continue;
                string key = piece.Color.ToString();
                colors[key] = colors.TryGetValue(key, out var count) ? count + 1 : 1;
            }
            var destroyed = _board.ClearPositions(targets);
            var hits = new List<ObstacleHit>();
            var blockers = _board.DamageAdjacentObstacles(destroyed, hits, targets);
            UpdateObjectives(colors, blockers.Count, 0);
            if (destroyed.Count > 0)
                _animationSteps.Add(new AnimationStep(new AnimationCommand(NextAnimationId(),
                    AnimationType.DESTROY, 180f, 0f, MapPointsToTargets(destroyed)), 180f));
            QueueObstacleHits(hits, 0f);
            var moves = new List<GemMove>();
            var moved = _board.ApplyGravity(moves);
            if (moved.Count > 0)
            {
                var command = new AnimationCommand(NextAnimationId(), AnimationType.MOVE, 220f, 180f,
                    MapPointsToTargets(moved));
                command.Moves = moves;
                _animationSteps.Add(new AnimationStep(command, 220f));
            }
            var spawned = _board.FillFromTop();
            if (spawned.Count > 0)
            {
                var command = new AnimationCommand(NextAnimationId(), AnimationType.SPAWN, 220f, 400f,
                    MapPointsToTargets(spawned));
                command.Appearances = CaptureAppearances(spawned);
                _animationSteps.Add(new AnimationStep(command, 220f));
            }
            var cascade = _board.FindMatches();
            if (cascade.matches.Count > 0) ProcessMatches(cascade.matches, cascade.specials, 2);
            else CheckGameOver();
            return true;
        }

        private string NextAnimationId()
        {
            return $"anim_{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{_animationIdCounter++}";
        }

        public bool TrySwap(int r1, int c1, int r2, int c2)
        {
            if (_state.Status != GameStatus.WAITING_INPUT) return false;

            // Keep renderer bookkeeping bounded to the current turn.
            _board.DestroyedThisTurn.Clear();

            var piece1 = _board.GetPiece(r1, c1);
            var piece2 = _board.GetPiece(r2, c2);
            if (piece1 == null || piece2 == null) return false;

            bool isAdjacent =
                (System.Math.Abs(r1 - r2) == 1 && c1 == c2) ||
                (System.Math.Abs(c1 - c2) == 1 && r1 == r2);
            if (!isAdjacent) return false;

            // Swap, check for matches
            _board.SwapPieces(r1, c1, r2, c2);

            if (piece1.Special != SpecialType.NONE || piece2.Special != SpecialType.NONE)
            {
                _state.Status = GameStatus.RESOLVING;
                _state.MovesRemaining--;
                _state.LastMatches = new List<Match>();
                _state.ScoreAdded = 0;
                ProcessSpecialSwap(piece1, piece2);
                return true;
            }

            var (matches, specials) = _board.FindMatches();

            if (matches.Count == 0 && !specials.Exists(s => s.Shape == "square"))
            {
                // Revert swap
                _board.SwapPieces(r1, c1, r2, c2);
                return false;
            }

            _state.Status = GameStatus.RESOLVING;
            _state.MovesRemaining--;
            _state.LastMatches = matches;
            _state.ScoreAdded = 0;

            ProcessMatches(matches, specials, 1, new Point(r2, c2));

            return true;
        }

        private void ProcessSpecialSwap(Piece first, Piece second)
        {
            if (first.Special != SpecialType.NONE && second.Special != SpecialType.NONE)
                _state.CombosMade++;
            _animationSteps = new List<AnimationStep>();
            var flights = new List<GemMove>();
            var affected = SpecialComboResolver.Resolve(_board, first, second, flights);
            if (_state.SuperLightBallActive &&
                (first.Special == SpecialType.COLOR_BOMB || second.Special == SpecialType.COLOR_BOMB))
            {
                var primary = first.Special == SpecialType.COLOR_BOMB ? second.Color : first.Color;
                var extra = (PieceType)(((int)primary + 1) % _board.Config.GemTypes);
                foreach (var point in SpecialAbilities.ResolveColorBomb(_board.ToSerializedBoard(), extra.ToString()))
                    affected.Add(point);
            }
            int specialScore = 120 + affected.Count * 10;

            // Deduplicate affected points
            var unique = DeduplicatePoints(affected);
            var colorCounts = new Dictionary<string, int>();
            foreach (var point in unique)
            {
                var piece = _board.GetPiece(point.Row, point.Col);
                if (piece != null)
                {
                    string colorKey = piece.Color.ToString();
                    if (!colorCounts.ContainsKey(colorKey))
                        colorCounts[colorKey] = 0;
                    colorCounts[colorKey]++;
                }
            }

            var destroyed = _board.ClearPositions(unique);
            var obstacleHits = new List<ObstacleHit>();
            var blockers = _board.DamageAdjacentObstacles(destroyed, obstacleHits, unique);
            _state.Score += specialScore;
            _state.ScoreAdded += specialScore;
            UpdateObjectives(colorCounts, blockers.Count, specialScore);

            var activation = new AnimationCommand(
                    NextAnimationId(),
                    AnimationType.SPECIAL_ACTIVATE,
                    350f,
                    0f,
                    MapPointsToTargets(destroyed));
            activation.Flights = flights;
            _animationSteps.Add(new AnimationStep(activation, 350f));

            QueueObstacleHits(obstacleHits, flights.Count > 0 ? 300f : 90f);

            var specialMoves = new List<GemMove>();
            var movedAfterSpecial = _board.ApplyGravity(specialMoves);
            if (movedAfterSpecial.Count > 0)
            {
                var moveCommand = new AnimationCommand(
                    NextAnimationId(), AnimationType.MOVE, 250f, 350f,
                    MapPointsToTargets(movedAfterSpecial));
                moveCommand.Moves = specialMoves;
                _animationSteps.Add(new AnimationStep(moveCommand, 250f));
            }

            var spawnedAfterSpecial = _board.FillFromTop();
            if (spawnedAfterSpecial.Count > 0)
            {
                var spawnCommand = new AnimationCommand(
                    NextAnimationId(), AnimationType.SPAWN, 250f, 600f,
                    MapPointsToTargets(spawnedAfterSpecial));
                spawnCommand.Appearances = CaptureAppearances(spawnedAfterSpecial);
                _animationSteps.Add(new AnimationStep(spawnCommand, 250f));
            }
            var cascade = _board.FindMatches();
            if (cascade.matches.Count > 0)
            {
                ProcessMatches(cascade.matches, cascade.specials, 2);
            }
            else
            {
                CheckGameOver();
            }
        }

        private void ProcessMatches(List<Match> matches, List<MatchFinder.SpecialPiece> specials,
            int cascadeNumber = 1, Point? preferredPosition = null)
        {
            if (cascadeNumber == 1)
                _animationSteps = new List<AnimationStep>();

            // Compute score for this cascade
            var scoreResult = Scoring.CalculateCascadeScore(matches, cascadeNumber);
            int scoreGained = scoreResult.points +
                specials.FindAll(s => s.Shape == "square").Count * 100;
            _state.Score += scoreGained;
            _state.ScoreAdded += scoreGained;

            var matchedPoints = GetMatchedPoints(matches, specials);
            var allPoints = new List<Point>(matchedPoints);

            // A special that participates in a match activates before the board drops.
            string[][] serialized = _board.ToSerializedBoard();
            foreach (var point in matchedPoints)
            {
                var piece = _board.GetPiece(point.Row, point.Col);
                if (piece == null || piece.Special == SpecialType.NONE) continue;

                var match = matches.Find(m => MatchContains(m, point));
                MatchDirection direction = match?.Direction ?? MatchDirection.Horizontal;

                string effectType;
                string color = null;
                string directionStr = null;

                if (piece.Special == SpecialType.BOMB)
                {
                    effectType = "bomb";
                    directionStr = null;
                }
                else if (piece.Special == SpecialType.COLOR_BOMB)
                {
                    effectType = "color_bomb";
                    color = piece.Color.ToString();
                }
                else if (piece.Special == SpecialType.ROCKET)
                {
                    effectType = "rocket";
                    directionStr = direction == MatchDirection.Horizontal ? "horizontal" : "vertical";
                }
                else
                {
                    effectType = "line_clear";
                    directionStr = "both";
                }

                allPoints.AddRange(SpecialAbilities.ApplySpecialEffect(serialized, new SpecialEffect
                {
                    Type = effectType,
                    Position = point,
                    Color = color,
                    Direction = directionStr,
                }));
            }

            var colorCounts = new Dictionary<string, int>();
            var uniquePoints = new Dictionary<string, Point>();
            foreach (var point in allPoints)
            {
                string key = $"{point.Row},{point.Col}";
                if (!uniquePoints.ContainsKey(key))
                    uniquePoints[key] = point;
            }

            foreach (var point in uniquePoints.Values)
            {
                var piece = _board.GetPiece(point.Row, point.Col);
                if (piece != null)
                {
                    string colorKey = piece.Color.ToString();
                    if (!colorCounts.ContainsKey(colorKey))
                        colorCounts[colorKey] = 0;
                    colorCounts[colorKey]++;
                }
            }

            var destroyed = _board.ClearPositions(new List<Point>(uniquePoints.Values));
            var obstacleHits = new List<ObstacleHit>();
            var destroyedBlockers = _board.DamageAdjacentObstacles(destroyed, obstacleHits, uniquePoints.Values);

            // Matching four or more leaves a real special piece on the board.
            var createdSpecials = new List<Point>();
            var occupiedSpecialPositions = new HashSet<Point>();
            foreach (var candidate in specials)
            {
                var sourceMatch = matches.Find(m =>
                    m.Color == candidate.Color && MatchContains(m, candidate.Position));
                int length = candidate.Shape == "line" ? (sourceMatch?.Length ?? 4) :
                    candidate.Shape == "square" ? 4 : 5;
                var special = SpecialAbilities.MatchToSpecial(length, candidate.Shape);
                if (special == SpecialType.ROCKET)
                    special = candidate.Direction == MatchDirection.Horizontal
                        ? SpecialType.ROCKET_HORIZONTAL : SpecialType.ROCKET_VERTICAL;

                var position = candidate.Position;
                if (preferredPosition.HasValue &&
                    (candidate.SourcePoints.Contains(preferredPosition.Value) ||
                     (sourceMatch != null && MatchContains(sourceMatch, preferredPosition.Value))))
                    position = preferredPosition.Value;

                if (special != SpecialType.NONE && occupiedSpecialPositions.Add(position))
                {
                    if (special == SpecialType.ROCKET_HORIZONTAL || special == SpecialType.ROCKET_VERTICAL)
                        _state.RocketsCreated++;
                    else if (special == SpecialType.BOMB) _state.TntCreated++;
                    else if (special == SpecialType.PROPELLER) _state.PropellersCreated++;
                    else if (special == SpecialType.COLOR_BOMB) _state.LightballsCreated++;
                    _board.SetPiece(position.Row, position.Col,
                        new Piece(candidateColor(candidate.Color), special, position.Row, position.Col));
                    createdSpecials.Add(position);
                }
            }

            float delay = (cascadeNumber - 1) * 600f;

            _animationSteps.Add(new AnimationStep(
                new AnimationCommand(
                    NextAnimationId(),
                    AnimationType.DESTROY,
                    300f,
                    delay,
                    MapPointsToTargets(destroyed)),
                300f));

            QueueObstacleHits(obstacleHits, delay + 75f);

            if (createdSpecials.Count > 0)
            {
                var specialSpawn = new AnimationCommand(
                    NextAnimationId(), AnimationType.SPAWN, 250f, delay + 300f,
                    MapPointsToTargets(createdSpecials));
                specialSpawn.Appearances = CaptureAppearances(createdSpecials);
                _animationSteps.Add(new AnimationStep(specialSpawn, 250f));
            }

            // Update objectives based on destroyed pieces
            UpdateObjectives(colorCounts, destroyedBlockers.Count, scoreGained);

            // Apply gravity
            var transitions = new List<GemMove>();
            var moved = _board.ApplyGravity(transitions);
            if (moved.Count > 0)
            {
                var moveCommand = new AnimationCommand(
                    NextAnimationId(), AnimationType.MOVE, 250f,
                    delay + 300f, MapPointsToTargets(moved));
                moveCommand.Moves = transitions;
                _animationSteps.Add(new AnimationStep(
                    moveCommand,
                    250f));
            }

            // Fill from top
            var spawned = _board.FillFromTop();
            if (spawned.Count > 0)
            {
                var spawnCommand = new AnimationCommand(
                    NextAnimationId(), AnimationType.SPAWN, 250f,
                    delay + 550f, MapPointsToTargets(spawned));
                spawnCommand.Appearances = CaptureAppearances(spawned);
                _animationSteps.Add(new AnimationStep(
                    spawnCommand,
                    250f));
            }

            // Check for cascade
            var newMatches = _board.FindMatches();
            if (newMatches.matches.Count > 0)
            {
                ProcessMatches(newMatches.matches, newMatches.specials, cascadeNumber + 1);
            }
            else
            {
                CheckGameOver();
            }
        }

        private void QueueObstacleHits(List<ObstacleHit> hits, float delay)
        {
            if (hits == null || hits.Count == 0) return;
            var command = new AnimationCommand(
                NextAnimationId(), AnimationType.OBSTACLE_HIT, 210f, delay,
                MapPointsToTargets(hits.ConvertAll(hit => hit.Position)));
            command.ObstacleHits = hits;
            _animationSteps.Add(new AnimationStep(command, 210f));
        }

        private void UpdateObjectives(Dictionary<string, int> colorCounts, int blockersDestroyed, int scoreDelta)
        {
            foreach (var obj in _state.Objectives)
            {
                switch (obj.Type)
                {
                    case ObjectiveType.COLLECT_COLOR:
                        if (obj.Color != null && colorCounts.ContainsKey(obj.Color))
                            obj.Current += colorCounts[obj.Color];
                        break;
                    case ObjectiveType.REACH_SCORE:
                        obj.Current += scoreDelta;
                        break;
                    case ObjectiveType.REMOVE_BLOCKER:
                        obj.Current += blockersDestroyed;
                        break;
                }
            }
        }

        private List<Point> GetMatchedPoints(List<Match> matches,
            List<MatchFinder.SpecialPiece> specials)
        {
            var points = new Dictionary<string, Point>();
            foreach (var match in matches)
            {
                if (match.Direction == MatchDirection.Horizontal)
                {
                    for (int c = match.Start.Col; c <= match.End.Col; c++)
                    {
                        string key = $"{match.Start.Row},{c}";
                        points[key] = new Point(match.Start.Row, c);
                    }
                }
                else
                {
                    for (int r = match.Start.Row; r <= match.End.Row; r++)
                    {
                        string key = $"{r},{match.Start.Col}";
                        points[key] = new Point(r, match.Start.Col);
                    }
                }
            }
            foreach (var candidate in specials)
                if (candidate.Shape == "square")
                    foreach (var point in candidate.SourcePoints)
                        points[$"{point.Row},{point.Col}"] = point;
            return new List<Point>(points.Values);
        }

        private bool MatchContains(Match match, Point point)
        {
            if (match.Direction == MatchDirection.Horizontal)
                return point.Row == match.Start.Row && point.Col >= match.Start.Col && point.Col <= match.End.Col;
            else
                return point.Col == match.Start.Col && point.Row >= match.Start.Row && point.Row <= match.End.Row;
        }

        private static PieceType candidateColor(string color)
        {
            if (System.Enum.TryParse<PieceType>(color, out var result))
                return result;
            return PieceType.RED; // fallback
        }

        private Piece FindOther(Piece[] pieces, Piece piece)
        {
            foreach (var candidate in pieces)
            {
                if (candidate != piece) return candidate;
            }
            return null;
        }

        private List<Point> DeduplicatePoints(List<Point> points)
        {
            var unique = new Dictionary<string, Point>();
            foreach (var p in points)
            {
                string key = $"{p.Row},{p.Col}";
                if (!unique.ContainsKey(key))
                    unique[key] = p;
            }
            return new List<Point>(unique.Values);
        }

        private List<string> MapPointsToTargets(List<Point> points)
        {
            var targets = new List<string>();
            foreach (var p in points)
                targets.Add($"{p.Row},{p.Col}");
            return targets;
        }

        private List<GemAppearance> CaptureAppearances(List<Point> positions)
        {
            var appearances = new List<GemAppearance>(positions.Count);
            foreach (var position in positions)
            {
                var piece = _board.GetPiece(position.Row, position.Col);
                if (piece != null)
                    appearances.Add(new GemAppearance(position, piece.Color, piece.Special));
            }
            return appearances;
        }

        private void CheckGameOver()
        {
            if (GameState.IsLevelComplete(_state))
            {
                _state.Status = GameStatus.WIN;
            }
            else if (_state.MovesRemaining <= 0)
            {
                _state.Status = GameStatus.LOSE;
            }
            else
            {
                if (!_board.HasValidMove())
                {
                    var shuffled = _board.ShufflePieces();
                    if (shuffled.Count == 0)
                    {
                        _state.Status = GameStatus.LOSE;
                        return;
                    }
                    var command = new AnimationCommand(NextAnimationId(), AnimationType.MOVE, 300f, 0f,
                        MapPointsToTargets(shuffled.ConvertAll(move => move.To)));
                    command.Moves = shuffled;
                    _animationSteps.Add(new AnimationStep(command, 300f));
                }
                _state.Status = GameStatus.WAITING_INPUT;
            }
        }

        public int GetScore() => _state.Score;
    }
}
