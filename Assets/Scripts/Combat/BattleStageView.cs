using CCQ.Core;
using CCQ.Data;
using CCQ.Enemies;
using CCQ.Localization;
using CCQ.Sidekicks;
using TMPro;
using UnityEngine;

namespace CCQ.Combat
{
    /// <summary>
    /// Presentation conductor for the battle stage: hero/critter positions, lunge and
    /// hit-flash windows, HP bars, name label, sidekick orbs. Reads engine state,
    /// never mutates logic. Ticked by GameManager.
    /// </summary>
    public class BattleStageView : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private HeroView _hero;
        [SerializeField] private CritterView _critter;
        [SerializeField] private HpBarView _heroBar;
        [SerializeField] private HpBarView _enemyBar;
        [SerializeField] private TextMeshPro _enemyName;
        [SerializeField] private SidekickOrbView[] _orbs;
        [SerializeField] private float _heroX = -1.85f;
        [SerializeField] private float _enemyX = 1.85f;
        [SerializeField] private float _baseY = 3.1f;

        private static readonly Color BossNameColor = new Color(1f, 0.83f, 0.36f);
        private static readonly Color EnrageTint = new Color(1f, 0.55f, 0.55f);

        private EnemyState _enemy;
        private float _spawnT = 1f;
        private float _dissolveT = -1f;
        private float _enterT = 1f;
        private bool _walking;
        private float _walkPhase;
        private float _walkAmount;
        private float _enemyBarY;

        private void Awake()
        {
            _enemyBarY = _enemyBar.transform.position.y;
        }

        public float HeroX => _heroX;
        public float EnemyX => _enemyX;
        public float BaseY => _baseY;

        public Vector3 HeroFloaterPos => new Vector3(_heroX, _baseY + 1.4f, 0f);

        public Vector3 EnemyFloaterPos =>
            new Vector3(_enemyX, _baseY + (_enemy != null ? 1.3f * _enemy.Look.Size : 1.3f), 0f);

        public void ShowEnemy(EnemyState enemy)
        {
            _enemy = enemy;
            _critter.gameObject.SetActive(true);
            _critter.Init(enemy);
            _spawnT = 0f;
            _dissolveT = -1f;
            _enterT = 0f; // slides in from off-screen right, as if the hero walked up to it

            bool boss = enemy.Kind == EnemyKind.Boss;
            _enemyName.text = boss
                ? Loc.Get(LocKeys.StageBossPrefix).Replace("{0}", enemy.Name)
                : enemy.Name;
            _enemyName.color = boss ? BossNameColor : Color.white;
            Vector3 namePos = _enemyName.transform.position;
            namePos.y = _baseY + 2.15f * enemy.Look.Size + 0.32f;
            _enemyName.transform.position = namePos;
            _enemyName.gameObject.SetActive(true);
            _enemyBar.SetVisible(true);
        }

        public void BeginDissolve()
        {
            _dissolveT = 0f;
        }

        public void HideEnemy()
        {
            _enemy = null;
            _critter.gameObject.SetActive(false);
            _enemyBar.SetVisible(false);
            _enemyName.gameObject.SetActive(false);
        }

        /// <summary>
        /// Parks the floating bars and the enemy name for the front screen, where they read
        /// as stray artefacts over the menu. Restoring never revives an enemy bar that has
        /// no enemy behind it. SetValues re-fills the bars on the way back.
        /// </summary>
        public void SetBarsVisible(bool on)
        {
            _heroBar.SetVisible(on);
            bool enemyShown = on && _enemy != null;
            _enemyBar.SetVisible(enemyShown);
            if (_enemyName.gameObject.activeSelf != enemyShown)
            {
                _enemyName.gameObject.SetActive(enemyShown);
            }
        }

        /// <summary>Hero walk cycle on/off — blended, so stopping mid-stride still settles.</summary>
        public void SetWalking(bool on)
        {
            _walking = on;
        }

        public void UpdateSidekicks(PlayerState player)
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                _orbs[i].Assign(i < player.Sidekicks.Count ? player.Sidekicks[i] : null);
            }
        }

        /// <summary>time = unscaled elapsed (for bob), scaledDt matches the engine tick rate.</summary>
        public void Tick(float time, float scaledDt, BattleEngine engine, PlayerState player)
        {
            bool heroAttacking = engine.AnimActive && engine.AnimWho == BattleActor.Hero;
            bool enemyAttacking = engine.AnimActive && engine.AnimWho == BattleActor.Enemy;
            float animT = engine.AnimT;
            bool inFlashWindow = animT > 0.3f && animT < 0.7f;

            // hero — the idle bob cross-fades into the walk hop
            _walkAmount = Mathf.MoveTowards(_walkAmount, _walking ? 1f : 0f, scaledDt * 6f);
            if (_walking) _walkPhase += scaledDt * _config.WalkHopsPerSecond;
            _hero.SetWalk(_walkPhase, _walkAmount);

            float lunge = heroAttacking ? Mathf.Sin(animT * Mathf.PI) * _config.LungeDistance : 0f;
            _hero.transform.position = new Vector3(
                _heroX + lunge,
                _baseY + Mathf.Sin(time * _config.BobFrequency) * _config.BobAmplitude
                    * (1f - _walkAmount),
                0f);
            _hero.SetSwing(heroAttacking ? animT : 0f);
            _hero.SetFlash(enemyAttacking && inFlashWindow);

            // critter
            if (_enemy != null)
            {
                float scaleMul = 1f;
                if (_spawnT < 1f)
                {
                    _spawnT = Mathf.Min(1f, _spawnT + scaledDt * 4f);
                    scaleMul = EaseOutBack(_spawnT);
                }
                if (_dissolveT >= 0f)
                {
                    _dissolveT = Mathf.Min(1f, _dissolveT + scaledDt / Mathf.Max(0.05f, _config.WinDelay));
                    _critter.SetDissolve(_dissolveT);
                }
                else
                {
                    float squish = 1f + Mathf.Sin(time * 5f) * 0.03f;
                    _critter.SetSquish(scaleMul * squish, scaleMul / squish);
                }

                // entrance: slide in from off-screen right, name and HP bar riding along
                float enter = 0f;
                if (_enterT < 1f)
                {
                    _enterT = Mathf.Min(1f, _enterT
                        + scaledDt / Mathf.Max(0.01f, _config.EnemyEnterDuration));
                    float u = 1f - _enterT;
                    enter = u * u * u * _config.EnemyEnterOffset;
                    Vector3 namePos = _enemyName.transform.position;
                    namePos.x = _enemyX + enter;
                    _enemyName.transform.position = namePos;
                    _enemyBar.transform.position = new Vector3(_enemyX + enter, _enemyBarY, 0f);
                }

                float eLunge = enemyAttacking ? Mathf.Sin(animT * Mathf.PI) * _config.LungeDistance : 0f;
                _critter.transform.position = new Vector3(
                    _enemyX - eLunge + enter,
                    _baseY + Mathf.Sin(time * 2.6f + 1f) * _config.BobAmplitude,
                    0f);
                _critter.SetFlash(heroAttacking && inFlashWindow && _enemy.Hp > 0);

                if (engine.IsRunning && engine.IsEnraged)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(time * 7f);
                    _critter.SetBodyTint(Color.Lerp(Color.white, EnrageTint, pulse));
                }

                if (_enemy.Hp > 0)
                {
                    _enemyBar.SetValues(_enemy.Hp, _enemy.MaxHp);
                }
                else
                {
                    _enemyBar.SetVisible(false);
                    _enemyName.gameObject.SetActive(false);
                }
            }

            _heroBar.SetValues(player.Hp, player.MaxHp);

            // sidekick orbs float behind the hero
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (!_orbs[i].gameObject.activeSelf) continue;
                _orbs[i].transform.position = new Vector3(
                    _heroX - 0.78f - i * 0.4f,
                    _baseY + 0.42f + Mathf.Sin(time * 2.5f + i * 1.7f) * 0.07f,
                    0f);
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
