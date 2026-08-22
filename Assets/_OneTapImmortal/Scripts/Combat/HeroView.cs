using System.Collections.Generic;
using Game.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Combat
{
    public enum HeroAttackStyle
    {
        Melee,
        FlyingSword,
        Spell
    }

    /// <summary>
    /// Pose-based hero presentation. Each action uses one authored sprite; ranged attacks add a
    /// separately animated projectile so the hero never has to slide into melee range.
    /// The authored pose sheets are weak-referenced (AssetReference) and stream in from the
    /// Hero bundle at Awake — until they land the body renderer simply shows nothing, the
    /// same boot behavior as the streamed backdrop. When the builder runs without authored
    /// sheets it wires procedural sprites hard and leaves the references empty instead.
    /// </summary>
    public class HeroView : MonoBehaviour
    {
        private enum HeroPose
        {
            Idle,
            Run,
            Attack,
            RangedSword,
            Spell,
            Hit,
            Fly
        }

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite _runSprite;
        [SerializeField] private Sprite _attackSprite;
        [SerializeField] private Sprite _rangedSwordSprite;
        [SerializeField] private Sprite _spellSprite;
        [SerializeField] private Sprite _hitSprite;
        [SerializeField] private Sprite _flySprite;
        [SerializeField] private Transform _rig;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private SpriteRenderer _sword;
        [SerializeField] private SpriteRenderer _flash;
        [SerializeField] private SpriteRenderer _projectile;
        [SerializeField] private Sprite _projectileSwordSprite;
        [SerializeField] private Sprite _projectileSpellSprite;

        [Header("Streamed pose sheets (empty = procedural sprites wired hard)")]
        [SerializeField] private AssetReference _runSheet;
        [SerializeField] private AssetReference _poseSheet;
        [SerializeField] private AssetReference _rangedSheet;

        [Header("Pose motion")]
        [SerializeField] private float _runBobHeight = 0.06f;
        [SerializeField] private float _runLeanAngle = 2.2f;
        [SerializeField] private float _flyHeight = 0.28f;
        [SerializeField] private float _flyBobHeight = 0.045f;
        [SerializeField] private float _hitRecoil = 0.11f;
        [SerializeField] private float _rangedDistance = 3.65f;

        private readonly StreamedAsset<IList<Sprite>> _runStream =
            new StreamedAsset<IList<Sprite>>();
        private readonly StreamedAsset<IList<Sprite>> _poseStream =
            new StreamedAsset<IList<Sprite>>();
        private readonly StreamedAsset<IList<Sprite>> _rangedStream =
            new StreamedAsset<IList<Sprite>>();
        private HeroPose _shownPose = (HeroPose)(-1);
        private HeroAttackStyle _attackStyle;
        private Color _shadowBaseColor;
        private float _travelPhase;
        private float _travelAmount;
        private float _attack01;
        private bool _hit;
        private bool _wasTraveling;
        private bool _flyThisLeg;
        private int _travelLeg;

        public string CurrentPoseName => _shownPose.ToString();
        public string CurrentAttackStyleName => _attackStyle.ToString();

        private void Awake()
        {
            _shadowBaseColor = _shadow.color;
            Color flashColor = _flash.color;
            flashColor.a = 0.38f;
            _flash.color = flashColor;
            _flash.enabled = false;
            _projectile.enabled = false;
            RefreshPose();
            StreamPoseSheets();
        }

        private void OnDestroy()
        {
            _runStream.Release();
            _poseStream.Release();
            _rangedStream.Release();
        }

        private void StreamPoseSheets()
        {
            if (_runSheet != null && _runSheet.RuntimeKeyIsValid())
            {
                _runStream.Load(_runSheet.RuntimeKey, OnRunSheetLoaded, "hero run sheet");
            }
            if (_poseSheet != null && _poseSheet.RuntimeKeyIsValid())
            {
                _poseStream.Load(_poseSheet.RuntimeKey, OnPoseSheetLoaded, "hero pose sheet");
            }
            if (_rangedSheet != null && _rangedSheet.RuntimeKeyIsValid())
            {
                _rangedStream.Load(_rangedSheet.RuntimeKey, OnRangedSheetLoaded,
                    "hero ranged sheet");
            }
        }

        private void OnRunSheetLoaded(IList<Sprite> sprites)
        {
            _idleSprite = BySuffix(sprites, 0);
            ReapplyPose();
        }

        private void OnPoseSheetLoaded(IList<Sprite> sprites)
        {
            _runSprite = BySuffix(sprites, 0);
            _attackSprite = BySuffix(sprites, 1);
            _hitSprite = BySuffix(sprites, 2);
            _flySprite = BySuffix(sprites, 3);
            ReapplyPose();
        }

        private void OnRangedSheetLoaded(IList<Sprite> sprites)
        {
            _rangedSwordSprite = BySuffix(sprites, 0);
            _spellSprite = BySuffix(sprites, 1);
            ReapplyPose();
        }

        /// <summary>Sub-sprite order in a loaded sheet is not contractual — pick by the
        /// numeric tail of the baker's name ("cultivator_side_0" and "hero_pose_00"
        /// styles both parse). Positional fallback covers renamed art.</summary>
        private static Sprite BySuffix(IList<Sprite> sprites, int index)
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                string name = sprites[i].name;
                int cut = name.LastIndexOf('_');
                if (cut < 0 || cut == name.Length - 1) continue;
                if (int.TryParse(name.Substring(cut + 1), out int value) && value == index)
                {
                    return sprites[i];
                }
            }
            return index < sprites.Count ? sprites[index] : null;
        }

        private void ReapplyPose()
        {
            _body.sprite = SpriteFor(_shownPose);
        }

        public void SetWalk(float phase, float amount)
        {
            _travelPhase = phase;
            _travelAmount = amount;
            bool traveling = amount > 0.22f;
            if (traveling && !_wasTraveling)
            {
                _travelLeg++;
                _flyThisLeg = (_travelLeg & 1) == 0;
            }
            _wasTraveling = traveling;
            RefreshPose();
        }

        public void SetAttack(float attack01, HeroAttackStyle style)
        {
            _attack01 = attack01;
            _attackStyle = style;
            RefreshPose();
        }

        public void SetSwing(float swing01)
        {
            SetAttack(swing01, HeroAttackStyle.Melee);
        }

        public void SetFlash(bool on)
        {
            _hit = on;
            if (_flash.enabled != on) _flash.enabled = on;
            RefreshPose();
        }

        private void RefreshPose()
        {
            bool attacking = _attack01 > 0.015f && _attack01 < 0.985f;
            bool traveling = _travelAmount > 0.22f;
            HeroPose pose = _hit
                ? HeroPose.Hit
                : attacking
                    ? AttackPose(_attackStyle)
                    : traveling
                        ? (_flyThisLeg ? HeroPose.Fly : HeroPose.Run)
                        : HeroPose.Idle;

            if (_shownPose != pose)
            {
                _shownPose = pose;
                _body.sprite = SpriteFor(pose);
                _sword.enabled = pose == HeroPose.Idle;
            }

            float stride = Mathf.Sin(_travelPhase * Mathf.PI * 2f);
            Vector3 position = Vector3.zero;
            float angle = 0f;
            float shadowScale = 1f;
            float shadowAlpha = _shadowBaseColor.a;

            switch (pose)
            {
                case HeroPose.Run:
                    position = new Vector3(stride * 0.018f,
                        Mathf.Abs(stride) * _runBobHeight, 0f);
                    angle = -stride * _runLeanAngle;
                    shadowScale = 1f - Mathf.Abs(stride) * 0.08f;
                    break;
                case HeroPose.Attack:
                    float attackArc = Mathf.Sin(_attack01 * Mathf.PI);
                    position = new Vector3(attackArc * 0.055f, attackArc * 0.018f, 0f);
                    angle = -attackArc * 3.5f;
                    shadowScale = 1.05f;
                    break;
                case HeroPose.RangedSword:
                    float swordFocus = Mathf.Sin(_attack01 * Mathf.PI);
                    position = new Vector3(-swordFocus * 0.018f, swordFocus * 0.012f, 0f);
                    angle = swordFocus * 1.5f;
                    shadowScale = 0.98f;
                    break;
                case HeroPose.Spell:
                    float spellFocus = Mathf.Sin(_attack01 * Mathf.PI);
                    position = new Vector3(-spellFocus * 0.025f, spellFocus * 0.025f, 0f);
                    angle = spellFocus * 1.8f;
                    shadowScale = 1f + spellFocus * 0.04f;
                    break;
                case HeroPose.Hit:
                    position = new Vector3(-_hitRecoil, 0.025f, 0f);
                    angle = 7f;
                    shadowScale = 1.08f;
                    break;
                case HeroPose.Fly:
                    float hover = Mathf.Sin(_travelPhase * Mathf.PI * 4f);
                    position = new Vector3(0f, _flyHeight + hover * _flyBobHeight, 0f);
                    angle = hover * 1.2f;
                    shadowScale = 0.72f;
                    shadowAlpha *= 0.32f;
                    break;
            }

            _rig.localPosition = position;
            _rig.localRotation = Quaternion.Euler(0f, 0f, angle);
            _shadow.transform.localScale = new Vector3(shadowScale, shadowScale, 1f);
            Color shadowColor = _shadowBaseColor;
            shadowColor.a = shadowAlpha;
            _shadow.color = shadowColor;
            UpdateProjectile(attacking);
        }

        private void UpdateProjectile(bool attacking)
        {
            bool ranged = _attackStyle == HeroAttackStyle.FlyingSword ||
                _attackStyle == HeroAttackStyle.Spell;
            bool visible = attacking && ranged && _attack01 > 0.10f && _attack01 < 0.90f;
            if (_projectile.enabled != visible) _projectile.enabled = visible;
            if (!visible) return;

            float travel = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.12f, 0.72f, _attack01));
            float wave = Mathf.Sin(travel * Mathf.PI * 3f);
            _projectile.transform.localPosition = new Vector3(
                Mathf.Lerp(0.55f, _rangedDistance, travel),
                0.78f + wave * 0.10f, 0f);

            if (_attackStyle == HeroAttackStyle.FlyingSword)
            {
                _projectile.sprite = _projectileSwordSprite;
                _projectile.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                _projectile.transform.localScale = Vector3.one * 1.05f;
            }
            else
            {
                _projectile.sprite = _projectileSpellSprite;
                _projectile.transform.localRotation = Quaternion.Euler(
                    0f, 0f, travel * 540f);
                float pulse = 1f + Mathf.Sin(travel * Mathf.PI * 6f) * 0.15f;
                _projectile.transform.localScale = Vector3.one * pulse;
            }
        }

        private static HeroPose AttackPose(HeroAttackStyle style)
        {
            switch (style)
            {
                case HeroAttackStyle.FlyingSword: return HeroPose.RangedSword;
                case HeroAttackStyle.Spell: return HeroPose.Spell;
                default: return HeroPose.Attack;
            }
        }

        private Sprite SpriteFor(HeroPose pose)
        {
            switch (pose)
            {
                case HeroPose.Run: return _runSprite;
                case HeroPose.Attack: return _attackSprite;
                case HeroPose.RangedSword: return _rangedSwordSprite;
                case HeroPose.Spell: return _spellSprite;
                case HeroPose.Hit: return _hitSprite;
                case HeroPose.Fly: return _flySprite;
                default: return _idleSprite;
            }
        }
    }
}