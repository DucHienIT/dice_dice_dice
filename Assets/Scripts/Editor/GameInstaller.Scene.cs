using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceDiceDice.EditorTools
{
    public static partial class GameInstaller
    {
        /// <summary>All scene components collected during the build for wiring + verification.</summary>
        private class SceneRefs
        {
            public GameManager Game;
            public BoardController Board;
            public EconomyController Economy;
            public ShopController Shop;
            public EnemyManager Enemies;
            public Transform EnemyPool;
            public ProjectileManager Projectiles;
            public Transform ProjectilePool;
            public EffectManager Effects;
            public Transform EffectPool;
            public WaveSpawner Spawner;
            public ItemTicker Ticker;
            public AuraService Auras;
            public UpgradeSystem Upgrades;
            public BaseWall Wall;
            public AudioManager Audio;
            public AudioSource SfxSource;
            public AudioSource MusicSource;
            public WallView WallView;
            public SpriteRenderer WallBody, WallHitFlash, Ground, BoardBackdrop, ShieldGlow, CrackLow, CrackHigh;
            public UIController Ui;
            public HUDView Hud;
            public TMP_Text GoldLabel, HpLabel, LevelLabel, XpLabel, WaveLabel, PhaseLabel;
            public Image HpFill, ShieldFill, XpFill, MuteIcon;
            public Button MuteButton;
            public BoardSlotView[] Slots = new BoardSlotView[8];
            public ShopPanelView ShopPanel;
            public ShopItemView[] ShopItems = new ShopItemView[3];
            public InfoPanelView InfoPanel;
            public ModalView Modal;
            public UpgradeChoiceView[] Choices = new UpgradeChoiceView[3];
            public BannerView Banner;
            public ToastView Toast;
            public FloatingTextManager FloatingText;
            public RectTransform WorldUiRoot;
            public Image DragGhost;
        }

        [MenuItem("Tools/DICE DICE DICE/Build Scene Only")]
        public static void BuildScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveOldRoots(scene, "GameSystems", "WorldVisuals", "UICanvas", "EventSystem");
            SetupCamera(scene);

            var refs = new SceneRefs();
            BuildSystems(refs);
            BuildWorldVisuals(refs);
            BuildUi(refs);
            WireEverything(refs);
            VerifyWiring(refs);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EnsureBuildSettings();
            Debug.Log("[Installer] Scene built and saved: " + ScenePath);
        }

        private static void RemoveOldRoots(Scene scene, params string[] names)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    if (roots[i].name == names[n])
                    {
                        Object.DestroyImmediate(roots[i]);
                        break;
                    }
                }
            }
        }

        private static void SetupCamera(Scene scene)
        {
            Camera camera = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                camera = roots[i].GetComponentInChildren<Camera>();
                if (camera != null)
                {
                    break;
                }
            }
            if (camera == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                camera = go.GetComponent<Camera>();
            }
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette != null ? Palette.BackgroundTop : new Color(0.09f, 0.11f, 0.18f);
        }

        // ---------- Systems ----------

        private static void BuildSystems(SceneRefs refs)
        {
            var root = new GameObject("GameSystems");

            refs.Game = NewSystem<GameManager>(root, "GameManager");
            refs.Board = NewSystem<BoardController>(root, "Board");
            refs.Economy = NewSystem<EconomyController>(root, "Economy");
            refs.Shop = NewSystem<ShopController>(root, "Shop");
            refs.Enemies = NewSystem<EnemyManager>(root, "Enemies");
            refs.EnemyPool = NewChild(refs.Enemies.transform, "EnemyPool").transform;
            refs.Projectiles = NewSystem<ProjectileManager>(root, "Projectiles");
            refs.ProjectilePool = NewChild(refs.Projectiles.transform, "ProjectilePool").transform;
            refs.Effects = NewSystem<EffectManager>(root, "Effects");
            refs.EffectPool = NewChild(refs.Effects.transform, "EffectPool").transform;
            refs.Spawner = NewSystem<WaveSpawner>(root, "WaveSpawner");
            refs.Ticker = NewSystem<ItemTicker>(root, "ItemTicker");
            refs.Auras = NewSystem<AuraService>(root, "Auras");
            refs.Upgrades = NewSystem<UpgradeSystem>(root, "Upgrades");
            refs.Wall = NewSystem<BaseWall>(root, "Wall");

            GameObject audioGo = NewChild(root.transform, "Audio");
            refs.Audio = audioGo.AddComponent<AudioManager>();
            refs.SfxSource = audioGo.AddComponent<AudioSource>();
            refs.SfxSource.playOnAwake = false;
            refs.MusicSource = audioGo.AddComponent<AudioSource>();
            refs.MusicSource.playOnAwake = false;
        }

        private static T NewSystem<T>(GameObject root, string name) where T : Component
        {
            GameObject go = NewChild(root.transform, name);
            return go.AddComponent<T>();
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        // ---------- World visuals ----------

        private static void BuildWorldVisuals(SceneRefs refs)
        {
            var root = new GameObject("WorldVisuals");
            refs.WallView = root.AddComponent<WallView>();

            refs.BoardBackdrop = WorldSprite(root.transform, "BoardBackdrop", 1);
            refs.Ground = WorldSprite(root.transform, "Ground", 2);
            refs.ShieldGlow = WorldSprite(root.transform, "ShieldGlow", 4);

            GameObject wallRoot = NewChild(root.transform, "WallRoot");
            refs.WallBody = WorldSprite(wallRoot.transform, "WallBody", 5);
            refs.WallHitFlash = WorldSprite(wallRoot.transform, "WallHitFlash", 6);
            refs.WallBody.transform.localPosition = Vector3.zero;
            wallRoot.transform.position = new Vector3(Config.WallCenterX, 0f, 0f);

            refs.CrackLow = WorldSprite(root.transform, "CrackLow", 7);
            refs.CrackHigh = WorldSprite(root.transform, "CrackHigh", 7);
        }

        private static SpriteRenderer WorldSprite(Transform parent, string name, int sortingOrder)
        {
            GameObject go = NewChild(parent, name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        // ---------- UI ----------

        private static void BuildUi(SceneRefs refs)
        {
            var canvasGo = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            refs.Ui = canvasGo.AddComponent<UIController>();

            RectTransform boardCanvas = NestedCanvas(canvasGo.transform, "BoardCanvas", 10);
            RectTransform shopCanvas = NestedCanvas(canvasGo.transform, "ShopCanvas", 20);
            RectTransform hudCanvas = NestedCanvas(canvasGo.transform, "HUDCanvas", 30);
            RectTransform popupCanvas = NestedCanvas(canvasGo.transform, "PopupCanvas", 40);
            RectTransform modalCanvas = NestedCanvas(canvasGo.transform, "ModalCanvas", 50);

            BuildBoardUi(refs, boardCanvas);
            BuildShopUi(refs, shopCanvas);
            BuildInfoPanel(refs, shopCanvas);
            BuildHud(refs, hudCanvas);
            BuildPopupUi(refs, popupCanvas);
            BuildModal(refs, modalCanvas);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            _ = eventSystem;
        }

        private static RectTransform NestedCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            return rect;
        }

        private static void BuildBoardUi(SceneRefs refs, RectTransform parent)
        {
            UiSkin skin = Skin;
            Sprite progressBackSprite = LoadSprite(FhComponents + "Slider/Slider_Border_Rectangle_01_Bg.png");
            Sprite progressFillSprite = LoadSprite(FhComponents + "Slider/Slider_Border_Rectangle_01_Fill_White.png");

            RectTransform boardRoot = CenterRect(parent, "BoardRoot");
            for (int i = 0; i < 8; i++)
            {
                Vector2 world = Config.SlotWorldPosition(i);
                var slotGo = new GameObject("Slot" + i, typeof(RectTransform), typeof(Image), typeof(BoardSlotView));
                slotGo.transform.SetParent(boardRoot, false);
                var rect = slotGo.GetComponent<RectTransform>();
                SetRect(rect, Half, Half, Half, world * 100f, new Vector2(104f, 104f));

                Image frame = slotGo.GetComponent<Image>();
                frame.sprite = skin.ItemFrameEmpty;
                Image icon = CreateImage(rect, "Icon", Color.white, false);
                SetRect(icon.rectTransform, Half, Half, Half, new Vector2(0f, 2f), new Vector2(62f, 62f));
                icon.preserveAspect = true;
                TMP_Text rarity = CreateTmp(rect, "Rarity", string.Empty, 12f, TextAlignmentOptions.TopLeft, true);
                SetRect(rarity.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(10f, -7f), new Vector2(84f, 16f));
                rarity.font = DisplayFont;
                rarity.fontStyle = FontStyles.Bold;
                Image dot = CreateImage(rect, "GroupDot", Color.white, false);
                SetRect(dot.rectTransform, TopRight, TopRight, TopRight, new Vector2(-10f, -10f), new Vector2(12f, 12f));
                Image progressBack = CreateSpriteImage(rect, "ProgressBack", progressBackSprite, false, true);
                SetRect(progressBack.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-22f, 10f));
                Image progressFill = CreateSpriteImage(progressBack.rectTransform, "ProgressFill", progressFillSprite, false, true);
                SetInset(progressFill.rectTransform, 2f);
                MakeFilled(progressFill);
                TMP_Text face = CreateTmp(rect, "Face", string.Empty, 44f, TextAlignmentOptions.Center, true);
                Stretch(face.rectTransform);
                face.font = DisplayFontOutlined;
                face.fontStyle = FontStyles.Bold;
                face.color = Color.white;
                Image selection = CreateSpriteImage(rect, "Selection", skin.ItemFrameFocus, false, false);
                SetInset(selection.rectTransform, -7f);

                var view = slotGo.GetComponent<BoardSlotView>();
                var so = new SerializedObject(view);
                SetRefProp(so, "_rect", rect);
                SetRefProp(so, "_frame", frame);
                SetRefProp(so, "_icon", icon);
                SetRefProp(so, "_rarityLabel", rarity);
                SetRefProp(so, "_groupDot", dot);
                SetRefProp(so, "_progressBack", progressBack);
                SetRefProp(so, "_progressFill", progressFill);
                SetRefProp(so, "_faceLabel", face);
                SetRefProp(so, "_selectionRing", selection);
                so.ApplyModifiedPropertiesWithoutUndo();
                refs.Slots[i] = view;
            }
        }

        private static void BuildShopUi(SceneRefs refs, RectTransform parent)
        {
            Sprite popupBg = LoadSprite(FhComponents + "Popup/Popup_Box_Bg.png");
            Sprite popupBorder = LoadSprite(FhComponents + "Popup/Popup_Box_Border.png");
            Sprite rowBg = LoadSprite(FhComponents + "Frame/ListFrame_01_Bg.png");
            Sprite rowBorder = LoadSprite(FhComponents + "Frame/ListFrame_01_Border.png");

            var panelGo = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(ShopPanelView));
            panelGo.transform.SetParent(parent, false);
            var rect = panelGo.GetComponent<RectTransform>();
            SetRect(rect, TopRight, TopRight, TopRight, new Vector2(-28f, -100f), new Vector2(430f, 570f));
            Image panelBg = panelGo.GetComponent<Image>();
            ApplySprite(panelBg, popupBg, true);
            Image panelBorder = CreateSpriteImage(rect, "Border", popupBorder, false, true);
            Stretch(panelBorder.rectTransform);

            Image titleFlag = CreateSpriteImage(rect, "TitleFlag", LoadSprite(FhComponents + "Title/Title_Flag_01_Red.Png"), false, true);
            SetRect(titleFlag.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, 24f), new Vector2(280f, 62f));
            TMP_Text title = CreateTmp(titleFlag.rectTransform, "Title", "SHOP", 27f, TextAlignmentOptions.Center, true);
            SetRect(title.rectTransform, Half, Half, Half, new Vector2(0f, 5f), new Vector2(240f, 34f));
            title.font = DisplayFont;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            TMP_Text subtitle = CreateTmp(rect, "Subtitle", "Mua sắm trước khi vào wave", 13f, TextAlignmentOptions.Center, true);
            SetRect(subtitle.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -40f), new Vector2(360f, 18f));
            subtitle.color = new Color(0.62f, 0.58f, 0.52f);

            for (int i = 0; i < 3; i++)
            {
                var itemGo = new GameObject("ShopItem" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(ShopItemView));
                itemGo.transform.SetParent(rect, false);
                var itemRect = itemGo.GetComponent<RectTransform>();
                SetRect(itemRect, TopCenter, TopCenter, TopCenter, new Vector2(0f, -70f - i * 114f), new Vector2(390f, 106f));
                Image itemBg = itemGo.GetComponent<Image>();
                ApplySprite(itemBg, rowBg, true);
                var button = itemGo.GetComponent<Button>();
                button.targetGraphic = itemBg;
                Image itemBorder = CreateSpriteImage(itemRect, "Border", rowBorder, false, true);
                Stretch(itemBorder.rectTransform);

                Image icon = CreateImage(itemRect, "Icon", Color.white, false);
                SetRect(icon.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(14f, 0f), new Vector2(58f, 58f));
                icon.preserveAspect = true;
                TMP_Text name = CreateTmp(itemRect, "Name", string.Empty, 18f, TextAlignmentOptions.TopLeft, true);
                SetRect(name.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(84f, -10f), new Vector2(200f, 24f));
                name.font = DisplayFont;
                name.fontStyle = FontStyles.Bold;
                TMP_Text tag = CreateTmp(itemRect, "Tag", string.Empty, 11f, TextAlignmentOptions.TopLeft, true);
                SetRect(tag.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(84f, -33f), new Vector2(220f, 16f));
                tag.color = new Color(0.62f, 0.58f, 0.52f);
                TMP_Text desc = CreateTmp(itemRect, "Desc", string.Empty, 11f, TextAlignmentOptions.TopLeft, true);
                SetRect(desc.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(84f, -50f), new Vector2(296f, 52f));
                desc.color = new Color(0.42f, 0.36f, 0.30f);
                Image priceCoin = CreateSpriteImage(itemRect, "PriceCoin", Skin.CoinIcon, false, false);
                SetRect(priceCoin.rectTransform, TopRight, TopRight, TopRight, new Vector2(-64f, -10f), new Vector2(22f, 22f));
                priceCoin.preserveAspect = true;
                TMP_Text price = CreateTmp(itemRect, "Price", string.Empty, 19f, TextAlignmentOptions.TopRight, true);
                SetRect(price.rectTransform, TopRight, TopRight, TopRight, new Vector2(-12f, -9f), new Vector2(50f, 24f));
                price.font = DisplayFont;
                price.fontStyle = FontStyles.Bold;
                price.color = new Color(1f, 0.85f, 0.4f);

                var view = itemGo.GetComponent<ShopItemView>();
                var so = new SerializedObject(view);
                SetRefProp(so, "_button", button);
                SetRefProp(so, "_background", itemBg);
                SetRefProp(so, "_icon", icon);
                SetRefProp(so, "_nameLabel", name);
                SetRefProp(so, "_tagLabel", tag);
                SetRefProp(so, "_descriptionLabel", desc);
                SetRefProp(so, "_priceLabel", price);
                SetRefProp(so, "_group", itemGo.GetComponent<CanvasGroup>());
                so.ApplyModifiedPropertiesWithoutUndo();
                refs.ShopItems[i] = view;
            }

            Button reroll = CreateButton(rect, "RerollButton", out TMP_Text rerollLabel, out Image rerollBg, "Reroll (2 vàng)", 15f);
            SetRect(((RectTransform)reroll.transform), TopLeft, TopLeft, TopLeft, new Vector2(20f, -420f), new Vector2(188f, 50f));
            ApplySprite(rerollBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Sky.Png"), true);
            Button lockButton = CreateButton(rect, "LockButton", out TMP_Text lockLabel, out Image lockBg, "Khóa", 15f);
            SetRect(((RectTransform)lockButton.transform), TopRight, TopRight, TopRight, new Vector2(-20f, -420f), new Vector2(188f, 50f));
            ApplySprite(lockBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Dark.Png"), true);
            Button start = CreateButton(rect, "StartWaveButton", out TMP_Text startLabel, out Image startBg, "Bắt đầu Wave 1", 20f);
            SetRect(((RectTransform)start.transform), TopCenter, TopCenter, TopCenter, new Vector2(0f, -482f), new Vector2(392f, 64f));
            ApplySprite(startBg, LoadSprite(FhComponents + "Button/Button_01_Mian_l_Bg_Green.png"), true);

            refs.ShopPanel = panelGo.GetComponent<ShopPanelView>();
            var panelSo = new SerializedObject(refs.ShopPanel);
            SetRefProp(panelSo, "_group", panelGo.GetComponent<CanvasGroup>());
            SetArrayProp(panelSo, "_items", refs.ShopItems);
            SetRefProp(panelSo, "_rerollButton", reroll);
            SetRefProp(panelSo, "_rerollLabel", rerollLabel);
            SetRefProp(panelSo, "_lockButton", lockButton);
            SetRefProp(panelSo, "_lockLabel", lockLabel);
            SetRefProp(panelSo, "_lockBackground", lockBg);
            SetRefProp(panelSo, "_startWaveButton", start);
            SetRefProp(panelSo, "_startWaveLabel", startLabel);
            panelSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildInfoPanel(SceneRefs refs, RectTransform parent)
        {
            var panelGo = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image), typeof(InfoPanelView));
            panelGo.transform.SetParent(parent, false);
            var rect = panelGo.GetComponent<RectTransform>();
            SetRect(rect, BottomLeft, BottomLeft, BottomLeft, new Vector2(24f, 20f), new Vector2(360f, 320f));
            Image bg = panelGo.GetComponent<Image>();
            ApplySprite(bg, LoadSprite(FhComponents + "Popup/Popup_List_VerticalLayout_Bg.png"), true);
            Image border = CreateSpriteImage(rect, "Border", LoadSprite(FhComponents + "Popup/Popup_List_VerticalLayout_Border.png"), false, true);
            Stretch(border.rectTransform);

            TMP_Text title = CreateTmp(rect, "Title", "THÔNG TIN", 12f, TextAlignmentOptions.TopLeft, true);
            SetRect(title.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(18f, -14f), new Vector2(200f, 18f));
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.62f, 0.58f, 0.52f);

            TMP_Text body = CreateTmp(rect, "Body", string.Empty, 13f, TextAlignmentOptions.TopLeft, true);
            SetRect(body.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(18f, -38f), new Vector2(324f, 212f));
            body.richText = true;

            Button sell = CreateButton(rect, "SellButton", out TMP_Text sellLabel, out Image sellBg, "Bán", 15f);
            SetRect(((RectTransform)sell.transform), BottomCenter, BottomCenter, BottomCenter, new Vector2(0f, 14f), new Vector2(320f, 48f));
            ApplySprite(sellBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Orange.Png"), true);

            refs.InfoPanel = panelGo.GetComponent<InfoPanelView>();
            var so = new SerializedObject(refs.InfoPanel);
            SetRefProp(so, "_body", body);
            SetRefProp(so, "_sellButton", sell);
            SetRefProp(so, "_sellLabel", sellLabel);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHud(SceneRefs refs, RectTransform parent)
        {
            Sprite barBg = LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Bg.png");
            Sprite barBorder = LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Border.png");

            var hudGo = new GameObject("HUD", typeof(RectTransform), typeof(HUDView));
            hudGo.transform.SetParent(parent, false);
            Stretch((RectTransform)hudGo.transform);

            Image topBar = CreateSpriteImage((RectTransform)hudGo.transform, "TopBar",
                LoadSprite(FhComponents + "Frame/BaseFrame_Border_Rectangle_H60_Bg.png"), false, true);
            SetRect(topBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(12f, 70f));
            RectTransform bar = topBar.rectTransform;
            Image topBarBorder = CreateSpriteImage(bar, "Border", LoadSprite(FhComponents + "Frame/BaseFrame_Border_Rectangle_H60_Border.png"), false, true);
            Stretch(topBarBorder.rectTransform);

            Image goldPill = CreateSpriteImage(bar, "GoldPill", LoadSprite(FhComponents + "Frame/BaseFrame_Basic_Rectangle_H40_Bg.png"), false, true);
            SetRect(goldPill.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(20f, -3f), new Vector2(150f, 40f));
            Image goldCoin = CreateSpriteImage(goldPill.rectTransform, "Coin", Skin.CoinIcon, false, false);
            SetRect(goldCoin.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(8f, 0f), new Vector2(28f, 28f));
            goldCoin.preserveAspect = true;
            TMP_Text gold = CreateTmp(goldPill.rectTransform, "GoldLabel", "0", 24f, TextAlignmentOptions.Left, true);
            SetRect(gold.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(44f, 0f), new Vector2(100f, 36f));
            gold.font = DisplayFont;
            gold.fontStyle = FontStyles.Bold;
            gold.color = new Color(1f, 0.87f, 0.45f);

            Image hpBack = CreateSpriteImage(bar, "HpBarBack", barBg, false, true);
            SetRect(hpBack.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(190f, -3f), new Vector2(230f, 26f));
            Image hpFill = CreateSpriteImage(hpBack.rectTransform, "HpFill", LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Fill_Red.png"), false, true);
            SetInset(hpFill.rectTransform, 3f);
            MakeFilled(hpFill);
            Image shieldFill = CreateSpriteImage(hpBack.rectTransform, "ShieldFill", LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Fill_Mint.png"), false, true);
            SetInset(shieldFill.rectTransform, 3f);
            MakeFilled(shieldFill);
            shieldFill.fillAmount = 0f;
            shieldFill.color = new Color(1f, 1f, 1f, 0.85f);
            Image hpBorder = CreateSpriteImage(hpBack.rectTransform, "BarBorder", barBorder, false, true);
            Stretch(hpBorder.rectTransform);
            TMP_Text hpText = CreateTmp(hpBack.rectTransform, "HpText", string.Empty, 13f, TextAlignmentOptions.Center, true);
            Stretch(hpText.rectTransform);
            hpText.font = DisplayFontOutlined;
            hpText.fontStyle = FontStyles.Bold;
            hpText.color = Color.white;

            TMP_Text level = CreateTmp(bar, "LevelLabel", "Lv.1", 20f, TextAlignmentOptions.Left, true);
            SetRect(level.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(438f, -3f), new Vector2(74f, 40f));
            level.font = DisplayFont;
            level.fontStyle = FontStyles.Bold;

            Image xpBack = CreateSpriteImage(bar, "XpBarBack", barBg, false, true);
            SetRect(xpBack.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(516f, -3f), new Vector2(170f, 22f));
            Image xpFill = CreateSpriteImage(xpBack.rectTransform, "XpFill", LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Fill_Purple.png"), false, true);
            SetInset(xpFill.rectTransform, 3f);
            MakeFilled(xpFill);
            Image xpBorder = CreateSpriteImage(xpBack.rectTransform, "BarBorder", barBorder, false, true);
            Stretch(xpBorder.rectTransform);
            TMP_Text xpText = CreateTmp(xpBack.rectTransform, "XpText", string.Empty, 12f, TextAlignmentOptions.Center, true);
            Stretch(xpText.rectTransform);
            xpText.font = DisplayFontOutlined;
            xpText.fontStyle = FontStyles.Bold;
            xpText.color = Color.white;

            TMP_Text wave = CreateTmp(bar, "WaveLabel", "Wave 0/10", 24f, TextAlignmentOptions.Left, true);
            SetRect(wave.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(716f, -3f), new Vector2(190f, 40f));
            wave.font = DisplayFont;
            wave.fontStyle = FontStyles.Bold;

            TMP_Text phase = CreateTmp(bar, "PhaseLabel", "GIAI ĐOẠN MUA SẮM", 15f, TextAlignmentOptions.Left, true);
            SetRect(phase.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(920f, -3f), new Vector2(300f, 40f));
            phase.fontStyle = FontStyles.Bold;

            Button mute = CreateButton(bar, "MuteButton", out TMP_Text muteLabel, out Image muteBg, string.Empty, 13f);
            SetRect(((RectTransform)mute.transform), RightCenter, RightCenter, RightCenter, new Vector2(-22f, -3f), new Vector2(58f, 44f));
            ApplySprite(muteBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Dark.Png"), true);
            Object.DestroyImmediate(muteLabel.gameObject);
            Image muteIcon = CreateSpriteImage((RectTransform)mute.transform, "Icon", Skin.SoundOnIcon, false, false);
            SetRect(muteIcon.rectTransform, Half, Half, Half, new Vector2(0f, 2f), new Vector2(26f, 26f));
            muteIcon.preserveAspect = true;

            refs.Hud = hudGo.GetComponent<HUDView>();
            refs.GoldLabel = gold;
            refs.HpFill = hpFill;
            refs.ShieldFill = shieldFill;
            refs.HpLabel = hpText;
            refs.LevelLabel = level;
            refs.XpFill = xpFill;
            refs.XpLabel = xpText;
            refs.WaveLabel = wave;
            refs.PhaseLabel = phase;
            refs.MuteButton = mute;
            refs.MuteIcon = muteIcon;

            var so = new SerializedObject(refs.Hud);
            SetRefProp(so, "_goldLabel", gold);
            SetRefProp(so, "_hpFill", hpFill);
            SetRefProp(so, "_shieldFill", shieldFill);
            SetRefProp(so, "_hpLabel", hpText);
            SetRefProp(so, "_levelLabel", level);
            SetRefProp(so, "_xpFill", xpFill);
            SetRefProp(so, "_xpLabel", xpText);
            SetRefProp(so, "_waveLabel", wave);
            SetRefProp(so, "_phaseLabel", phase);
            SetRefProp(so, "_muteButton", mute);
            SetRefProp(so, "_muteIcon", muteIcon);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPopupUi(SceneRefs refs, RectTransform parent)
        {
            refs.FloatingText = parent.gameObject.AddComponent<FloatingTextManager>();
            refs.WorldUiRoot = CenterRect(parent, "WorldUiRoot");

            var bannerGo = new GameObject("Banner", typeof(RectTransform), typeof(BannerView));
            bannerGo.transform.SetParent(parent, false);
            TMP_Text bannerLabel = CreateTmp((RectTransform)bannerGo.transform, "Label", string.Empty, 48f, TextAlignmentOptions.Center, true);
            SetRect(((RectTransform)bannerGo.transform), TopCenter, TopCenter, TopCenter, new Vector2(0f, -150f), new Vector2(1200f, 80f));
            Stretch(bannerLabel.rectTransform);
            bannerLabel.fontStyle = FontStyles.Bold;
            bannerLabel.color = new Color(1f, 1f, 1f, 0f);
            refs.Banner = bannerGo.GetComponent<BannerView>();
            var bannerSo = new SerializedObject(refs.Banner);
            SetRefProp(bannerSo, "_label", bannerLabel);
            bannerSo.ApplyModifiedPropertiesWithoutUndo();

            var toastGo = new GameObject("Toast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(ToastView));
            toastGo.transform.SetParent(parent, false);
            SetRect(((RectTransform)toastGo.transform), TopCenter, TopCenter, TopCenter, new Vector2(0f, -86f), new Vector2(640f, 52f));
            Image toastBg = toastGo.GetComponent<Image>();
            ApplySprite(toastBg, LoadSprite(FhComponents + "Frame/BubbleFrame_03_Bg.png"), true);
            toastBg.color = new Color(0.35f, 0.3f, 0.26f, 0.97f);
            toastBg.raycastTarget = false;
            TMP_Text toastLabel = CreateTmp((RectTransform)toastGo.transform, "Label", string.Empty, 15f, TextAlignmentOptions.Center, true);
            Stretch(toastLabel.rectTransform);
            toastLabel.fontStyle = FontStyles.Bold;
            toastLabel.color = Color.white;
            var toastGroup = toastGo.GetComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;
            refs.Toast = toastGo.GetComponent<ToastView>();
            var toastSo = new SerializedObject(refs.Toast);
            SetRefProp(toastSo, "_group", toastGroup);
            SetRefProp(toastSo, "_label", toastLabel);
            toastSo.ApplyModifiedPropertiesWithoutUndo();

            refs.DragGhost = CreateImage(parent, "DragGhost", Color.white, false);
            SetRect(refs.DragGhost.rectTransform, Half, Half, Half, Vector2.zero, new Vector2(64f, 64f));
            refs.DragGhost.preserveAspect = true;
            refs.DragGhost.enabled = false;
        }

        private static void BuildModal(SceneRefs refs, RectTransform parent)
        {
            refs.Modal = parent.gameObject.AddComponent<ModalView>();
            UiSkin skin = Skin;

            var rootGo = new GameObject("ModalRoot", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var root = (RectTransform)rootGo.transform;
            Stretch(root);

            Image dim = CreateImage(root, "Dim", new Color(0.03f, 0.03f, 0.05f, 0.88f), true);
            Stretch(dim.rectTransform);

            Image titleFlag = CreateSpriteImage(root, "TitleFlag", LoadSprite(FhComponents + "Title/Title_Flag_01_Purple.Png"), false, true);
            SetRect(titleFlag.rectTransform, Half, Half, Half, new Vector2(0f, 330f), new Vector2(620f, 90f));
            TMP_Text title = CreateTmp(titleFlag.rectTransform, "Title", string.Empty, 32f, TextAlignmentOptions.Center, true);
            SetRect(title.rectTransform, Half, Half, Half, new Vector2(0f, 8f), new Vector2(560f, 48f));
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            TMP_Text subtitle = CreateTmp(root, "Subtitle", string.Empty, 16f, TextAlignmentOptions.Center, true);
            SetRect(subtitle.rectTransform, Half, Half, Half, new Vector2(0f, 268f), new Vector2(900f, 30f));
            subtitle.color = new Color(0.78f, 0.74f, 0.68f);

            RectTransform choicesRoot = CenterRect(root, "ChoicesRoot");
            choicesRoot.anchoredPosition = new Vector2(0f, 10f);
            for (int i = 0; i < 3; i++)
            {
                var choiceGo = new GameObject("Choice" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UpgradeChoiceView));
                choiceGo.transform.SetParent(choicesRoot, false);
                var rect = (RectTransform)choiceGo.transform;
                SetRect(rect, Half, Half, Half, new Vector2((i - 1) * 310f, 0f), new Vector2(284f, 300f));
                Image bg = choiceGo.GetComponent<Image>();
                ApplySprite(bg, skin.CardBg(ItemGroup.Economy), true);
                var button = choiceGo.GetComponent<Button>();
                button.targetGraphic = bg;
                Image cardBorder = CreateSpriteImage(rect, "CardBorder", skin.CardBorder(ItemGroup.Economy), false, true);
                Stretch(cardBorder.rectTransform);

                TMP_Text group = CreateTmp(rect, "Group", string.Empty, 13f, TextAlignmentOptions.Center, true);
                SetRect(group.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -24f), new Vector2(240f, 20f));
                group.fontStyle = FontStyles.Bold;
                group.color = new Color(1f, 1f, 1f, 0.85f);
                TMP_Text name = CreateTmp(rect, "Name", string.Empty, 20f, TextAlignmentOptions.Center, true);
                SetRect(name.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -52f), new Vector2(250f, 58f));
                name.fontStyle = FontStyles.Bold;
                name.color = Color.white;
                TMP_Text desc = CreateTmp(rect, "Desc", string.Empty, 15f, TextAlignmentOptions.Top, true);
                SetRect(desc.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -120f), new Vector2(232f, 156f));
                desc.color = new Color(0.96f, 0.94f, 0.88f);

                var view = choiceGo.GetComponent<UpgradeChoiceView>();
                var so = new SerializedObject(view);
                SetRefProp(so, "_button", button);
                SetRefProp(so, "_background", bg);
                SetRefProp(so, "_border", cardBorder);
                SetRefProp(so, "_groupLabel", group);
                SetRefProp(so, "_nameLabel", name);
                SetRefProp(so, "_descriptionLabel", desc);
                so.ApplyModifiedPropertiesWithoutUndo();
                refs.Choices[i] = view;
            }

            Image statsBg = CreateSpriteImage(root, "StatsPanel", LoadSprite(FhComponents + "Popup/Popup_List_VerticalLayout_Bg.png"), false, true);
            SetRect(statsBg.rectTransform, Half, Half, Half, new Vector2(0f, -10f), new Vector2(680f, 470f));
            Image statsBorder = CreateSpriteImage(statsBg.rectTransform, "Border", LoadSprite(FhComponents + "Popup/Popup_List_VerticalLayout_Border.png"), false, true);
            Stretch(statsBorder.rectTransform);
            TMP_Text statsBody = CreateTmp(statsBg.rectTransform, "Body", string.Empty, 16f, TextAlignmentOptions.TopLeft, true);
            SetInset(statsBody.rectTransform, 30f);
            statsBody.richText = true;
            statsBody.lineSpacing = 14f;

            Button action = CreateButton(root, "ActionButton", out TMP_Text actionLabel, out Image actionBg, string.Empty, 20f);
            SetRect(((RectTransform)action.transform), Half, Half, Half, new Vector2(0f, -330f), new Vector2(320f, 66f));
            ApplySprite(actionBg, LoadSprite(FhComponents + "Button/Button_01_Mian_l_Bg_Yellow.png"), true);
            actionLabel.color = new Color(0.32f, 0.2f, 0.06f);

            var modalSo = new SerializedObject(refs.Modal);
            SetRefProp(modalSo, "_root", rootGo);
            SetRefProp(modalSo, "_title", title);
            SetRefProp(modalSo, "_subtitle", subtitle);
            SetRefProp(modalSo, "_choicesRoot", choicesRoot.gameObject);
            SetArrayProp(modalSo, "_choices", refs.Choices);
            SetRefProp(modalSo, "_statsBackground", statsBg);
            SetRefProp(modalSo, "_statsBody", statsBody);
            SetRefProp(modalSo, "_actionButton", action);
            SetRefProp(modalSo, "_actionLabel", actionLabel);
            modalSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Wiring ----------

        private static void WireEverything(SceneRefs refs)
        {
            GameConfig config = Config;
            PaletteConfig palette = Palette;
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Enemy.prefab").GetComponent<Enemy>();
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Projectile.prefab").GetComponent<Projectile>();
            var effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/VisualEffect.prefab").GetComponent<VisualEffect>();
            var lightningPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/LightningBolt.prefab").GetComponent<LightningBolt>();
            var floatingTextPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/FloatingText.prefab").GetComponent<FloatingText>();
            ItemDefinition[] itemPool = LoadItemPool();
            UpgradeDefinition[] upgradePool = LoadUpgradePool();
            var waveSet = AssetDatabase.LoadAssetAtPath<WaveSet>(DataRoot + "/Waves/WaveSet.asset");

            Wire(refs.Game,
                ("_config", config), ("_palette", palette), ("_uiSkin", Skin), ("_board", refs.Board), ("_economy", refs.Economy),
                ("_shop", refs.Shop), ("_enemies", refs.Enemies), ("_projectiles", refs.Projectiles),
                ("_effects", refs.Effects), ("_spawner", refs.Spawner), ("_ticker", refs.Ticker),
                ("_auras", refs.Auras), ("_upgrades", refs.Upgrades), ("_wall", refs.Wall),
                ("_audio", refs.Audio), ("_wallView", refs.WallView), ("_ui", refs.Ui));
            WireArray(refs.Game, "_startingItems", new Object[] { FindItem(itemPool, "Dice"), FindItem(itemPool, "Bow") });

            Wire(refs.Board, ("_game", refs.Game), ("_economy", refs.Economy), ("_audio", refs.Audio));
            Wire(refs.Economy, ("_config", config));
            Wire(refs.Shop,
                ("_config", config), ("_game", refs.Game), ("_economy", refs.Economy),
                ("_board", refs.Board), ("_audio", refs.Audio));
            WireArray(refs.Shop, "_itemPool", itemPool);
            Wire(refs.Enemies,
                ("_config", config), ("_enemyPrefab", enemyPrefab), ("_enemyParent", refs.EnemyPool),
                ("_effects", refs.Effects), ("_audio", refs.Audio));
            Wire(refs.Projectiles,
                ("_config", config), ("_projectilePrefab", projectilePrefab),
                ("_projectileParent", refs.ProjectilePool), ("_effects", refs.Effects));
            Wire(refs.Effects,
                ("_config", config), ("_palette", palette), ("_effectPrefab", effectPrefab),
                ("_lightningPrefab", lightningPrefab), ("_effectParent", refs.EffectPool),
                ("_floatingText", refs.FloatingText), ("_wallView", refs.WallView));
            Wire(refs.Spawner, ("_waveSet", waveSet), ("_enemies", refs.Enemies));
            Wire(refs.Ticker,
                ("_config", config), ("_game", refs.Game), ("_board", refs.Board),
                ("_economy", refs.Economy), ("_audio", refs.Audio));
            Wire(refs.Auras, ("_board", refs.Board));
            Wire(refs.Upgrades, ("_wall", refs.Wall), ("_auras", refs.Auras));
            WireArray(refs.Upgrades, "_pool", upgradePool);
            Wire(refs.Wall, ("_config", config));
            Wire(refs.Audio, ("_sfxSource", refs.SfxSource), ("_musicSource", refs.MusicSource));
            Wire(refs.WallView,
                ("_config", config), ("_palette", palette), ("_wall", refs.Wall),
                ("_wallBody", refs.WallBody), ("_wallHitFlash", refs.WallHitFlash), ("_ground", refs.Ground),
                ("_boardBackdrop", refs.BoardBackdrop), ("_shieldGlow", refs.ShieldGlow),
                ("_crackLow", refs.CrackLow), ("_crackHigh", refs.CrackHigh));
            Wire(refs.FloatingText,
                ("_config", config), ("_palette", palette), ("_prefab", floatingTextPrefab),
                ("_poolParent", refs.WorldUiRoot), ("_goldTarget", refs.GoldLabel.rectTransform));
            Wire(refs.Ui,
                ("_hud", refs.Hud), ("_shopPanel", refs.ShopPanel), ("_infoPanel", refs.InfoPanel),
                ("_modal", refs.Modal), ("_banner", refs.Banner), ("_toast", refs.Toast),
                ("_floatingText", refs.FloatingText), ("_dragGhost", refs.DragGhost));
            WireArray(refs.Ui, "_slots", refs.Slots);
        }

        private static ItemDefinition FindItem(ItemDefinition[] pool, string displayName)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null && pool[i].DisplayName == displayName)
                {
                    return pool[i];
                }
            }
            return null;
        }

        private static void Wire(Component component, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(component);
            for (int i = 0; i < fields.Length; i++)
            {
                SetRefProp(so, fields[i].field, fields[i].value);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Component component, string field, System.Collections.IList values)
        {
            var so = new SerializedObject(component);
            SetArrayProp(so, field, values);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefProp(SerializedObject so, string field, Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[Installer] Missing serialized field '" + field + "' on " + so.targetObject.GetType().Name);
                return;
            }
            property.objectReferenceValue = value;
        }

        private static void SetArrayProp(SerializedObject so, string field, System.Collections.IList values)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[Installer] Missing serialized array '" + field + "' on " + so.targetObject.GetType().Name);
                return;
            }
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = (Object)values[i];
            }
        }

        private static void VerifyWiring(SceneRefs refs)
        {
            int missing = 0;
            Component[] targets =
            {
                refs.Game, refs.Board, refs.Economy, refs.Shop, refs.Enemies, refs.Projectiles, refs.Effects,
                refs.Spawner, refs.Ticker, refs.Auras, refs.Upgrades, refs.Wall, refs.Audio, refs.WallView,
                refs.Ui, refs.Hud, refs.ShopPanel, refs.InfoPanel, refs.Modal, refs.Banner, refs.Toast, refs.FloatingText
            };
            for (int t = 0; t < targets.Length; t++)
            {
                var so = new SerializedObject(targets[t]);
                SerializedProperty property = so.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.name != "m_Script" && property.objectReferenceValue == null)
                    {
                        Debug.LogError("[Installer] Unwired reference: " + targets[t].GetType().Name + "." + property.propertyPath);
                        missing++;
                    }
                }
            }
            if (missing == 0)
            {
                Debug.Log("[Installer] Wiring verified: no missing references.");
            }
        }

        private static void EnsureBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    return;
                }
            }
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------- UI helpers ----------

        private static readonly Vector2 Half = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 LeftCenter = new Vector2(0f, 0.5f);
        private static readonly Vector2 RightCenter = new Vector2(1f, 0.5f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Half;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetInset(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Half;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-inset * 2f, -inset * 2f);
        }

        private static RectTransform CenterRect(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            SetRect(rect, Half, Half, Half, Vector2.zero, Vector2.zero);
            return rect;
        }

        private static Image CreateImage(RectTransform parent, string name, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void ApplySprite(Image image, Sprite sprite, bool sliced)
        {
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
        }

        private static Image CreateSpriteImage(RectTransform parent, string name, Sprite sprite, bool raycast, bool sliced)
        {
            Image image = CreateImage(parent, name, Color.white, raycast);
            ApplySprite(image, sprite, sliced);
            return image;
        }

        private static void MakeFilled(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
        }

        private static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, bool raycastOff)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.raycastTarget = !raycastOff;
            tmp.color = new Color(0.30f, 0.25f, 0.20f); // dark default for the light Layer Lab panels
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Button CreateButton(RectTransform parent, string name, out TMP_Text label, out Image background, string text, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            background = go.GetComponent<Image>();
            background.color = new Color(0.15f, 0.17f, 0.25f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            TextMeshProUGUI tmp = CreateTmp((RectTransform)go.transform, "Label", text, fontSize, TextAlignmentOptions.Center, true);
            Stretch(tmp.rectTransform);
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white; // buttons use colored sprites
            label = tmp;
            return button;
        }
    }
}
