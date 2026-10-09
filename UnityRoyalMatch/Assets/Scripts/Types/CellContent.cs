// CellContent.cs — Cell content enum
// Ported from src/types/GameState.ts CellContent enum

namespace RoyalMatch.Types
{
    public enum CellContent
    {
        EMPTY,
        BLOCKED,
        OBSTACLE_BOX,
        OBSTACLE_CAGE,
        OBSTACLE_ICE,
        OBSTACLE_STONE,
        VOID,
    }

    public static class CellContentExtensions
    {
        public static string ToString(CellContent content)
        {
            return content.ToString();
        }

        public static bool IsObstacle(CellContent content)
        {
            return content == CellContent.OBSTACLE_BOX ||
                   content == CellContent.OBSTACLE_CAGE ||
                   content == CellContent.OBSTACLE_ICE ||
                   content == CellContent.OBSTACLE_STONE;
        }

        public static bool IsEmpty(CellContent content)
        {
            return content == CellContent.EMPTY || content == CellContent.VOID;
        }
    }
}
