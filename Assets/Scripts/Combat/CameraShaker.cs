using UnityEngine;

namespace CCQ.Combat
{
    /// <summary>Decaying positional shake applied to the camera holder. Ticked by GameManager.</summary>
    public class CameraShaker : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _decay = 6.5f;

        private Vector3 _basePosition;
        private float _strength;

        private void Awake()
        {
            _basePosition = _target.localPosition;
        }

        public void Shake(float strength)
        {
            if (strength > _strength) _strength = strength;
        }

        public void Tick(float dt)
        {
            if (_strength <= 0.0005f)
            {
                if (_strength > 0f)
                {
                    _strength = 0f;
                    _target.localPosition = _basePosition;
                }
                return;
            }
            Vector2 o = Random.insideUnitCircle * _strength;
            _target.localPosition = _basePosition + new Vector3(o.x, o.y, 0f);
            _strength *= Mathf.Exp(-_decay * dt);
        }
    }
}
