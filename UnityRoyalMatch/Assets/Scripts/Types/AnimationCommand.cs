// AnimationCommand.cs — Animation command data structure
// Ported from src/types/GameState.ts AnimationCommand interface

using System;
using System.Collections.Generic;

namespace RoyalMatch.Types
{
    [Serializable]
    public struct GemMove
    {
        public Point From;
        public Point To;

        public GemMove(Point from, Point to)
        {
            From = from;
            To = to;
        }
    }

    [Serializable]
    public struct GemAppearance
    {
        public Point Position;
        public PieceType Color;
        public SpecialType Special;

        public GemAppearance(Point position, PieceType color, SpecialType special)
        {
            Position = position;
            Color = color;
            Special = special;
        }
    }

    [Serializable]
    public struct ObstacleHit
    {
        public Point Position;
        public CellContent Content;
        public int HealthBefore;
        public int HealthAfter;

        public bool Destroyed => HealthAfter <= 0;

        public ObstacleHit(Point position, CellContent content, int healthBefore, int healthAfter)
        {
            Position = position;
            Content = content;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
        }
    }

    [Serializable]
    public class AnimationCommand
    {
        public string Id;
        public AnimationType Type;
        public float Duration;
        public float Delay;
        public List<string> Targets;
        public Dictionary<string, object> Params;
        public List<GemMove> Moves = new List<GemMove>();
        public List<GemMove> Flights = new List<GemMove>();
        public List<GemAppearance> Appearances = new List<GemAppearance>();
        public List<ObstacleHit> ObstacleHits = new List<ObstacleHit>();

        public AnimationCommand(string id, AnimationType type, float duration, float delay, List<string> targets, Dictionary<string, object> paramDict = null)
        {
            Id = id;
            Type = type;
            Duration = duration;
            Delay = delay;
            Targets = targets ?? new List<string>();
            Params = paramDict ?? new Dictionary<string, object>();
        }
    }

    public enum AnimationType
    {
        MOVE,
        SCALE_POP,
        SCALE_SHRINK,
        ROTATE,
        DESTROY,
        SPAWN,
        SPECIAL_ACTIVATE,
        OBSTACLE_HIT,
        SWAP,
        SWAP_BACK,
    }
}
