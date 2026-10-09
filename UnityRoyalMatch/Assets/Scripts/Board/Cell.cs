// Cell.cs — Grid cell data structure
// Ported from src/game/Grid.ts Cell interface

using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public class Cell
    {
        public CellContent Content;
        public PieceType? GemColor;
        public SpecialType? SpecialType;
        public bool HasItem;
        public string ItemId;
        public int Health;

        public Cell(CellContent content = CellContent.EMPTY)
        {
            Content = content;
            GemColor = null;
            SpecialType = null;
            HasItem = false;
            ItemId = null;
            Health = 0;
        }

        public Cell Clone()
        {
            return new Cell(Content)
            {
                GemColor = this.GemColor,
                SpecialType = this.SpecialType,
                HasItem = this.HasItem,
                ItemId = this.ItemId,
                Health = this.Health,
            };
        }
    }
}
