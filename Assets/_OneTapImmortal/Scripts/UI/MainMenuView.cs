using System;
using Game.Localization;
using Game.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The front screen, laid out the way mobile games lay out a home screen rather than as a
    /// stack of menu entries: status chips along the top, the hero and world as the art, one
    /// big thumb-height CTA near the bottom, and the tab bar beneath it. The tab bar lives
    /// here and only here — during a run the HUD's home button brings the player back.
    /// Deliberately a scrim, not an opaque panel, so the world stays visible behind it.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [Header("Top chips")]
        [SerializeField] private Button _profileChip;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private TextMeshProUGUI _shardLabel;

        [Header("World presentation")]
        [SerializeField] private Transform _heroPresentation;
        private Vector3 _heroGameplayPosition;
        private Vector3 _heroGameplayScale;
        private bool _heroPresentationCaptured;
        private const float MenuHeroScale = 1.45f;

        [Header("Banner")]
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _progress;
        [SerializeField] private TextMeshProUGUI _best;
        [SerializeField] private Image _progressFill;


        [Header("Call to action")]
        [SerializeField] private Button _startButton;
        [SerializeField] private TextMeshProUGUI _startLabel;
        [SerializeField] private Button _newRunButton;
        [SerializeField] private TextMeshProUGUI _newRunLabel;

        public event Action StartPressed;
        public event Action NewRunPressed;
        public event Action ProfilePressed;

        public bool IsOpen => gameObject.activeSelf;

        public void Init()
        {
            _startButton.onClick.AddListener(() => StartPressed?.Invoke());
            _newRunButton.onClick.AddListener(() => NewRunPressed?.Invoke());
            _profileChip.onClick.AddListener(() => ProfilePressed?.Invoke());
            RefreshStaticText();
            Hide();
        }

        public void RefreshStaticText()
        {
            _title.text = Loc.Get(LocKeys.MenuTitle);
            _newRunLabel.text = Loc.Get(LocKeys.MenuNewRun);
        }

        /// <summary>
        /// <paramref name="canContinue"/> is false on a fresh save or after a death: the CTA
        /// becomes "start" and the separate new-run button would be a duplicate, so it goes.
        /// </summary>
        public void Show(bool canContinue, MenuStatus status, BestSaveData best)
        {
            RefreshStaticText();
            _startLabel.text = Loc.Get(canContinue ? LocKeys.MenuContinue : LocKeys.MenuNewRun);
            if (_newRunButton.gameObject.activeSelf != canContinue)
            {
                _newRunButton.gameObject.SetActive(canContinue);
            }

            _levelLabel.text = Loc.Format(LocKeys.HudLevel, status.Level);
            _shardLabel.text = status.Shards.ToString();
            _progress.text = canContinue
                ? Loc.Format(LocKeys.MenuProgress, status.WorldName, status.Round,
                    status.RoundsPerWorld)
                : Loc.Get(LocKeys.MenuSubtitle);
            _best.text = best != null
                ? Loc.Format(LocKeys.OverlayBestShort, best.world, best.round)
                : string.Empty;

            if (_progressFill != null)
            {
                float totalRounds = Mathf.Max(1f, status.RoundsPerWorld);
                _progressFill.fillAmount = canContinue
                    ? Mathf.Clamp01(status.Round / totalRounds)
                    : 0f;
            }

            SetHeroPresentation(true);
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!gameObject.activeSelf || !_heroPresentationCaptured ||
                _heroPresentation == null) return;

            Vector3 position = _heroPresentation.position;
            position.x = 0f;
            _heroPresentation.position = position;
        }

        private void SetHeroPresentation(bool menuMode)
        {
            if (_heroPresentation == null) return;

            if (!_heroPresentationCaptured)
            {
                _heroGameplayPosition = _heroPresentation.localPosition;
                _heroGameplayScale = _heroPresentation.localScale;
                _heroPresentationCaptured = true;
            }

            if (menuMode)
            {
                Vector3 menuPosition = _heroGameplayPosition;
                menuPosition.x = 0f;
                _heroPresentation.localPosition = menuPosition;
                _heroPresentation.localScale = _heroGameplayScale * MenuHeroScale;
            }
            else
            {
                _heroPresentation.localPosition = _heroGameplayPosition;
                _heroPresentation.localScale = _heroGameplayScale;
            }
        }

        public void Hide()
        {
            SetHeroPresentation(false);
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }

    /// <summary>What the front screen shows about the run waiting behind it.</summary>
    public struct MenuStatus
    {
        public int Level;
        public int Shards;
        public string WorldName;
        public int Round;
        public int RoundsPerWorld;
    }
}
