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

        private static readonly string[] GroupNames = { "Kinh tế", "Vũ khí", "Phép thuật", "Hỗ trợ", "Phòng thủ" };

        private const string DefaultInfoText =
            "<b>Dice tạo vàng</b> — vũ khí và phép thuật tiêu diệt quái!\n\n" +
            "· Kéo item vào ô trống để di chuyển.\n" +
            "· Kéo 2 item <b>cùng loại + cùng phẩm cấp</b> vào nhau để <b>merge</b>.\n" +
            "· Mua sắm giữa các wave, sẵn sàng rồi bấm <b>Bắt đầu Wave</b>.";

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
            PaletteConfig palette = game.Palette;
            _dragGhostRect = _dragGhost.rectTransform;
            _dragGhost.enabled = false;

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Init(i, this, palette);
            }
            _shopPanel.Init(palette);
            for (int i = 0; i < _shopPanel.Items.Length; i++)
            {
                _shopPanel.Items[i].Init(i, this, palette);
            }
            _infoPanel.Init(palette);
            _modal.Init(palette);
            _toast.Init(palette);
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
            _hud.RefreshMute(game.Audio.Muted);
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
                "Bảo vệ <b>tường thành</b> — quái tiến vào từ bên phải.\n\n" +
                "<b>Dice tạo vàng</b> (tự roll trong wave) — <b>vũ khí và phép thuật tiêu diệt quái</b>.\n\n" +
                "<b>Mua sắm giữa các wave</b> — sẵn sàng rồi bấm <b>Bắt đầu Wave</b>. Bảng chỉ có <b>8 ô</b>!\n\n" +
                "Kéo 2 item giống nhau + cùng phẩm cấp vào nhau để <b>merge</b> lên phẩm cấp cao hơn.\n\n" +
                "Diệt quái nhận XP — lên cấp chọn 1 trong 3 nâng cấp roguelike.\n\n" +
                "Sống sót 10 wave và hạ Boss <b>Loaded Golem</b> để chiến thắng!",
                "Vào game", () =>
                {
                    _modal.Hide();
                    onStart();
                });
        }

        public void ShowLevelUp(int level, List<UpgradeDefinition> choices, Action<UpgradeDefinition> onPicked)
        {
            _modal.ShowChoices("LÊN CẤP " + level + "!", "Chọn một nâng cấp roguelike — hiệu lực đến hết run.");
            UpgradeChoiceView[] views = _modal.Choices;
            for (int i = 0; i < views.Length; i++)
            {
                bool used = i < choices.Count;
                views[i].gameObject.SetActive(used);
                if (used)
                {
                    UpgradeDefinition upgrade = choices[i];
                    views[i].Render(upgrade, _game.Palette, GroupName(upgrade.Group), picked =>
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
            _stringBuilder.Append("Wave đạt được: <b>").Append(_game.Wave).Append('/').Append(_game.Config.WaveCount).Append("</b>\n");
            _stringBuilder.Append("Quái tiêu diệt: <b>").Append(stats.Kills).Append("</b>\n");
            _stringBuilder.Append("Vàng từ Dice: <b>").Append(stats.DiceGold).Append("</b>\n");
            _stringBuilder.Append("Số lần roll / roll ra 6: <b>").Append(stats.Rolls).Append(" / ").Append(stats.Sixes).Append("</b>\n");
            _stringBuilder.Append("Item đã mua / merge: <b>").Append(stats.ItemsBought).Append(" / ").Append(stats.Merges).Append("</b>\n");
            _stringBuilder.Append("Sát thương cao nhất: <b>")
                .Append(topSource ?? "—");
            if (topSource != null)
            {
                _stringBuilder.Append(" (").Append(Mathf.RoundToInt(topAmount)).Append(')');
            }
            _stringBuilder.Append("</b>\n");
            _stringBuilder.Append("Nâng cấp đã chọn: ").Append(stats.ChosenUpgrades.Count == 0 ? "—" : string.Join(", ", stats.ChosenUpgrades));

            _modal.ShowInfo(
                win ? "CHIẾN THẮNG!" : "TƯỜNG THÀNH THẤT THỦ",
                win ? "Bạn đã hạ gục Loaded Golem và sống sót qua 10 wave!" : "Máu tường thành đã về 0.",
                _stringBuilder.ToString(),
                "Chơi lại", _game.Restart);
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
            _dragGhost.sprite = SpriteFactory.Icon(item.Definition.Icon);
            _dragGhost.color = _game.Palette.GroupColor(item.Definition.Group);
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
                _toast.Show("Đã đạt phẩm cấp tối đa (Legendary)");
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
                _toast.Show("Đã bán item, ô được giải phóng");
                _selectedSlot = -1;
                RenderBoard();
            }
        }

        private void OnMuteClicked()
        {
            _game.Audio.ToggleMute();
            _hud.RefreshMute(_game.Audio.Muted);
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
            _toast.Show("Merge! " + merged.Definition.DisplayName + " → " + merged.Rarity);
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
                _slots[i].Render(model.Get(i), _game.Palette);
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
            string sellLabel = "Bán (" + item.Definition.SellPrice(item.Rarity, _game.Mods) + " vàng)";
            _infoPanel.ShowText(BuildItemText(item.Definition, item.Rarity), canSell, sellLabel);
        }

        private string BuildItemText(ItemDefinition definition, ItemRarity rarity)
        {
            PaletteConfig palette = _game.Palette;
            _stringBuilder.Length = 0;
            _stringBuilder.Append("<b><color=#").Append(ColorUtility.ToHtmlStringRGB(palette.GroupColor(definition.Group)))
                .Append('>').Append(definition.DisplayName).Append("</color></b> — <color=#")
                .Append(ColorUtility.ToHtmlStringRGB(palette.RarityColor(rarity))).Append('>').Append(rarity).Append("</color>\n");
            _stringBuilder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(palette.TextDim)).Append('>')
                .Append(GroupName(definition.Group)).Append("</color>\n\n");
            _stringBuilder.Append(definition.Description).Append("\n\n");
            _stringBuilder.Append(definition.DescribeStats(rarity, _game.Ctx));
            return _stringBuilder.ToString();
        }
    }
}
