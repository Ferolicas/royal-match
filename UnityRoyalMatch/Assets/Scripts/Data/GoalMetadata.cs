// GoalMetadata.cs — Objective type metadata (icon, description templates)
// Ported from src/data/goals.ts

namespace RoyalMatch.Data
{
    [System.Serializable]
    public class ObjectiveMetadata
    {
        public Types.ObjectiveType Id;
        public string Name;
        public string Description;
        public string Icon;
    }

    public static class GoalMetadata
    {
        public static readonly ObjectiveMetadata[] All = {
            new ObjectiveMetadata {
                Id = Types.ObjectiveType.COLLECT_COLOR,
                Name = "Collect Gems",
                Description = "Collect {target} gems of a specific color",
                Icon = "◆"
            },
            new ObjectiveMetadata {
                Id = Types.ObjectiveType.REACH_SCORE,
                Name = "Score Target",
                Description = "Reach {target} points",
                Icon = "★"
            },
            new ObjectiveMetadata {
                Id = Types.ObjectiveType.REMOVE_BLOCKER,
                Name = "Eliminar obstáculos",
                Description = "Elimina {target} obstáculos",
                Icon = "▢"
            },
            new ObjectiveMetadata {
                Id = Types.ObjectiveType.COLLECT_ITEM,
                Name = "Collect Items",
                Description = "Collect {target} {itemId}s",
                Icon = "★"
            },
            new ObjectiveMetadata {
                Id = Types.ObjectiveType.DROP_DOWN,
                Name = "Drop Objects",
                Description = "Drop {target} objects to the bottom",
                Icon = "↓"
            },
        };

        public static string GetDescription(Types.ObjectiveType type, int target, string color = null, string itemId = null)
        {
            foreach (var meta in All)
            {
                if (meta.Id == type)
                {
                    return meta.Description
                        .Replace("{target}", target.ToString())
                        .Replace("{itemId}", itemId ?? "")
                        .Replace("{color}", color ?? "");
                }
            }
            return "Unknown objective";
        }

        public static string GetIcon(Types.ObjectiveType type)
        {
            foreach (var meta in All)
            {
                if (meta.Id == type) return meta.Icon;
            }
            return "?";
        }
    }
}
