// Match.cs — Match data structure
// Ported from src/types/GameState.ts Match interface

using System;

namespace RoyalMatch.Types
{
    [Serializable]
    public class Match
    {
        public Point Start;
        public Point End;
        public int Length;
        public MatchDirection Direction;
        public string Color;

        public Match() { }

        public Match(Point start, Point end, int length, MatchDirection direction, string color)
        {
            Start = start;
            End = end;
            Length = length;
            Direction = direction;
            Color = color;
        }
    }

    public enum MatchDirection
    {
        Horizontal,
        Vertical,
    }
}
