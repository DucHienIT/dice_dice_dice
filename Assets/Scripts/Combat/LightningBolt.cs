using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Pooled three-point lightning segment that fades out fast.</summary>
    public class LightningBolt : MonoBehaviour
    {
        private const float Duration = 0.18f;

        [SerializeField] private LineRenderer _line;

        private float _time;
        private Color _color;

        public LineRenderer Line => _line;

        public void Setup(Vector2 from, Vector2 to, Color color)
        {
            _time = 0f;
            _color = color;
            Vector2 mid = (from + to) * 0.5f + new Vector2(Random.Range(-0.14f, 0.14f), Random.Range(-0.14f, 0.14f));
            _line.positionCount = 3;
            _line.SetPosition(0, from);
            _line.SetPosition(1, mid);
            _line.SetPosition(2, to);
            _line.startColor = color;
            _line.endColor = color;
        }

        public bool Tick(float deltaTime)
        {
            _time += deltaTime;
            float alpha = Mathf.Clamp01(1f - _time / Duration);
            Color faded = _color;
            faded.a = alpha;
            _line.startColor = faded;
            _line.endColor = faded;
            return _time >= Duration;
        }
    }
}
