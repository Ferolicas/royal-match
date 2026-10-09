// AnimationStep.cs — Animation step data structure
// Ported from src/game/GameManager.ts AnimationStep interface

using RoyalMatch.Types;

namespace RoyalMatch.Core
{
    public class AnimationStep
    {
        public AnimationCommand Command;
        public float Duration;

        public AnimationStep(AnimationCommand command, float duration)
        {
            Command = command;
            Duration = duration;
        }
    }
}
