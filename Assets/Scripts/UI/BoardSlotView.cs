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
        [SerializeField] private Image _groupDot;
        [SerializeField] private Image _progressBack;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TMP_Text _faceLabel;
        [SerializeField] private Image _selectionRing;

        private UIController _controller;
        private int _index;
        private float _lastProgress = -1f;
        private RectTransform _iconRect;
        private Image _iconShadow;
        private Image _iconGlow;

        private ItemDefinition _definition;
        private Vector2 _iconHome;
private Vector3 _renderScale = Vector3.one;

        public RectTransform Rect => _rect;

public void Init(int index, UIController controller)
        {
            _index = index;
            _controller = controller;
            _iconRect = _icon.rectTransform;
            _iconHome = _iconRect.anchoredPosition;
            EnsureIconDepthLayers();
            _groupDot.sprite = SpriteFactory.Circle;
            _faceLabel.alpha = 0f;
            SetSelected(false);
        }

public void Render(ItemInstance item, PaletteConfig palette, UiSkin skin)
        {
            if (item == null)
            {
                _definition = null;
                _frame.sprite = skin.ItemFrameEmpty;
                _icon.enabled = false;
                _iconShadow.enabled = false;
                _iconGlow.enabled = false;
                _groupDot.enabled = false;
                _progressBack.enabled = false;
                _progressFill.enabled = false;
                SetProgress(0f);
                return;
            }

            ItemDefinition definition = item.Definition;
            _definition = definition;
            _frame.sprite = skin.ItemFrame(item.Rarity);
            _icon.enabled = true;
            _icon.sprite = item.LastDiceFace > 0 && definition is DiceDefinition
                ? SpriteFactory.DiceFace(item.LastDiceFace)
                : definition.IconSprite;
            _icon.color = Color.white;
            _icon.preserveAspect = true;
            _renderScale = Vector3.one * (1f + 0.055f * item.RarityIndex);
            _iconRect.localScale = _renderScale;
            _iconRect.anchoredPosition = _iconHome;
            _iconRect.localRotation = Quaternion.identity;

            _iconShadow.enabled = true;
            _iconShadow.sprite = _icon.sprite;
            _iconShadow.preserveAspect = true;
            _iconGlow.enabled = true;
            _iconGlow.sprite = SpriteFactory.SoftCircle;
            _iconGlow.color = RarityGlow(item.Rarity);

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
            _iconRect.anchoredPosition = _iconHome;
            Sequence roll = DOTween.Sequence().SetLink(gameObject);
            int steps = Mathf.Max(4, Mathf.RoundToInt(duration / 0.085f));
            float stepDuration = duration / steps;
            for (int i = 0; i < steps; i++)
            {
                int previewFace = i % 6 + 1;
                roll.AppendCallback(() => SetDiceFace(previewFace));
                roll.Join(_iconRect.DOLocalRotate(new Vector3(0f, 0f, i % 2 == 0 ? 18f : -18f), stepDuration)
                    .SetEase(Ease.InOutSine));
                roll.Join(_iconRect.DOAnchorPos(_iconHome + new Vector2(0f, i % 2 == 0 ? 7f : -2f), stepDuration)
                    .SetEase(Ease.InOutSine));
            }
            roll.OnComplete(() =>
            {
                _iconRect.anchoredPosition = _iconHome;
                _iconRect.localRotation = Quaternion.identity;
            });
        }

public void PlayFace(int face)
        {
            _iconRect.DOKill();
            _iconRect.anchoredPosition = _iconHome;
            _iconRect.localRotation = Quaternion.identity;
            SetDiceFace(face);
            _iconRect.localScale = _renderScale * 1.2f;
            _iconRect.DOScale(_renderScale, 0.24f).SetEase(Ease.OutBack).SetLink(gameObject);
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
            _iconRect.anchoredPosition = _iconHome;
            _iconRect.localRotation = Quaternion.identity;
            _iconRect.localScale = _renderScale;

            if (_definition is SwordDefinition)
            {
                Sequence swing = DOTween.Sequence().SetLink(gameObject);
                swing.Append(_iconRect.DOLocalRotate(new Vector3(0f, 0f, 26f), 0.08f).SetEase(Ease.OutQuad));
                swing.Append(_iconRect.DOLocalRotate(new Vector3(0f, 0f, -38f), 0.13f).SetEase(Ease.InQuad));
                swing.Join(_iconRect.DOPunchScale(Vector3.one * 0.16f, 0.16f, 3));
                swing.Append(_iconRect.DOLocalRotate(Vector3.zero, 0.12f).SetEase(Ease.OutBack));
                return;
            }
            if (_definition is BowDefinition || _definition is CrossbowDefinition)
            {
                Sequence draw = DOTween.Sequence().SetLink(gameObject);
                draw.Append(_iconRect.DOScale(new Vector3(_renderScale.x * 0.78f, _renderScale.y * 1.08f, 1f), 0.15f)
                    .SetEase(Ease.OutSine));
                draw.Join(_iconRect.DOAnchorPos(_iconHome + Vector2.left * 5f, 0.15f).SetEase(Ease.OutSine));
                draw.Append(_iconRect.DOScale(_renderScale * 1.08f, 0.055f).SetEase(Ease.OutExpo));
                draw.Join(_iconRect.DOAnchorPos(_iconHome + Vector2.right * 6f, 0.055f).SetEase(Ease.OutExpo));
                draw.Append(_iconRect.DOScale(_renderScale, 0.1f).SetEase(Ease.OutBack));
                draw.Join(_iconRect.DOAnchorPos(_iconHome, 0.1f));
                return;
            }
            if (_definition is CannonDefinition)
            {
                Sequence recoil = DOTween.Sequence().SetLink(gameObject);
                recoil.Append(_iconRect.DOAnchorPos(_iconHome + Vector2.left * 10f, 0.06f).SetEase(Ease.OutExpo));
                recoil.Join(_iconRect.DOLocalRotate(new Vector3(0f, 0f, 8f), 0.06f));
                recoil.Append(_iconRect.DOAnchorPos(_iconHome, 0.2f).SetEase(Ease.OutBack));
                recoil.Join(_iconRect.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack));
                return;
            }
            if (_definition is FireBookDefinition)
            {
                _iconRect.DOPunchRotation(new Vector3(0f, 0f, 12f), 0.28f, 5).SetLink(gameObject);
                _iconRect.DOPunchScale(Vector3.one * 0.2f, 0.28f, 5).SetLink(gameObject);
                return;
            }
            if (_definition is FrostStoneDefinition)
            {
                _iconRect.DOShakeRotation(0.25f, new Vector3(0f, 0f, 8f), 12).SetLink(gameObject);
                _iconRect.DOPunchScale(Vector3.one * 0.12f, 0.25f, 4).SetLink(gameObject);
                return;
            }
            if (_definition is LightningOrbDefinition)
            {
                _iconRect.DOPunchScale(Vector3.one * 0.24f, 0.2f, 7).SetLink(gameObject);
                _iconRect.DOPunchRotation(new Vector3(0f, 0f, 16f), 0.2f, 7).SetLink(gameObject);
                return;
            }
            _iconRect.DOPunchScale(Vector3.one * 0.18f, 0.18f, 6).SetLink(gameObject);
        }

private void SetDiceFace(int face)
        {
            Sprite sprite = SpriteFactory.DiceFace(face);
            _icon.sprite = sprite;
            _iconShadow.sprite = sprite;
        }


        public void PlayMergeFlash()
        {
            _rect.DOKill(true);
            _rect.DOPunchScale(Vector3.one * 0.22f, 0.35f, 5).SetLink(gameObject);
            _iconRect.DOKill();
            _iconRect.localScale = _renderScale * 1.35f;
            _iconRect.DOScale(_renderScale, 0.3f).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        private void EnsureIconDepthLayers()
        {
            _iconGlow = CreateIconLayer("Rarity Glow", _iconRect.GetSiblingIndex());
            _iconGlow.rectTransform.sizeDelta = _iconRect.sizeDelta * 1.18f;
            _iconShadow = CreateIconLayer("Item Shadow", _iconRect.GetSiblingIndex());
            _iconShadow.color = new Color(0.06f, 0.035f, 0.09f, 0.62f);
            _iconShadow.rectTransform.anchoredPosition = _iconRect.anchoredPosition + new Vector2(4f, -6f);
        }

        private Image CreateIconLayer(string layerName, int siblingIndex)
        {
            var layer = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)layer.transform;
            rect.SetParent(_iconRect.parent, false);
            rect.anchorMin = _iconRect.anchorMin;
            rect.anchorMax = _iconRect.anchorMax;
            rect.pivot = _iconRect.pivot;
            rect.sizeDelta = _iconRect.sizeDelta;
            rect.anchoredPosition = _iconRect.anchoredPosition;
            rect.localRotation = _iconRect.localRotation;
            rect.SetSiblingIndex(siblingIndex);
            var image = layer.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Color RarityGlow(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return new Color(0.16f, 0.55f, 1f, 0.18f);
                case ItemRarity.Epic: return new Color(0.72f, 0.28f, 1f, 0.22f);
                case ItemRarity.Legendary: return new Color(1f, 0.66f, 0.12f, 0.28f);
                default: return new Color(0.72f, 0.80f, 0.90f, 0.10f);
            }
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
