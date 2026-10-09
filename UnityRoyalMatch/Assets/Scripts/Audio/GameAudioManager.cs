using System;
using System.Collections;
using UnityEngine;

namespace RoyalMatch.Audio
{
    /// <summary>
    /// Central event-driven mix for music, UI, gameplay SFX and short voice reactions.
    /// Falls back to small synthesized transients so interaction is never silent
    /// while authored clips can be assigned by ProjectSetup.
    /// </summary>
    public sealed class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager Instance { get; private set; }

        [Header("Authored assets")]
        public AudioClip MusicLoop;
        public AudioClip SwipeClip;
        public AudioClip InvalidClip;
        public AudioClip MatchClip;
        public AudioClip SpecialClip;
        public AudioClip UiClip;
        public AudioClip VictoryFanfareClip;
        public AudioClip ApplauseClip;
        public AudioClip VictoryVoiceClip;
        public AudioClip[] VoiceReactions;

        [Header("Mix")]
        [Range(0f, 1f)] public float MusicVolume = 0.32f;
        [Range(0f, 1f)] public float SfxVolume = 0.82f;
        [Range(0f, 1f)] public float VoiceVolume = 0.88f;
        [Range(0f, 1f)] public float VictoryVoiceVolume = 1f;
        [Range(0f, 1f)] public float ApplauseVolume = 0.70f;
        [Range(0f, 1f)] public float FanfareVolume = 0.38f;

        private AudioSource _music;
        private AudioSource _sfx;
        private AudioSource _voice;
        private AudioSource _fanfare;
        private AudioSource _crowd;
        private float _lastVoiceTime = -10f;
        private int _voiceIndex;
        private Coroutine _celebrationRoutine;
        private bool _musicPausedForCelebration;
        public bool IsCelebrating => _musicPausedForCelebration || _celebrationRoutine != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            MigrateAudioDefaults();
            _music = CreateSource("Music", true);
            _sfx = CreateSource("SFX", false);
            _voice = CreateSource("Voice", false);
            _fanfare = CreateSource("Fanfare", false);
            _crowd = CreateSource("Crowd", false);

            SwipeClip ??= CreateChirp("Swipe", 420f, 680f, 0.08f);
            InvalidClip ??= CreateChirp("Invalid", 230f, 120f, 0.13f);
            MatchClip ??= CreateChirp("Match", 520f, 940f, 0.16f);
            SpecialClip ??= CreateChirp("Special", 380f, 1280f, 0.28f);
            UiClip ??= CreateChirp("UI", 360f, 520f, 0.07f);
        }

        private void Start()
        {
            RefreshSettings();
            EnsureClipReady(MusicLoop);
            EnsureClipReady(VictoryFanfareClip);
            EnsureClipReady(ApplauseClip);
            EnsureClipReady(VictoryVoiceClip);
            if (VoiceReactions != null)
            {
                foreach (var clip in VoiceReactions)
                    EnsureClipReady(clip);
            }
            bool musicEnabled = PlayerPrefs.GetInt("music", 1) == 1;
            if (MusicLoop != null && musicEnabled)
            {
                _music.clip = MusicLoop;
                _music.volume = 0f;
                _music.Play();
                StartCoroutine(FadeMusic(0f, MusicVolume, 2.4f));
                Debug.Log($"GameAudio: music started ({MusicLoop.name}, {MusicLoop.loadState}).");
            }
            else
                Debug.LogWarning($"GameAudio: music did not start. clip={(MusicLoop != null ? MusicLoop.name : "NULL")}, enabled={musicEnabled}.");
        }

        private static void MigrateAudioDefaults()
        {
            const string migrationKey = "audio_defaults_v2";
            if (PlayerPrefs.GetInt(migrationKey, 0) == 1) return;
            PlayerPrefs.SetInt("sound", 1);
            PlayerPrefs.SetInt("music", 1);
            PlayerPrefs.SetInt(migrationKey, 1);
            PlayerPrefs.Save();
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        private static void EnsureClipReady(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

        public void RefreshSettings()
        {
            bool soundOn = PlayerPrefs.GetInt("sound", 1) == 1;
            bool musicOn = PlayerPrefs.GetInt("music", 1) == 1;
            _sfx.mute = !soundOn;
            _voice.mute = !soundOn;
            _fanfare.mute = !soundOn;
            _crowd.mute = !soundOn;
            _music.mute = !musicOn;
            if (musicOn && MusicLoop != null && !_music.isPlaying && !_musicPausedForCelebration)
            {
                _music.clip = MusicLoop;
                _music.volume = MusicVolume;
                _music.Play();
            }
        }

        public void PlayUi() => Play(SfxBus.Ui, UiClip, 1f);
        public void PlaySwipe() => Play(SfxBus.Gameplay, SwipeClip, UnityEngine.Random.Range(0.96f, 1.05f));
        public void PlayInvalid() => Play(SfxBus.Gameplay, InvalidClip, 0.96f);

        public void PlayMatch(int clearedCount)
        {
            float pitch = Mathf.Clamp(0.94f + Mathf.Max(0, clearedCount - 3) * 0.07f, 0.94f, 1.35f);
            Play(SfxBus.Gameplay, MatchClip, pitch);
            if (clearedCount >= 5 && !IsCelebrating)
                PlayVoiceReaction();
        }

        public void PlaySpecial()
        {
            Play(SfxBus.Gameplay, SpecialClip, 1f);
            if (!IsCelebrating)
                PlayVoiceReaction();
        }

        public void PlayLevelComplete()
        {
            StopLevelComplete(false);
            if (_music.isPlaying)
            {
                _music.Pause();
                _musicPausedForCelebration = true;
            }
            Debug.Log($"GameAudio: victory celebration started; musicPaused={_musicPausedForCelebration}.");
            _celebrationRoutine = StartCoroutine(LevelCompleteRoutine());
        }

        private IEnumerator LevelCompleteRoutine()
        {
            if (VictoryFanfareClip != null)
            {
                EnsureClipReady(VictoryFanfareClip);
                _fanfare.PlayOneShot(VictoryFanfareClip, FanfareVolume);
            }
            if (ApplauseClip != null)
            {
                EnsureClipReady(ApplauseClip);
                _crowd.PlayOneShot(ApplauseClip, ApplauseVolume);
            }

            if (VictoryVoiceClip != null)
            {
                EnsureClipReady(VictoryVoiceClip);
                _voice.pitch = 1f;
                _voice.PlayOneShot(VictoryVoiceClip, VictoryVoiceVolume);
                _lastVoiceTime = Time.unscaledTime;
            }

            float celebrationLength = Mathf.Max(
                VictoryFanfareClip != null ? VictoryFanfareClip.length : 0f,
                ApplauseClip != null ? ApplauseClip.length : 0f,
                VictoryVoiceClip != null ? VictoryVoiceClip.length : 0f);
            yield return new WaitForSecondsRealtime(Mathf.Max(2.5f, celebrationLength));
            _celebrationRoutine = null;
            ResumeMusicAfterCelebration();
        }

        public void StopLevelComplete(bool resumeMusic = true)
        {
            if (_celebrationRoutine != null)
            {
                StopCoroutine(_celebrationRoutine);
                _celebrationRoutine = null;
            }
            _fanfare?.Stop();
            _crowd?.Stop();
            _voice?.Stop();
            if (resumeMusic)
                ResumeMusicAfterCelebration();
            Debug.Log($"GameAudio: victory celebration stopped; resumeMusic={resumeMusic}.");
        }

        private void ResumeMusicAfterCelebration()
        {
            if (!_musicPausedForCelebration) return;
            _musicPausedForCelebration = false;
            if (PlayerPrefs.GetInt("music", 1) == 1)
                _music.UnPause();
            Debug.Log("GameAudio: background music resumed after victory celebration.");
        }

        private IEnumerator FadeMusic(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _music.volume = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            _music.volume = to;
        }

        private enum SfxBus { Ui, Gameplay }

        private void Play(SfxBus bus, AudioClip clip, float pitch)
        {
            if (clip == null) return;
            EnsureClipReady(clip);
            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip, bus == SfxBus.Ui ? SfxVolume * 0.72f : SfxVolume);
        }

        private void PlayVoiceReaction()
        {
            if (IsCelebrating || VoiceReactions == null || VoiceReactions.Length == 0 ||
                Time.unscaledTime - _lastVoiceTime < 4.5f)
                return;

            var clip = VoiceReactions[_voiceIndex++ % VoiceReactions.Length];
            if (clip == null) return;
            _lastVoiceTime = Time.unscaledTime;
            _voice.pitch = UnityEngine.Random.Range(0.98f, 1.03f);
            _voice.PlayOneShot(clip, VoiceVolume);
        }

        private static AudioClip CreateChirp(string clipName, float startHz, float endHz, float duration)
        {
            const int sampleRate = 44100;
            int samples = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            var data = new float[samples];
            double phase = 0d;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float frequency = Mathf.Lerp(startHz, endHz, t * t);
                phase += Math.PI * 2d * frequency / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * t) * Mathf.Pow(1f - t, 0.35f);
                data[i] = (float)Math.Sin(phase) * envelope * 0.5f;
            }

            var clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
