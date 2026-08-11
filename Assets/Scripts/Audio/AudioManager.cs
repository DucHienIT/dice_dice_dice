using System.Collections.Generic;
using CCQ.Data;
using CCQ.Save;
using UnityEngine;

namespace CCQ.Audio
{
    /// <summary>
    /// Central audio hub. All clips synthesized once in Awake; SFX play round-robin on
    /// pre-placed sources (scene-authored, no AddComponent). Music = per-planet ambient pad.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource[] _sfxSources;
        [SerializeField] private float _musicVolume = 0.4f;
        [SerializeField] private float _sfxVolume = 0.55f;

        private AudioClip _engage;
        private AudioClip _click;
        private AudioClip _hit;
        private AudioClip _crit;
        private AudioClip _hurt;
        private AudioClip _heal;
        private AudioClip _chime;
        private AudioClip _win;
        private AudioClip _levelUp;
        private AudioClip _trap;
        private AudioClip _boss;
        private AudioClip _death;
        private AudioClip _warp;

        private readonly Dictionary<Planet, AudioClip> _padCache = new Dictionary<Planet, AudioClip>(8);
        private int _nextSource;
        private bool _sfxOn;
        private bool _musicOn;

        private void Awake()
        {
            _engage = SfxSynth.Engage();
            _click = SfxSynth.Click();
            _hit = SfxSynth.Hit();
            _crit = SfxSynth.CritHit();
            _hurt = SfxSynth.HeroHurt();
            _heal = SfxSynth.Heal();
            _chime = SfxSynth.Chime();
            _win = SfxSynth.WinArp();
            _levelUp = SfxSynth.LevelUp();
            _trap = SfxSynth.Trap();
            _boss = SfxSynth.BossSting();
            _death = SfxSynth.Death();
            _warp = SfxSynth.Warp();

            _sfxOn = SaveSystem.SfxOn;
            _musicOn = SaveSystem.MusicOn;
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume;
        }

        public bool MusicOn => _musicOn;
        public bool SfxOn => _sfxOn;

        public void SetMusicOn(bool on)
        {
            _musicOn = on;
            SaveSystem.MusicOn = on;
            if (!on) _musicSource.Stop();
            else if (_musicSource.clip != null) _musicSource.Play();
        }

        public void SetSfxOn(bool on)
        {
            _sfxOn = on;
            SaveSystem.SfxOn = on;
        }

        public void PlayPlanetMusic(Planet planet)
        {
            if (!_padCache.TryGetValue(planet, out AudioClip pad))
            {
                pad = SfxSynth.PadLoop(planet.MusicRootHz, planet.MinorMood);
                _padCache.Add(planet, pad);
            }
            _musicSource.clip = pad;
            if (_musicOn) _musicSource.Play();
        }

        public void PlayEngage() => Play(_engage, 1f, 0.04f);
        public void PlayClick() => Play(_click, 1f, 0.03f);
        public void PlayHeroHit(bool crit) => Play(crit ? _crit : _hit, 1f, 0.07f);
        public void PlayHeroHurt(bool crit) => Play(_hurt, crit ? 0.85f : 1f, 0.06f);
        public void PlayHeal() => Play(_heal, 1f, 0.05f);
        public void PlayChime() => Play(_chime, 1f, 0.03f);
        public void PlayWin() => Play(_win, 1f, 0.02f);
        public void PlayLevelUp() => Play(_levelUp, 1f, 0.02f);
        public void PlayTrap() => Play(_trap, 1f, 0.05f);
        public void PlayBossSting() => Play(_boss, 1f, 0f);
        public void PlayDeath() => Play(_death, 1f, 0f);
        public void PlayWarp() => Play(_warp, 1f, 0f);

        private void Play(AudioClip clip, float basePitch, float pitchJitter)
        {
            if (!_sfxOn || clip == null) return;
            AudioSource src = _sfxSources[_nextSource];
            _nextSource = (_nextSource + 1) % _sfxSources.Length;
            src.pitch = basePitch + Random.Range(-pitchJitter, pitchJitter);
            src.volume = _sfxVolume;
            src.clip = clip;
            src.Play();
        }
    }
}
