using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Plays synthesized one-shots and the ambient loop. Scene object; sources wired in Inspector.</summary>
    public class AudioManager : MonoBehaviour
    {
        private const int SfxCount = 15;
        private const float MinRepeatInterval = 0.04f;

        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.5f;

        private readonly AudioClip[] _clips = new AudioClip[SfxCount];
        private readonly float[] _lastPlayTime = new float[SfxCount];
        private bool _muted;

        public bool Muted => _muted;

        private void Awake()
        {
            for (int i = 0; i < SfxCount; i++)
            {
                _clips[i] = AudioSynth.CreateClip((Sfx)i);
                _lastPlayTime[i] = -1f;
            }
            _muted = SaveSystem.Muted;
            _musicSource.clip = AudioSynth.CreateAmbientLoop();
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume;
            ApplyMute();
        }

        public void Play(Sfx sfx)
        {
            if (_muted)
            {
                return;
            }
            int index = (int)sfx;
            float now = Time.unscaledTime;
            if (now - _lastPlayTime[index] < MinRepeatInterval)
            {
                return;
            }
            _lastPlayTime[index] = now;
            _sfxSource.PlayOneShot(_clips[index], _sfxVolume);
        }

        public void ToggleMute()
        {
            _muted = !_muted;
            SaveSystem.Muted = _muted;
            ApplyMute();
        }

        private void ApplyMute()
        {
            if (_muted)
            {
                _musicSource.Stop();
            }
            else if (!_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }
    }
}
