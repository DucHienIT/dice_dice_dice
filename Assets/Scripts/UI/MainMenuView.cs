using System;
using CCQ.Localization;
using CCQ.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>
    /// The front screen, laid out the way mobile games lay out a home screen rather than as a
    /// stack of menu entries: status chips along the top, the hero and planet as the art, one
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

        [Header("Banner")]
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _progress;
        [SerializeField] private TextMeshProUGUI _best;

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
        /// becomes "start" and the separate new-voyage button would be a duplicate, so it goes.
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
                ? Loc.Format(LocKeys.MenuProgress, status.PlanetName, status.Round,
                    status.RoundsPerPlanet)
                : Loc.Get(LocKeys.MenuSubtitle);
            _best.text = best != null
                ? Loc.Format(LocKeys.OverlayBestShort, best.planet, best.round)
                : string.Empty;

            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }

    /// <summary>What the front screen shows about the run waiting behind it.</summary>
    public struct MenuStatus
    {
        public int Level;
        public int Shards;
        public string PlanetName;
        public int Round;
        public int RoundsPerPlanet;
    }
}
