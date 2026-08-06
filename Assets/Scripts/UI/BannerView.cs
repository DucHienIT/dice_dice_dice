using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Big center-top announcement text ("Wave 3", "BOSS", arrow rain...).</summary>
    public class BannerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public void Show(string text)
        {
            _label.text = text;
            _label.DOKill();
            _label.alpha = 0f;
            DOTween.Sequence()
                .Append(_label.DOFade(1f, 0.2f))
                .AppendInterval(1.7f)
                .Append(_label.DOFade(0f, 0.4f))
                .SetLink(gameObject);
        }
    }
}
