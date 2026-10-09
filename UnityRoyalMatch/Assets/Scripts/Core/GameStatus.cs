// GameStatus.cs — Game state machine enum
// Ported from src/game/GameState.ts GameStatus enum

namespace RoyalMatch.Core
{
    public enum GameStatus
    {
        IDLE,
        PLAYING,
        WAITING_INPUT,
        RESOLVING,
        WIN,
        LOSE,
        PAUSED,
    }
}
