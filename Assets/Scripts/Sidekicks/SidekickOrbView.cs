using Game.Data;
using UnityEngine;

namespace Game.Sidekicks
{
    /// <summary>
    /// Floating orb behind the hero for one owned sidekick. The orb sprite is baked at build
    /// time and stored on the Sidekick asset — assignment is just a sprite swap.
    /// </summary>
    public class SidekickOrbView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;

        private Sidekick _current;

        public void Assign(Sidekick sidekick)
        {
            if (sidekick == null)
            {
                _current = null;
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            if (_current != sidekick)
            {
                _current = sidekick;
                _body.sprite = sidekick.OrbSprite;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
    }
}
