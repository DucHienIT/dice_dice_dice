using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>One of the 8 board slots: renders the item, plays roll/merge/fire feedback, forwards input to UIController.</summary>
    public class BoardSlotView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _rarityLabel;
        [SerializeField] private Image _groupDot;
        [SerializeField] private Image _progressBack;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TMP_Text _faceLabel;
        [SerializeField] private Image _selectionRing;

        private UIController _controller;
        private int _index;
        private float _lastProgress = -1f;
        private RectTransform _iconRect;

        public RectTransform Rect => _rect;

        public void Init(int index, UIController controller)
        {
            _index = index;
            _controller = controller;
            _iconRect = _icon.rectTransform;
            _groupDot.sprite = SpriteFactory.Circle;
            _faceLabel.alpha = 0f;
            SetSelected(false);
        }

        public void Render(ItemInstance item, PaletteConfig palette, UiSkin skin)
        {
            if (item == null)
            {
                _frame.sprite = skin.ItemFrameEmpty;
                _icon.enabled = false;
                _rarityLabel.text = string.Empty;
                _groupDot.enabled = false;
                _progressBack.enabled = false;
                _progressFill.enabled = false;
                SetProgress(0f);
                return;
            }

            ItemDefinition definition = item.Definition;
            _frame.sprite = skin.ItemFrame(item.Rarity);
            _icon.enabled = true;
            _icon.sprite = definition.IconSprite;
            _icon.color = Color.white;
            _iconRect.localScale = Vector3.one * (1f + 0.06f * item.RarityIndex);
            _rarityLabel.text = item.Rarity.ToString();
            _rarityLabel.color = palette.RarityColor(item.Rarity);
            _groupDot.enabled = true;
            _groupDot.color = palette.GroupColor(definition.Group);
            bool showsProgress = definition is DiceDefinition || definition is CombatItemDefinition;
            _progressBack.enabled = showsProgress;
            _progressFill.enabled = showsProgress;
            _progressFill.color = definition is DiceDefinition ? palette.Gold : new Color(0.44f, 0.63f, 1f);
        }

        public void SetSelected(bool selected)
        {
            _selectionRing.enabled = selected;
        }

        public void SetProgress(float progress)
        {
            if (Mathf.Abs(progress - _lastProgress) < 0.004f)
            {
                return;
            }
            _lastProgress = progress;
            _progressFill.fillAmount = progress;
        }

        public void PlayRollAnim(float duration)
        {
            _iconRect.DOKill();
            _iconRect.DOShakeAnchorPos(duration, 9f, 24).SetLink(gameObject);
            _iconRect.DOPunchRotation(new Vector3(0f, 0f, 24f), duration, 6).SetLink(gameObject);
        }

        public void PlayFace(int face)
        {
            _faceLabel.text = face == 6 ? "6!" : face.ToString();
            _faceLabel.alpha = 1f;
            RectTransform faceRect = _faceLabel.rectTransform;
            faceRect.DOKill();
            faceRect.localScale = Vector3.one * 0.4f;
            faceRect.DOScale(1.4f, 0.22f).SetEase(Ease.OutBack).SetLink(gameObject);
            _faceLabel.DOKill();
            _faceLabel.DOFade(0f, 0.35f).SetDelay(0.55f).SetLink(gameObject);
        }

        public void PlayFireAnim()
        {
            _iconRect.DOKill();
            _iconRect.localScale = Vector3.one;
            _iconRect.DOPunchScale(Vector3.one * 0.18f, 0.18f, 6).SetLink(gameObject);
        }

        public void PlayMergeFlash()
        {
            _rect.DOKill(true);
            _rect.DOPunchScale(Vector3.one * 0.22f, 0.35f, 5).SetLink(gameObject);
            _iconRect.DOKill();
            _iconRect.localScale = Vector3.one * 1.35f;
            _iconRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
            {
                _controller.OnSlotClicked(_index);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _controller.OnSlotBeginDrag(_index, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _controller.OnSlotDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _controller.OnSlotEndDrag(eventData.position);
        }
    }
}
