using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>Top bar: gold, wall HP + shield, level/XP, wave and phase labels, mute. Dumb view — UIController feeds it.</summary>
    public class HUDView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _goldLabel;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _shieldFill;
        [SerializeField] private TMP_Text _hpLabel;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private Image _xpFill;
        [SerializeField] private TMP_Text _xpLabel;
        [SerializeField] private TMP_Text _waveLabel;
        [SerializeField] private TMP_Text _phaseLabel;
        [SerializeField] private Button _muteButton;
        [SerializeField] private Image _muteIcon;

        public RectTransform GoldRect => _goldLabel.rectTransform;
        public Button MuteButton => _muteButton;

        public void RefreshGold(int gold)
        {
            _goldLabel.text = gold.ToString();
        }

        public void RefreshWall(int hp, int maxHp, int shield)
        {
            _hpFill.fillAmount = maxHp > 0 ? (float)hp / maxHp : 0f;
            _shieldFill.fillAmount = maxHp > 0 ? Mathf.Clamp01((float)shield / maxHp) : 0f;
            _hpLabel.text = shield > 0 ? hp + "/" + maxHp + " (+" + shield + ")" : hp + "/" + maxHp;
        }

        public void RefreshXp(int level, int xp, int xpNeeded)
        {
            _levelLabel.text = "Lv." + level;
            _xpFill.fillAmount = xpNeeded > 0 ? (float)xp / xpNeeded : 0f;
            _xpLabel.text = xp + "/" + xpNeeded;
        }

        public void RefreshWave(int wave, int totalWaves)
        {
            _waveLabel.text = "Wave " + wave + "/" + totalWaves;
        }

        public void RefreshPhase(GamePhase phase, PaletteConfig palette)
        {
            if (phase == GamePhase.Wave)
            {
                _phaseLabel.text = "WAVE IN PROGRESS!";
                _phaseLabel.color = palette.GroupColor(ItemGroup.Weapon);
            }
            else
            {
                _phaseLabel.text = "SHOPPING PHASE";
                _phaseLabel.color = palette.TextDim;
            }
        }

        public void RefreshMute(bool muted, UiSkin skin)
        {
            _muteIcon.sprite = muted ? skin.SoundOffIcon : skin.SoundOnIcon;
        }
    }
}
