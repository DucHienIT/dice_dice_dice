using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Pooled enemy. All per-frame logic is driven by EnemyManager (no own Update).</summary>
    public class Enemy : MonoBehaviour
    {
        private const float CircleSpriteDiameter = 0.9375f; // 60px visual at 64ppu
        private const float RingSpriteDiameter = 0.906f;
        private const float FlashDuration = 0.12f;

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _statusRing;
        [SerializeField] private SpriteRenderer _burnMark;
        [SerializeField] private SpriteRenderer _hpBarBack;
        [SerializeField] private SpriteRenderer _hpBarFill;
        [SerializeField] private SpriteRenderer _armorBarFill;

        private struct Dot
        {
            public float Dps;
            public float TimeLeft;
        }

        private readonly List<Dot> _dots = new List<Dot>(4);
        private Transform _cachedTransform;
        private Transform _bodyTransform;
        private Vector2 _position;
        private float _slowTimer;
        private float _freezeTimer;
        private float _flashTimer;
        private float _wobblePhase;
        private float _maxArmor;
        private Color _baseColor;
        private Color _hpFillColor;

        public EnemyDefinition Definition { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float Armor { get; private set; }
        public float Speed { get; private set; }
        public float Radius { get; private set; }
        public bool Dead { get; private set; }
        public float HealTimer;

        public Vector2 Position => _position;

        private void Awake()
        {
            _cachedTransform = transform;
            _bodyTransform = _body.transform;
        }

        public void Setup(EnemyDefinition definition, float hpMultiplier, float speedMultiplier, Vector2 spawnPosition)
        {
            Definition = definition;
            MaxHp = definition.MaxHp * hpMultiplier;
            Hp = MaxHp;
            Armor = definition.Armor;
            _maxArmor = definition.Armor;
            Speed = definition.Speed * speedMultiplier;
            Radius = definition.Radius;
            Dead = false;
            _slowTimer = 0f;
            _freezeTimer = 0f;
            _flashTimer = 0f;
            HealTimer = 0f;
            _wobblePhase = Random.Range(0f, 6.28f);
            _dots.Clear();
            _position = spawnPosition;
            _cachedTransform.position = _position;

            _baseColor = definition.BodyColor;
            _body.sprite = SpriteFactory.Circle;
            _body.color = _baseColor;
            _bodyTransform.localScale = Vector3.one * (Radius * 2f / CircleSpriteDiameter);

            _statusRing.sprite = SpriteFactory.Ring;
            _statusRing.transform.localScale = Vector3.one * ((Radius * 2f + 0.1f) / RingSpriteDiameter);
            _statusRing.enabled = false;

            _burnMark.sprite = SpriteFactory.SoftCircle;
            _burnMark.transform.localPosition = new Vector3(0f, Radius * 0.7f, 0f);
            _burnMark.transform.localScale = Vector3.one * 0.14f;
            _burnMark.color = new Color(1f, 0.42f, 0.13f, 0.75f);
            _burnMark.enabled = false;

            _hpBarBack.sprite = SpriteFactory.White;
            _hpBarFill.sprite = SpriteFactory.White;
            _armorBarFill.sprite = SpriteFactory.White;
            _hpBarBack.color = new Color(0.05f, 0.06f, 0.1f, 0.9f);
            _hpFillColor = definition.IsBoss ? new Color(1f, 0.72f, 0.19f)
                : definition.IsElite ? new Color(1f, 0.29f, 0.42f)
                : new Color(0.35f, 0.82f, 0.54f);
            _hpBarFill.color = _hpFillColor;
            _armorBarFill.color = new Color(0.78f, 0.82f, 0.91f);
            float barY = Radius + 0.16f;
            _hpBarBack.transform.localPosition = new Vector3(0f, barY, 0f);
            _hpBarBack.transform.localScale = new Vector3(Radius * 2f, 0.07f, 1f);
            _armorBarFill.enabled = _maxArmor > 0f;
            _armorBarFill.transform.localPosition = new Vector3(0f, barY + 0.1f, 0f);
            RefreshBars();
        }

        public void TickTimers(float deltaTime, out float dotDamage)
        {
            dotDamage = 0f;
            if (_flashTimer > 0f)
            {
                _flashTimer -= deltaTime;
                if (_flashTimer <= 0f)
                {
                    _body.color = _baseColor;
                }
            }

            _wobblePhase += deltaTime * 6f;
            Vector3 bodyPos = _bodyTransform.localPosition;
            bodyPos.y = Mathf.Sin(_wobblePhase) * 0.035f;
            _bodyTransform.localPosition = bodyPos;

            for (int i = _dots.Count - 1; i >= 0; i--)
            {
                Dot dot = _dots[i];
                dotDamage += dot.Dps * deltaTime;
                dot.TimeLeft -= deltaTime;
                if (dot.TimeLeft <= 0f)
                {
                    _dots[i] = _dots[_dots.Count - 1];
                    _dots.RemoveAt(_dots.Count - 1);
                }
                else
                {
                    _dots[i] = dot;
                }
            }
            if (_burnMark.enabled != (_dots.Count > 0))
            {
                _burnMark.enabled = _dots.Count > 0;
            }

            bool statusVisible = false;
            if (_freezeTimer > 0f)
            {
                _freezeTimer -= deltaTime;
                _statusRing.color = new Color(0.66f, 0.91f, 1f, 0.9f);
                statusVisible = true;
            }
            else if (_slowTimer > 0f)
            {
                _slowTimer -= deltaTime;
                _statusRing.color = new Color(0.48f, 0.82f, 1f, 0.6f);
                statusVisible = true;
            }
            if (_statusRing.enabled != statusVisible)
            {
                _statusRing.enabled = statusVisible;
            }
        }

        public float CurrentSpeed(float slowFactor)
        {
            if (_freezeTimer > 0f)
            {
                return 0f;
            }
            return _slowTimer > 0f ? Speed * slowFactor : Speed;
        }

        public void Move(float distance)
        {
            _position.x -= distance;
            _cachedTransform.position = _position;
        }

        /// <summary>Applies raw damage after resists. Returns armor-absorbed flag for boss feedback.</summary>
        public bool ApplyDamage(float amount, out float dealt)
        {
            bool armorJustBroke = false;
            if (Armor > 0f)
            {
                float absorbed = Mathf.Min(Armor, amount);
                Armor -= absorbed;
                amount -= absorbed;
                armorJustBroke = Armor <= 0f;
            }
            Hp -= amount;
            dealt = amount;
            _flashTimer = FlashDuration;
            _body.color = Color.white;
            RefreshBars();
            return armorJustBroke && _maxArmor > 0f;
        }

        public void ApplyDirectDamage(float amount)
        {
            Hp -= amount;
            RefreshBars();
        }

        public void HealBy(float amount)
        {
            Hp = Mathf.Min(MaxHp, Hp + amount);
            RefreshBars();
        }

        public void AddDot(float dps, float duration)
        {
            Dot dot;
            dot.Dps = dps;
            dot.TimeLeft = duration;
            _dots.Add(dot);
        }

        public void ApplySlow(float duration)
        {
            _slowTimer = Mathf.Max(_slowTimer, duration);
        }

        public void ApplyFreeze(float duration)
        {
            _freezeTimer = Mathf.Max(_freezeTimer, duration);
        }

        public void MarkDead()
        {
            Dead = true;
        }

        private void RefreshBars()
        {
            float ratio = MaxHp > 0f ? Mathf.Clamp01(Hp / MaxHp) : 0f;
            float width = Radius * 2f * ratio;
            _hpBarFill.transform.localPosition = new Vector3(-Radius * (1f - ratio), Radius + 0.16f, 0f);
            _hpBarFill.transform.localScale = new Vector3(width, 0.07f, 1f);
            if (_maxArmor > 0f)
            {
                float armorRatio = Mathf.Clamp01(Armor / _maxArmor);
                _armorBarFill.transform.localPosition = new Vector3(-Radius * (1f - armorRatio), Radius + 0.26f, 0f);
                _armorBarFill.transform.localScale = new Vector3(Radius * 2f * armorRatio, 0.055f, 1f);
            }
        }
    }
}
