// SettingsMenu.cs — Settings screen for sound, vibration, etc.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalMatch.Audio;

namespace RoyalMatch.UI
{
    public class SettingsMenu : MonoBehaviour
    {
        [Header("Toggles")]
        public Toggle SoundToggle;
        public Toggle VibrationToggle;
        public Toggle MusicToggle;

        [Header("UI")]
        public Button BackButton;
        public TextMeshProUGUI TitleLabel;

        private void Start()
        {
            if (SoundToggle != null) SoundToggle.onValueChanged.AddListener(OnSoundToggle);
            if (MusicToggle != null) MusicToggle.onValueChanged.AddListener(OnMusicToggle);
            if (VibrationToggle != null) VibrationToggle.onValueChanged.AddListener(OnVibrationToggle);
            if (BackButton != null) BackButton.onClick.AddListener(OnBack);

            if (TitleLabel != null)
                TitleLabel.text = "SETTINGS";

            // Load saved settings
            if (SoundToggle != null) SoundToggle.isOn = PlayerPrefs.GetInt("sound", 1) == 1;
            if (MusicToggle != null) MusicToggle.isOn = PlayerPrefs.GetInt("music", 1) == 1;
            if (VibrationToggle != null) VibrationToggle.isOn = PlayerPrefs.GetInt("vibration", 1) == 1;
        }

        private void OnSoundToggle(bool on)
        {
            PlayerPrefs.SetInt("sound", on ? 1 : 0);
            PlayerPrefs.Save();
            GameAudioManager.Instance?.RefreshSettings();
        }

        private void OnMusicToggle(bool on)
        {
            PlayerPrefs.SetInt("music", on ? 1 : 0);
            PlayerPrefs.Save();
            GameAudioManager.Instance?.RefreshSettings();
        }

        private void OnVibrationToggle(bool on)
        {
            PlayerPrefs.SetInt("vibration", on ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnBack()
        {
            GameSceneManager.Instance?.ShowMainMenu();
        }
    }
}
