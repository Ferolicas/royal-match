using RoyalMatch.Data;
// GameRenderer.cs — Converts GameState/Board into renderable engine state
// Ported from src/engine/GameRenderer.ts

using System.Collections.Generic;
using UnityEngine;
using RoyalMatch.Core;
using RoyalMatch.Match3;
using RoyalMatch.Types;

namespace RoyalMatch.Rendering
{
    public class GameRenderer
    {
        private BoardView _boardView;

        public GameRenderer(BoardView boardView)
        {
            _boardView = boardView;
        }

        public EngineState Render(GameStateData gameState, Board board)
        {
            var gems = new List<RenderableGem>();
            var obstacles = new List<ObstacleRenderable>();

            for (int r = 0; r < gameState.GridRows; r++)
            {
                for (int c = 0; c < gameState.GridCols; c++)
                {
                    var piece = board.GetPiece(r, c);
                    var cell = board.Grid.GetCell(r, c);

                    if (piece != null && !piece.Collected)
                    {
                        gems.Add(new RenderableGem
                        {
                            EntityId = _boardView.GetCellId(r, c),
                            Row = r,
                            Col = c,
                            Color = piece.Color,
                            Special = piece.Special,
                            Visible = true,
                            Scale = piece.IsSpecialPiece() ? 1.2f : 1f,
                        });
                    }
                    else if (cell != null && CellContentExtensions.IsObstacle(cell.Content))
                    {
                        obstacles.Add(new ObstacleRenderable
                        {
                            Row = r,
                            Col = c,
                            Content = cell.Content,
                            Health = Mathf.Max(1, cell.Health),
                        });
                    }
                }
            }

            // Add destroyed gems (fading out)
            foreach (var destroyed in board.DestroyedThisTurn)
            {
                gems.Add(new RenderableGem
                {
                    EntityId = _boardView.GetCellId(destroyed.Row, destroyed.Col),
                    Row = destroyed.Row,
                    Col = destroyed.Col,
                    Visible = false,
                    Scale = 0f,
                    Color = Types.PieceType.RED,
                    Special = SpecialType.NONE,
                });
            }

            return new EngineState
            {
                Gems = gems.ToArray(),
                Obstacles = obstacles.ToArray(),
                Camera = new CameraState
                {
                    Position = new Vector3(0, 0, 15),
                    Rotation = new Vector3(-90, 0, 0),
                    IsOrtho = false,
                },
                BackgroundColor = GemColors.BackgroundColor,
                IsAnimating = gameState.IsAnimating,
            };
        }
    }
}
