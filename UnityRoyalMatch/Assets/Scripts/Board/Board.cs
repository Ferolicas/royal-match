// Board.cs — 2D grid of pieces with swap, match, gravity, and fill
// Ported from src/game/Board.ts

using System;
using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public struct BoardConfig
    {
        public int Rows;
        public int Cols;
        public int GemTypes;
        public int MinMatch;

        public static BoardConfig Default => new BoardConfig
        {
            Rows = 11,
            Cols = 9,
            GemTypes = 6,
            MinMatch = 3,
        };
    }

    public class Board
    {
        public readonly BoardConfig Config;
        public readonly Grid Grid;
        public readonly Piece[,] Pieces;
        public readonly List<Point> DestroyedThisTurn;

        private PieceType[] _colors;
        private int _nextColorIndex;

        public Board(BoardConfig? config = null)
        {
            Config = config ?? BoardConfig.Default;
            Grid = new Grid(Config.Rows, Config.Cols);
            Pieces = new Piece[Config.Rows, Config.Cols];
            DestroyedThisTurn = new List<Point>();

            _colors = new PieceType[System.Math.Clamp(Config.GemTypes, 2, (int)PieceType.CYAN + 1)];
            var palette = PieceTypeExtensions.AllTypes;
            for (int i = 0; i < _colors.Length; i++) _colors[i] = palette[i];
            _nextColorIndex = 0;
        }

        public void InitializeFromLayout(string[][] layout)
        {
            for (int r = 0; r < Config.Rows; r++)
            {
                for (int c = 0; c < Config.Cols; c++)
                {
                    string cellValue = layout[r][c];
                    var gemColor = ParseGemColor(cellValue);

                    if (gemColor.HasValue)
                    {
                        var cell = new Cell(CellContent.EMPTY)
                        {
                            GemColor = gemColor,
                        };
                        Grid.SetCell(r, c, cell);
                        Pieces[r, c] = new Piece(gemColor.Value, SpecialType.NONE, r, c);
                    }
                    else if (cellValue == "BLOCKED" || cellValue == "VOID")
                    {
                        Grid.SetCell(r, c, new Cell(
                            cellValue == "BLOCKED" ? CellContent.BLOCKED : CellContent.VOID));
                    }
                    else if (cellValue == "OBSTACLE_BOX")
                    {
                        Grid.SetCell(r, c, new Cell(CellContent.OBSTACLE_BOX) { Health = 1 });
                    }
                    else if (cellValue == "OBSTACLE_CAGE")
                    {
                        Grid.SetCell(r, c, new Cell(CellContent.OBSTACLE_CAGE) { Health = 2 });
                    }
                    else if (cellValue == "OBSTACLE_ICE")
                    {
                        Grid.SetCell(r, c, new Cell(CellContent.OBSTACLE_ICE) { Health = 3 });
                    }
                    else if (cellValue == "OBSTACLE_STONE")
                    {
                        Grid.SetCell(r, c, new Cell(CellContent.OBSTACLE_STONE) { Health = 1 });
                    }
                }
            }
        }

        private PieceType? ParseGemColor(string cellValue)
        {
            if (cellValue.StartsWith("GEM_"))
            {
                string colorStr = cellValue.Substring(4);
                if (Enum.TryParse<PieceType>(colorStr, out var result))
                    return result;
            }
            return null;
        }

        public void SwapPieces(int r1, int c1, int r2, int c2)
        {
            Grid.Swap(r1, c1, r2, c2);
            var temp = Pieces[r1, c1];
            Pieces[r1, c1] = Pieces[r2, c2];
            Pieces[r2, c2] = temp;

            if (Pieces[r1, c1] != null) Pieces[r1, c1].SetPosition(r1, c1);
            if (Pieces[r2, c2] != null) Pieces[r2, c2].SetPosition(r2, c2);
        }

        public Piece GetPiece(int row, int col)
        {
            if (!Grid.IsValidPosition(row, col)) return null;
            return Pieces[row, col];
        }

        public void SetPiece(int row, int col, Piece piece)
        {
            Pieces[row, col] = piece;
            if (piece != null)
            {
                piece.SetPosition(row, col);
                var cell = Grid.GetCell(row, col) ?? new Cell(CellContent.EMPTY);
                cell.Content = CellContent.EMPTY;
                cell.GemColor = piece.Color;
                cell.SpecialType = piece.Special;
                Grid.SetCell(row, col, cell);
            }
        }

        public (List<Match> matches, List<MatchFinder.SpecialPiece> specials) FindMatches()
        {
            var boardStr = ToSerializedBoard();
            var result = MatchFinder.FindMatchesWithSpecials(boardStr);
            return (result.Matches, result.SpecialPieces);
        }

        public bool HasValidMove()
        {
            var directions = new (int dr, int dc)[] { (1, 0), (0, 1) };
            for (int row = 0; row < Config.Rows; row++)
            {
                for (int col = 0; col < Config.Cols; col++)
                {
                    if (GetPiece(row, col) == null) continue;
                    foreach (var (dr, dc) in directions)
                    {
                        int nextRow = row + dr;
                        int nextCol = col + dc;
                        if (GetPiece(nextRow, nextCol) == null) continue;

                        if (GetPiece(row, col).IsSpecialPiece() ||
                            GetPiece(nextRow, nextCol).IsSpecialPiece()) return true;
                        SwapPieces(row, col, nextRow, nextCol);
                        var (matches, _) = FindMatches();
                        bool valid = matches.Exists(m =>
                            MatchContains(m, row, col) || MatchContains(m, nextRow, nextCol));
                        if (!valid)
                        {
                            var result = MatchFinder.FindMatchesWithSpecials(ToSerializedBoard());
                            valid = result.SpecialPieces.Exists(s => s.Shape == "square" &&
                                (s.SourcePoints.Contains(new Point(row, col)) ||
                                 s.SourcePoints.Contains(new Point(nextRow, nextCol))));
                        }
                        SwapPieces(row, col, nextRow, nextCol);

                        if (valid) return true;
                    }
                }
            }
            return false;
        }

        private bool MatchContains(Match match, int row, int col)
        {
            if (match.Direction == MatchDirection.Horizontal)
                return row == match.Start.Row && col >= match.Start.Col && col <= match.End.Col;
            else
                return col == match.Start.Col && row >= match.Start.Row && row <= match.End.Row;
        }

        public string[][] ToSerializedBoard()
        {
            var result = new string[Config.Rows][];
            for (int r = 0; r < Config.Rows; r++)
            {
                result[r] = new string[Config.Cols];
                for (int c = 0; c < Config.Cols; c++)
                {
                    var cell = Grid.GetCell(r, c);
                    if (cell == null)
                    {
                        result[r][c] = CellContent.EMPTY.ToString();
                        continue;
                    }
                    if (Pieces[r, c] != null && Pieces[r, c].Special != SpecialType.NONE)
                    {
                        result[r][c] = $"SPECIAL_{Pieces[r, c].Special}";
                    }
                    else if (cell.GemColor.HasValue)
                    {
                        result[r][c] = CellValueUtils.GemCell(cell.GemColor.Value);
                    }
                    else
                    {
                        result[r][c] = cell.Content.ToString();
                    }
                }
            }
            return result;
        }

        public List<Point> RemoveMatches(List<Match> matches)
        {
            var destroyed = new List<Point>();
            var seen = new HashSet<string>();

            foreach (var match in matches)
            {
                if (match.Direction == MatchDirection.Horizontal)
                {
                    for (int c = match.Start.Col; c <= match.End.Col; c++)
                    {
                        string key = $"{match.Start.Row},{c}";
                        if (!seen.Contains(key))
                        {
                            seen.Add(key);
                            destroyed.Add(RemovePieceAt(match.Start.Row, c));
                        }
                    }
                }
                else
                {
                    for (int r = match.Start.Row; r <= match.End.Row; r++)
                    {
                        string key = $"{r},{match.Start.Col}";
                        if (!seen.Contains(key))
                        {
                            seen.Add(key);
                            destroyed.Add(RemovePieceAt(r, match.Start.Col));
                        }
                    }
                }
            }

            return destroyed;
        }

        private Point RemovePieceAt(int row, int col)
        {
            Pieces[row, col] = null;
            var cell = Grid.GetCell(row, col) ?? new Cell(CellContent.EMPTY);
            cell.Content = CellContent.EMPTY;
            cell.GemColor = null;
            cell.SpecialType = null;
            Grid.SetCell(row, col, cell);
            DestroyedThisTurn.Add(new Point(row, col));
            return new Point(row, col);
        }

        public List<Point> ClearPositions(List<Point> points)
        {
            var cleared = new List<Point>();
            var seen = new HashSet<string>();

            foreach (var point in points)
            {
                string key = $"{point.Row},{point.Col}";
                if (seen.Contains(key) || GetPiece(point.Row, point.Col) == null)
                    continue;
                seen.Add(key);
                cleared.Add(RemovePieceAt(point.Row, point.Col));
            }

            return cleared;
        }

        public void SetSpecial(int row, int col, SpecialType special)
        {
            var piece = GetPiece(row, col);
            if (piece == null) return;
            piece.Special = special;
            var cell = Grid.GetCell(row, col);
            if (cell != null) cell.SpecialType = special;
        }

        public List<Point> DamageAdjacentObstacles(List<Point> points, List<ObstacleHit> hits = null,
            IEnumerable<Point> directTargets = null)
        {
            var candidates = new HashSet<string>();
            foreach (var point in points)
            {
                foreach (var neighbor in Grid.GetNeighbors(point.Row, point.Col))
                {
                    candidates.Add($"{neighbor.Row},{neighbor.Col}");
                }
            }
            if (directTargets != null)
            {
                foreach (var point in directTargets)
                    candidates.Add($"{point.Row},{point.Col}");
            }

            var destroyed = new List<Point>();
            foreach (string key in candidates)
            {
                string[] parts = key.Split(',');
                int row = int.Parse(parts[0]);
                int col = int.Parse(parts[1]);
                var cell = Grid.GetCell(row, col);
                if (cell == null || !CellContentExtensions.IsObstacle(cell.Content))
                    continue;
                int before = cell.Health > 0 ? cell.Health : 1;
                cell.Health = System.Math.Max(0, before - 1);
                hits?.Add(new ObstacleHit(new Point(row, col), cell.Content, before, cell.Health));
                if (cell.Health == 0)
                {
                    cell.Content = CellContent.EMPTY;
                    destroyed.Add(new Point(row, col));
                }
            }

            return destroyed;
        }

        public List<Point> ApplyGravity(List<GemMove> transitions = null)
        {
            var moved = new List<Point>();

            for (int c = 0; c < Config.Cols; c++)
            {
                // Row zero is the bottom of the rendered board.
                int writeRow = 0;

                for (int r = 0; r < Config.Rows; r++)
                {
                    var cell = Grid.GetCell(r, c);
                    if (cell != null && cell.Content != CellContent.EMPTY)
                    {
                        writeRow = r + 1;
                        continue;
                    }

                    if (Pieces[r, c] != null)
                    {
                        if (writeRow != r)
                        {
                            var piece = Pieces[r, c];
                            Pieces[writeRow, c] = piece;
                            Pieces[r, c] = null;
                            piece.SetPosition(writeRow, c);

                            var srcCell = Grid.GetCell(r, c) ?? new Cell(CellContent.EMPTY);
                            srcCell.Content = CellContent.EMPTY;
                            srcCell.GemColor = null;
                            Grid.SetCell(r, c, srcCell);

                            var targetCell = Grid.GetCell(writeRow, c) ?? new Cell(CellContent.EMPTY);
                            targetCell.Content = CellContent.EMPTY;
                            targetCell.GemColor = piece.Color;
                            targetCell.SpecialType = piece.Special;
                            Grid.SetCell(writeRow, c, targetCell);

                            var destination = new Point(writeRow, c);
                            moved.Add(destination);
                            transitions?.Add(new GemMove(new Point(r, c), destination));
                        }
                        writeRow++;
                    }
                }
            }

            return moved;
        }

        public List<Point> FillFromTop()
        {
            var spawned = new List<Point>();

            for (int c = 0; c < Config.Cols; c++)
            {
                for (int r = Config.Rows - 1; r >= 0; r--)
                {
                    if (Pieces[r, c] == null)
                    {
                        var cell = Grid.GetCell(r, c);
                        if (cell != null && cell.Content != CellContent.EMPTY)
                            continue;

                        var color = RandomColor();
                        var piece = new Piece(color, SpecialType.NONE, r, c);
                        Pieces[r, c] = piece;

                        var newCell = Grid.GetCell(r, c) ?? new Cell(CellContent.EMPTY);
                        newCell.Content = CellContent.EMPTY;
                        newCell.GemColor = color;
                        newCell.SpecialType = null;
                        Grid.SetCell(r, c, newCell);

                        spawned.Add(new Point(r, c));
                    }
                }
            }

            return spawned;
        }

        public PieceType RandomColor()
        {
            var color = _colors[_nextColorIndex % _colors.Length];
            _nextColorIndex = (_nextColorIndex + 1) % _colors.Length;
            return color;
        }

        public List<GemMove> ShufflePieces()
        {
            var positions = new List<Point>();
            var originals = new List<Piece>();
            for (int r = 0; r < Config.Rows; r++)
                for (int c = 0; c < Config.Cols; c++)
                    if (Pieces[r, c] != null)
                    {
                        positions.Add(new Point(r, c));
                        originals.Add(Pieces[r, c]);
                    }

            if (positions.Count < 2) return new List<GemMove>();
            var shuffled = new List<Piece>(originals);
            var random = new System.Random(System.Environment.TickCount);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }
                for (int i = 0; i < positions.Count; i++)
                    SetPiece(positions[i].Row, positions[i].Col, shuffled[i]);
                if (FindMatches().matches.Count == 0 && HasValidMove())
                {
                    var moves = new List<GemMove>();
                    for (int i = 0; i < positions.Count; i++)
                    {
                        int oldIndex = originals.IndexOf(shuffled[i]);
                        if (oldIndex != i) moves.Add(new GemMove(positions[oldIndex], positions[i]));
                    }
                    return moves;
                }
            }
            for (int i = 0; i < positions.Count; i++)
                SetPiece(positions[i].Row, positions[i].Col, originals[i]);
            return new List<GemMove>();
        }

        public Board Clone()
        {
            var newBoard = new Board(Config);
            for (int r = 0; r < Config.Rows; r++)
            {
                for (int c = 0; c < Config.Cols; c++)
                {
                    var piece = Pieces[r, c];
                    if (piece != null)
                    {
                        newBoard.Pieces[r, c] = new Piece(piece.Color, piece.Special, r, c);
                    }
                    newBoard.Grid.SetCell(r, c, Grid.GetCell(r, c).Clone());
                }
            }
            return newBoard;
        }
    }
}
