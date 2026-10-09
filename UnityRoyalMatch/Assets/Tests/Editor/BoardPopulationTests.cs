using NUnit.Framework;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using RoyalMatch.Levels;
using RoyalMatch.Match3;
using RoyalMatch.Types;

namespace RoyalMatch.Tests
{
    public class BoardPopulationTests
    {
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(1000)]
        public void StoredLevelsHaveNoEmptyPlayableCells(int number)
        {
            var filename = DifficultySystem.GetLevelFilename(number);
            var path = Path.Combine(Application.streamingAssetsPath, "Levels", filename);
            var level = JsonConvert.DeserializeObject<LevelData>(File.ReadAllText(path));
            AssertPopulated(level);
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void NewlyGeneratedLevelsHaveNoEmptyPlayableCells(int number)
        {
            AssertPopulated(LevelGenerator.GenerateLevel(number));
        }

        private static void AssertPopulated(LevelData level)
        {
            var board = new Board(new BoardConfig
            {
                Rows = level.Grid.Rows,
                Cols = level.Grid.Cols,
                GemTypes = 6,
                MinMatch = 3,
            });
            board.InitializeFromLayout(level.BoardLayout);
            for (int row = 0; row < level.Grid.Rows; row++)
            for (int col = 0; col < level.Grid.Cols; col++)
            {
                var cell = board.Grid.GetCell(row, col);
                Assert.That(cell.Content, Is.Not.EqualTo(CellContent.VOID),
                    $"Level {level.LevelNumber} has a hole at ({row}, {col})");
                Assert.That(board.GetPiece(row, col) != null || CellContentExtensions.IsObstacle(cell.Content),
                    Is.True, $"Level {level.LevelNumber} is empty at ({row}, {col})");
            }
        }
    }
}
