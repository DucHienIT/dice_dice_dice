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
            _lastDirection = Vector2.right;
            _hitEnemies.Clear();

            _renderer.sprite = SpriteFactory.Projectile(spec.Visual);
            switch (spec.Visual)
            {
                case ProjectileVisual.Arrow:
                    _renderer.color = new Color(1f, 0.82f, 0.48f);
                    _cachedTransform.localScale = Vector3.one * 0.55f;
                    break;
                case ProjectileVisual.Bolt:
                    _renderer.color = spec.Crit ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.91f, 0.69f);
                    _cachedTransform.localScale = Vector3.one * 0.6f;
                    break;
                case ProjectileVisual.Shell:
                    _renderer.color = new Color(0.35f, 0.37f, 0.45f);
                    _cachedTransform.localScale = Vector3.one * 0.5f;
                    break;
                case ProjectileVisual.Meteor:
                    _renderer.color = new Color(1f, 0.48f, 0.24f);
                    _cachedTransform.localScale = Vector3.one * 0.7f;
                    break;
                case ProjectileVisual.Frost:
                    _renderer.color = new Color(0.66f, 0.91f, 1f);
                    _cachedTransform.localScale = Vector3.one * 0.45f;
                    break;
            }
        }

        /// <summary>Advances position; returns direction actually used this frame.</summary>
        public void MoveTowards(Vector2 targetPosition, float deltaTime)
        {
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
            _position += _lastDirection * (Spec.Speed * deltaTime);
            _cachedTransform.position = _position;
        }

        public void FallTo(float deltaTime)
        {
            _position.y -= Spec.Speed * deltaTime;
            _cachedTransform.position = _position;
            _cachedTransform.rotation = Quaternion.identity;
        }
    }
}
