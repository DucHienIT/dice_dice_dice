using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDiceDice
{
    /// <summary>
    /// Wires every view to game events. Logic never calls UI directly — this controller subscribes
    /// to system events and pushes state into dumb views (CODE_RULES 3).
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [SerializeField] private HUDView _hud;
        [SerializeField] private BoardSlotView[] _slots;
        [SerializeField] private ShopPanelView _shopPanel;
        [SerializeField] private InfoPanelView _infoPanel;
        [SerializeField] private ModalView _modal;
        [SerializeField] private BannerView _banner;
        [SerializeField] private ToastView _toast;
        [SerializeField] private FloatingTextManager _floatingText;
        [SerializeField] private Image _dragGhost;

        private static readonly string[] GroupNames = { "Economy", "Weapon", "Magic", "Support", "Defense" };

        private const string DefaultInfoText =
            "<b>Dice make gold</b> - weapons and magic kill monsters!\n\n" +
            "- Drag an item onto an empty slot to move it.\n" +
            "- Drag two items of the <b>same type and rarity</b> together to <b>merge</b>.\n" +
            "- Shop between waves, then press <b>Start Wave</b>.";

        private GameManager _game;
        private int _selectedSlot = -1;
        private int _dragFrom = -1;
        private RectTransform _dragGhostRect;
        private readonly StringBuilder _stringBuilder = new StringBuilder(512);

        public static string GroupName(ItemGroup group)
        {
            return GroupNames[(int)group];
        }

        public void Init(GameManager game)
        {
            _game = game;
            _dragGhostRect = _dragGhost.rectTransform;
            _dragGhost.enabled = false;

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Init(i, this);
            }
            for (int i = 0; i < _shopPanel.Items.Length; i++)
            {
                _shopPanel.Items[i].Init(i, this);
            }
            _modal.Init();
            _toast.Init();
            _floatingText.Init();

            _shopPanel.RerollButton.onClick.AddListener(OnRerollClicked);
            _shopPanel.LockButton.onClick.AddListener(OnLockClicked);
            _shopPanel.StartWaveButton.onClick.AddListener(OnStartWaveClicked);
            _infoPanel.SellButton.onClick.AddListener(OnSellClicked);
            _hud.MuteButton.onClick.AddListener(OnMuteClicked);

            EconomyController economy = game.Economy;
            economy.GoldChanged += RefreshGold;
            economy.XpChanged += RefreshXp;
            game.Wall.HpChanged += RefreshWall;
            game.PhaseChanged += OnPhaseChanged;
            game.WaveChanged += OnWaveChanged;
            game.BannerRequested += _banner.Show;
            game.ToastRequested += _toast.Show;
            game.GoldPopupRequested += OnGoldPopup;
            game.Shop.OffersChanged += RenderShop;
            game.Shop.ShopMessage += _toast.Show;
            game.Board.Model.BoardChanged += RenderBoard;
            game.Board.Model.MergedAt += OnMerged;
            game.Ticker.DiceRollStarted += OnDiceRollStarted;
            game.Ticker.DiceRolled += OnDiceRolled;
            game.Ticker.ItemFired += OnItemFired;

            RefreshGold();
            RefreshXp();
            RefreshWall();
            RenderBoard();
            RenderShop();
            OnWaveChanged();
            OnPhaseChanged();
            _hud.RefreshMute(game.Audio.Muted, game.Skin);
        }

        private void Update()
        {
            if (_game == null)
            {
                return;
            }
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetProgress(_game.Ticker.GetProgress(i));
            }
        }

        // ---------- Modal flows ----------

        public void ShowIntro(Action onStart)
        {
            _modal.ShowInfo("DICE DICE DICE!", "Tower Defense + Merge + Roguelite",
                "Defend the <b>wall</b> - monsters march in from the right.\n\n" +
                "<b>Dice make gold</b> (they roll during waves) - <b>weapons and magic kill monsters</b>.\n\n" +
                "<b>Shop between waves</b> - press <b>Start Wave</b> when ready. The board has only <b>8 slots</b>!\n\n" +
                "Drag two identical items of the same rarity together to <b>merge</b> them into a higher rarity.\n\n" +
                "Kills grant XP - each level up offers 1 of 3 roguelike upgrades.\n\n" +
                "Survive all 10 waves and defeat the <b>Loaded Golem</b> to win!",
                "Play", () =>
                {
                    _modal.Hide();
                    onStart();
                });
        }

        public void ShowLevelUp(int level, List<UpgradeDefinition> choices, Action<UpgradeDefinition> onPicked)
        {
            _modal.ShowChoices("LEVEL " + level + "!", "Pick one roguelike upgrade - it lasts for the rest of the run.");
            UpgradeChoiceView[] views = _modal.Choices;
            for (int i = 0; i < views.Length; i++)
            {
                bool used = i < choices.Count;
                views[i].gameObject.SetActive(used);
                if (used)
                {
                    UpgradeDefinition upgrade = choices[i];
                    views[i].Render(upgrade, _game.Skin, GroupName(upgrade.Group), picked =>
                    {
                        _modal.Hide();
                        onPicked(picked);
                    });
                }
            }
        }

        public void ShowGameOver(bool win)
        {
            RunStats stats = _game.Stats;
            float topAmount;
            string topSource = stats.TopDamageSource(out topAmount);

            _stringBuilder.Length = 0;
            _stringBuilder.Append("Wave reached: <b>").Append(_game.Wave).Append('/').Append(_game.Config.WaveCount).Append("</b>\n");
            _stringBuilder.Append("Monsters killed: <b>").Append(stats.Kills).Append("</b>\n");
            _stringBuilder.Append("Gold from Dice: <b>").Append(stats.DiceGold).Append("</b>\n");
            _stringBuilder.Append("Rolls / sixes rolled: <b>").Append(stats.Rolls).Append(" / ").Append(stats.Sixes).Append("</b>\n");
            _stringBuilder.Append("Items bought / merges: <b>").Append(stats.ItemsBought).Append(" / ").Append(stats.Merges).Append("</b>\n");
            _stringBuilder.Append("Top damage dealer: <b>")
                .Append(topSource ?? "-");
            if (topSource != null)
            {
                _stringBuilder.Append(" (").Append(Mathf.RoundToInt(topAmount)).Append(')');
            }
            _stringBuilder.Append("</b>\n");
            _stringBuilder.Append("Upgrades chosen: ").Append(stats.ChosenUpgrades.Count == 0 ? "-" : string.Join(", ", stats.ChosenUpgrades));

            _modal.ShowInfo(
                win ? "VICTORY!" : "THE WALL HAS FALLEN",
                win ? "You defeated the Loaded Golem and survived all 10 waves!" : "The wall's HP dropped to 0.",
                _stringBuilder.ToString(),
                "Play Again", _game.Restart);
        }

        // ---------- Board interaction ----------

        public void OnSlotClicked(int index)
        {
            if (_game.Board.Model.Get(index) == null)
            {
                _selectedSlot = -1;
            }
            else
            {
                _selectedSlot = _selectedSlot == index ? -1 : index;
            }
            RenderBoard();
        }

        public void OnSlotBeginDrag(int index, Vector2 screenPosition)
        {
            ItemInstance item = _game.Board.Model.Get(index);
            if (item == null || _game.Phase == GamePhase.GameOver)
            {
                _dragFrom = -1;
                return;
            }
            _dragFrom = index;
            _dragGhost.sprite = item.Definition.IconSprite;
            _dragGhost.color = Color.white;
            _dragGhost.enabled = true;
            _dragGhostRect.position = screenPosition;
        }

        public void OnSlotDrag(Vector2 screenPosition)
        {
            if (_dragFrom < 0)
            {
                return;
            }
            _dragGhostRect.position = screenPosition;
        }

        public void OnSlotEndDrag(Vector2 screenPosition)
        {
            _dragGhost.enabled = false;
            if (_dragFrom < 0)
            {
                return;
            }
            int from = _dragFrom;
            _dragFrom = -1;
            int target = FindSlotAt(screenPosition);
            if (target < 0 || target == from)
            {
                return;
            }
            BoardMoveResult result = _game.Board.RequestMove(from, target);
            if (result == BoardMoveResult.MaxRarity)
            {
                _game.Audio.Play(Sfx.Error);
                _toast.Show("Already at max rarity (Legendary)");
            }
            _selectedSlot = -1;
            RenderBoard();
        }

        private int FindSlotAt(Vector2 screenPosition)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_slots[i].Rect, screenPosition))
                {
                    return i;
                }
            }
            return -1;
        }

        // ---------- Shop interaction ----------

        public void OnShopItemClicked(int index)
        {
            _game.Shop.Buy(index);
        }

        public void OnShopItemHovered(int index)
        {
            if (_selectedSlot >= 0)
            {
                return;
            }
            ShopController.ShopOffer offer = _game.Shop.GetOffer(index);
            if (offer.Definition == null || offer.Sold)
            {
                return;
            }
            _infoPanel.ShowText(BuildItemText(offer.Definition, ItemRarity.Common), false, string.Empty);
        }

        private void OnRerollClicked()
        {
            _game.Shop.Reroll();
            RenderShop();
        }

        private void OnLockClicked()
        {
            _game.Shop.ToggleLock();
        }

        private void OnStartWaveClicked()
        {
            _game.StartWave();
        }

        private void OnSellClicked()
        {
            if (_selectedSlot < 0)
            {
                return;
            }
            if (_game.Board.TrySell(_selectedSlot))
            {
                _toast.Show("Item sold - slot freed");
                _selectedSlot = -1;
                RenderBoard();
            }
        }

        private void OnMuteClicked()
        {
            _game.Audio.ToggleMute();
            _hud.RefreshMute(_game.Audio.Muted, _game.Skin);
        }

        // ---------- Event handlers ----------

        private void RefreshGold()
        {
            _hud.RefreshGold(_game.Economy.Gold);
        }

        private void RefreshXp()
        {
            EconomyController economy = _game.Economy;
            _hud.RefreshXp(economy.Level, economy.Xp, economy.XpNeeded);
        }

        private void RefreshWall()
        {
            BaseWall wall = _game.Wall;
            _hud.RefreshWall(wall.Hp, wall.MaxHp, wall.Shield);
        }

        private void OnPhaseChanged()
        {
            bool shopping = _game.Phase == GamePhase.Shopping;
            _hud.RefreshPhase(_game.Phase, _game.Palette);
            _shopPanel.SetVisible(shopping);
            if (!shopping)
            {
                _selectedSlot = -1;
            }
            RefreshShopButtons();
            RenderBoard();
        }

        private void OnWaveChanged()
        {
            _hud.RefreshWave(_game.Wave, _game.Config.WaveCount);
            RefreshShopButtons();
        }

        private void OnGoldPopup(int slot, int amount)
        {
            _floatingText.ShowGoldFly(_game.Config.SlotWorldPosition(slot), amount);
        }

        private void OnMerged(int slot, ItemInstance merged)
        {
            _slots[slot].PlayMergeFlash();
            _toast.Show("Merge! " + merged.Definition.DisplayName + " -> " + merged.Rarity);
        }

        private void OnDiceRollStarted(int slot)
        {
            _slots[slot].PlayRollAnim(_game.Config.RollAnimDuration);
        }

        private void OnDiceRolled(int slot, int face, int gold)
        {
            _slots[slot].PlayFace(face);
            _floatingText.ShowGoldFly(_game.Config.SlotWorldPosition(slot), gold);
            _game.Audio.Play(Sfx.Coin);
        }

        private void OnItemFired(int slot)
        {
            _slots[slot].PlayFireAnim();
        }

        // ---------- Rendering ----------

        private void RenderBoard()
        {
            BoardModel model = _game.Board.Model;
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Render(model.Get(i), _game.Palette, _game.Skin);
                _slots[i].SetSelected(i == _selectedSlot);
            }
            RefreshInfo();
        }

        private void RenderShop()
        {
            ShopController shop = _game.Shop;
            ShopItemView[] views = _shopPanel.Items;
            for (int i = 0; i < views.Length; i++)
            {
                ShopController.ShopOffer offer = shop.GetOffer(i);
                if (offer.Definition != null)
                {
                    views[i].Render(offer, _game.Palette, GroupName(offer.Definition.Group));
                }
            }
            RefreshShopButtons();
        }

        private void RefreshShopButtons()
        {
            _shopPanel.RefreshButtons(_game.Shop.CurrentRerollCost, _game.Shop.Locked,
                _game.Wave + 1, _game.Config.WaveCount, _game.Palette);
        }

        private void RefreshInfo()
        {
            if (_selectedSlot < 0 || _game.Board.Model.Get(_selectedSlot) == null)
            {
                _infoPanel.ShowText(DefaultInfoText, false, string.Empty);
                return;
            }
            ItemInstance item = _game.Board.Model.Get(_selectedSlot);
            bool canSell = _game.Phase == GamePhase.Shopping;
            string sellLabel = "Sell (" + item.Definition.SellPrice(item.Rarity, _game.Mods) + "g)";
            _infoPanel.ShowText(BuildItemText(item.Definition, item.Rarity), canSell, sellLabel);
        }

        private string BuildItemText(ItemDefinition definition, ItemRarity rarity)
        {
            PaletteConfig palette = _game.Palette;
            _stringBuilder.Length = 0;
            _stringBuilder.Append("<b><color=#").Append(ColorUtility.ToHtmlStringRGB(palette.GroupColor(definition.Group)))
                .Append('>').Append(definition.DisplayName).Append("</color></b> - <color=#")
                .Append(ColorUtility.ToHtmlStringRGB(palette.RarityColor(rarity))).Append('>').Append(rarity).Append("</color>\n");
            _stringBuilder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(palette.TextDim)).Append('>')
                .Append(GroupName(definition.Group)).Append("</color>\n\n");
            _stringBuilder.Append(definition.Description).Append("\n\n");
            _stringBuilder.Append(definition.DescribeStats(rarity, _game.Ctx));
            return _stringBuilder.ToString();
        }
    }
}
