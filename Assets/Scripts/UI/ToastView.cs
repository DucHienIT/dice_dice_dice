using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>Small transient message pill under the top bar (errors, merge notices).</summary>
    public class ToastView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;

        public void Init(PaletteConfig palette)
        {
            _background.sprite = SpriteFactory.UiRounded;
            _background.type = Image.Type.Sliced;
            _background.color = new Color(0f, 0f, 0f, 0.8f);
            _group.alpha = 0f;
        }

        public void Show(string text)
        {
            _label.text = text;
            _group.DOKill();
            _group.alpha = 1f;
            _group.DOFade(0f, 0.35f).SetDelay(1.6f).SetLink(gameObject);
        }
    }
}
