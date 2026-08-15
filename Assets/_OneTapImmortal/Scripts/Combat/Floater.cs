using TMPro;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>One pooled damage/heal/notice text. State is driven by FloaterManager.</summary>
    public class Floater : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;

        // managed by FloaterManager — plain fields, no per-object Update
        public float Age;
        public bool Active;

        public TextMeshPro Text => _text;

        public void OnDespawn()
        {
            Active = false;
            Age = 0f;
            gameObject.SetActive(false);
        }
    }
}
