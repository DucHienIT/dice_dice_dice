using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Pooled projectile. Movement and collision are ticked by ProjectileManager.</summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private Transform _cachedTransform;
        private Vector2 _position;


        private float _launchDelay;
private float _visualTime;
private Vector2 _lastDirection;
        private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();

        public ProjectileSpec Spec;
        public Enemy Target;

        public Vector2 Position => _position;
        public HashSet<Enemy> HitEnemies => _hitEnemies;

        private void Awake()
        {
            _cachedTransform = transform;
        }

public void Setup(in ProjectileSpec spec)
        {
            Spec = spec;
            Target = spec.Target;
            _position = spec.Origin;
            _cachedTransform.position = _position;
            _cachedTransform.rotation = Quaternion.identity;
            _lastDirection = Vector2.right;
            _visualTime = 0f;
            _launchDelay = Mathf.Max(0f, spec.LaunchDelay);
            _hitEnemies.Clear();

            _renderer.sprite = SpriteFactory.Projectile(spec.Visual);
            _renderer.color = Color.white;
            _renderer.enabled = _launchDelay <= 0f;
            _renderer.transform.localPosition = Vector3.zero;
            _renderer.transform.localRotation = Quaternion.identity;
            switch (spec.Visual)
            {
                case ProjectileVisual.Arrow:
                    _cachedTransform.localScale = Vector3.one * 0.62f;
                    break;
                case ProjectileVisual.Bolt:
                    _cachedTransform.localScale = Vector3.one * (spec.Crit ? 0.78f : 0.7f);
                    break;
                case ProjectileVisual.Shell:
                    _cachedTransform.localScale = Vector3.one * 0.58f;
                    break;
                case ProjectileVisual.Meteor:
                    _cachedTransform.localScale = Vector3.one * 0.76f;
                    break;
                case ProjectileVisual.Frost:
                    _cachedTransform.localScale = Vector3.one * 0.54f;
                    break;
            }
        }

public bool TickLaunchDelay(float deltaTime)
        {
            if (_launchDelay <= 0f)
            {
                return false;
            }

            _launchDelay -= deltaTime;
            if (_launchDelay > 0f)
            {
                return true;
            }

            _renderer.enabled = true;
            return false;
        }


        /// <summary>Advances position; returns direction actually used this frame.</summary>
public void MoveTowards(Vector2 targetPosition, float deltaTime)
        {
            AnimateVisual(deltaTime);
            Vector2 delta = targetPosition - _position;
            float distance = delta.magnitude;
            if (distance > 0.0001f)
            {
                _lastDirection = delta / distance;
            }
            _position += _lastDirection * (Spec.Speed * deltaTime);
            _cachedTransform.position = _position;
            _cachedTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_lastDirection.y, _lastDirection.x) * Mathf.Rad2Deg);
        }

public void MoveStraight(float deltaTime)
        {
            AnimateVisual(deltaTime);
            _position += _lastDirection * (Spec.Speed * deltaTime);
            _cachedTransform.position = _position;
        }

public void FallTo(float deltaTime)
        {
            AnimateVisual(deltaTime);
            _position.y -= Spec.Speed * deltaTime;
            _cachedTransform.position = _position;
            _cachedTransform.rotation = Quaternion.identity;
        }

private void AnimateVisual(float deltaTime)
        {
            _visualTime += deltaTime;
            Transform visual = _renderer.transform;
            visual.localPosition = Vector3.zero;
            switch (Spec.Visual)
            {
                case ProjectileVisual.Shell:
                    visual.localRotation = Quaternion.Euler(0f, 0f, _visualTime * 480f);
                    break;
                case ProjectileVisual.Frost:
                    visual.localRotation = Quaternion.Euler(0f, 0f, _visualTime * 180f);
                    break;
                case ProjectileVisual.Meteor:
                    visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_visualTime * 18f) * 5f);
                    break;
                default:
                    visual.localRotation = Quaternion.identity;
                    break;
            }
        }

    }
}
