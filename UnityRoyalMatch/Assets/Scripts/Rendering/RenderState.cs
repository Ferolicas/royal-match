// RenderState.cs — Data structures for the Unity rendering layer
// Replaces PlayCanvas engine types with Unity-native alternatives

using RoyalMatch.Types;
using UnityEngine;

namespace RoyalMatch.Rendering
{
    public struct RenderableGem
    {
        public string EntityId;
        public int Row;
        public int Col;
        public PieceType Color;
        public SpecialType Special;
        public bool Visible;
        public float Scale;
    }

    public struct ObstacleRenderable
    {
        public int Row;
        public int Col;
        public CellContent Content;
        public int Health;
    }

    public struct CameraState
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public bool IsOrtho;
    }

    public class EngineState
    {
        public RenderableGem[] Gems;
        public ObstacleRenderable[] Obstacles;
        public CameraState Camera;
        public Color BackgroundColor;
        public bool IsAnimating;
    }

    public struct RenderOptions
    {
        public float AnimationDeltaTime;
        public bool ForceReinit;
    }
}
