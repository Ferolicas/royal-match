// ObjectiveItemView.cs — UI component for displaying a single objective

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalMatch.Levels;

namespace RoyalMatch.Rendering
{
    public class ObjectiveItemView : MonoBehaviour
    {
        public TextMeshProUGUI IconLabel;
        public TextMeshProUGUI DescriptionLabel;
        public TextMeshProUGUI ProgressLabel;
        public Image ProgressBar;
        public Image ProgressFill;

        public void Setup(RuntimeObjective objective)
        {
            string icon = Data.GoalMetadata.GetIcon(objective.Type);
            if (IconLabel != null) IconLabel.text = icon;

            string description = Data.GoalMetadata.GetDescription(
                objective.Type, objective.Target, objective.Color, objective.ItemId);
            if (DescriptionLabel != null) DescriptionLabel.text = description;

            if (ProgressLabel != null)
                ProgressLabel.text = $"{objective.Current}/{objective.Target}";

            float progress = objective.Target > 0 ? (float)objective.Current / objective.Target : 0f;
            progress = Mathf.Clamp01(progress);

            if (ProgressFill != null)
                ProgressFill.fillAmount = progress;

            if (ProgressBar != null)
                ProgressBar.gameObject.SetActive(true);
        }
    }

    public static class Data
    {
        public static class GoalMetadata
        {
            public static string GetIcon(Types.ObjectiveType type)
            {
                return RoyalMatch.Data.GoalMetadata.GetIcon(type);
            }

            public static string GetDescription(Types.ObjectiveType type, int target, string color = null, string itemId = null)
            {
                return RoyalMatch.Data.GoalMetadata.GetDescription(type, target, color, itemId);
            }
        }
    }
}
