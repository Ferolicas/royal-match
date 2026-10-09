// Point.cs — Board coordinate point
// Ported from src/types/GameState.ts Point type

using System;

namespace RoyalMatch.Types
{
    public struct Point : IEquatable<Point>
    {
        public int Row;
        public int Col;

        public Point(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString() => $"({Row}, {Col})";

        public override bool Equals(object obj) => obj is Point p && Equals(p);

        public bool Equals(Point other) => Row == other.Row && Col == other.Col;

        public override int GetHashCode() => HashCode.Combine(Row, Col);

        public static bool operator ==(Point a, Point b) => a.Equals(b);
        public static bool operator !=(Point a, Point b) => !a.Equals(b);
    }
}
