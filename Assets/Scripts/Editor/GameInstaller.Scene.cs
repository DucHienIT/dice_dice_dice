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
            public ModalView Modal;
            public UpgradeChoiceView[] Choices = new UpgradeChoiceView[3];
            public BannerView Banner;
            public ToastView Toast;
            public FloatingTextManager FloatingText;
            public RectTransform WorldUiRoot;
            public Image DragGhost;
            public Camera Camera;
            public ScreenFitter Fitter;
            public RectTransform[] SafeAreaRects = new RectTransform[3];
        }

        /// <summary>
        /// Mobile-first layout numbers for the 1920x1080 landscape design box. Touch targets are >= 88px
        /// tall here, which is ~5mm on a 5.5" phone held sideways - the smallest comfortable tap size.
        /// </summary>
        private static class Ui
        {
            public const float SlotSize = 128f;          // board slot art; pitch = GameConfig.SlotStep * 100
            public const float TopBarHeight = 108f;
            public const float ButtonSmall = 96f;        // reroll / lock / sell
            public const float ButtonLarge = 112f;       // start wave / modal action
            public const float BodyText = 24f;
            public const float LabelText = 21f;
        }

        [MenuItem("Tools/DICE DICE DICE/Build Scene Only")]
        public static void BuildScene()
        {
            ConfigureWorldArt();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveOldRoots(scene, "GameSystems", "WorldVisuals", "UICanvas", "EventSystem");

            var refs = new SceneRefs();
            refs.Camera = SetupCamera(scene);
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

        private static Camera SetupCamera(Scene scene)
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
            camera.orthographicSize = ScreenFitter.DesignHalfHeight; // ScreenFitter widens it on tall phones
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette != null ? Palette.BackgroundTop : new Color(0.09f, 0.11f, 0.18f);
            return camera;
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
            refs.Fitter = NewSystem<ScreenFitter>(root, "ScreenFitter");

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
            // Expand mirrors ScreenFitter's camera rule: the design box always fits whole, so
            // 1 world unit stays exactly 100 canvas px on every phone aspect (board slots depend on it).
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            refs.Ui = canvasGo.AddComponent<UIController>();

            RectTransform boardCanvas = NestedCanvas(canvasGo.transform, "BoardCanvas", 10);
            RectTransform shopCanvas = NestedCanvas(canvasGo.transform, "ShopCanvas", 20);
            RectTransform hudCanvas = NestedCanvas(canvasGo.transform, "HUDCanvas", 30);
            RectTransform popupCanvas = NestedCanvas(canvasGo.transform, "PopupCanvas", 40);
            RectTransform modalCanvas = NestedCanvas(canvasGo.transform, "ModalCanvas", 50);

            // Edge-anchored chrome lives inside a safe-area rect so notches and rounded corners
            // never cover it. The board is NOT inset - it must stay aligned with the world.
            refs.SafeAreaRects[0] = SafeAreaRect(shopCanvas);
            refs.SafeAreaRects[1] = SafeAreaRect(hudCanvas);
            refs.SafeAreaRects[2] = SafeAreaRect(modalCanvas);

            BuildBoardUi(refs, boardCanvas);
            BuildShopUi(refs, refs.SafeAreaRects[0]);
            BuildHud(refs, refs.SafeAreaRects[1]);
            BuildPopupUi(refs, popupCanvas);
            BuildModal(refs, modalCanvas, refs.SafeAreaRects[2]);
            BalanceSlicedBorders(canvasGo.transform, 1f);

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

        private static RectTransform SafeAreaRect(RectTransform parent)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Stretch(rect);
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
                SetRect(rect, Half, Half, Half, world * 100f, new Vector2(Ui.SlotSize, Ui.SlotSize));

                Image frame = slotGo.GetComponent<Image>();
                frame.sprite = skin.ItemFrameEmpty;
                Image icon = CreateImage(rect, "Icon", Color.white, false);
                SetRect(icon.rectTransform, Half, Half, Half, new Vector2(0f, 3f), new Vector2(80f, 80f));
                icon.preserveAspect = true;
                // No rarity caption: the frame sprite already colours the slot by rarity.
                Image dot = CreateImage(rect, "GroupDot", Color.white, false);
                SetRect(dot.rectTransform, TopRight, TopRight, TopRight, new Vector2(-12f, -12f), new Vector2(16f, 16f));
                Image progressBack = CreateSpriteImage(rect, "ProgressBack", progressBackSprite, false, true);
                SetRect(progressBack.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 11f), new Vector2(-26f, 14f));
                Image progressFill = CreateSpriteImage(progressBack.rectTransform, "ProgressFill", progressFillSprite, false, true);
                SetInset(progressFill.rectTransform, 3f);
                MakeFilled(progressFill);
                TMP_Text face = CreateTmp(rect, "Face", string.Empty, 62f, TextAlignmentOptions.Center, true);
                Stretch(face.rectTransform);
                face.fontStyle = FontStyles.Bold;
                face.color = Color.white;
                Image selection = CreateSpriteImage(rect, "Selection", skin.ItemFrameFocus, false, false);
                SetInset(selection.rectTransform, -9f);

                var view = slotGo.GetComponent<BoardSlotView>();
                var so = new SerializedObject(view);
                SetRefProp(so, "_rect", rect);
                SetRefProp(so, "_frame", frame);
                SetRefProp(so, "_icon", icon);
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
            UiSkin skin = Skin;

            // Same shape as the level-up screen: a title ribbon, three cards in a row, actions underneath.
            var panelGo = new GameObject("ShopPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(ShopPanelView));
            panelGo.transform.SetParent(parent, false);
            var rect = panelGo.GetComponent<RectTransform>();
            Stretch(rect);

            Image titleFlag = TitleFlag(rect, LoadSprite(FhComponents + "Title/Title_Flag_01_Red.Png"), 420f);
            SetRect(titleFlag.rectTransform, Half, Half, Half, new Vector2(0f, 322f), titleFlag.rectTransform.sizeDelta);
            TMP_Text title = CreateTmp(titleFlag.rectTransform, "Title", "SHOP", 42f, TextAlignmentOptions.Center, true);
            SetRect(title.rectTransform, Half, Half, Half, new Vector2(0f, 8f), new Vector2(360f, 56f));
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            for (int i = 0; i < 3; i++)
            {
                var itemGo = new GameObject("ShopItem" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(ShopItemView));
                itemGo.transform.SetParent(rect, false);
                var itemRect = itemGo.GetComponent<RectTransform>();
                SetRect(itemRect, Half, Half, Half, new Vector2((i - 1) * 350f, 20f), new Vector2(330f, 420f));
                Image itemBg = itemGo.GetComponent<Image>();
                ApplySprite(itemBg, skin.CardBg(ItemGroup.Economy), true);
                var button = itemGo.GetComponent<Button>();
                button.targetGraphic = itemBg;
                Image itemBorder = CreateSpriteImage(itemRect, "Border", skin.CardBorder(ItemGroup.Economy), false, true);
                Stretch(itemBorder.rectTransform);

                Image icon = CreateImage(itemRect, "Icon", Color.white, false);
                SetRect(icon.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -34f), new Vector2(132f, 132f));
                icon.preserveAspect = true;
                TMP_Text name = CreateTmp(itemRect, "Name", string.Empty, 34f, TextAlignmentOptions.Center, true);
                SetRect(name.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -176f), new Vector2(300f, 48f));
                name.fontStyle = FontStyles.Bold;
                name.color = Color.white;
                TMP_Text desc = CreateTmp(itemRect, "Desc", string.Empty, Ui.BodyText, TextAlignmentOptions.Top, true);
                SetRect(desc.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -230f), new Vector2(288f, 110f));
                desc.color = new Color(0.96f, 0.94f, 0.88f);
                Image priceCoin = CreateSpriteImage(itemRect, "PriceCoin", skin.CoinIcon, false, false);
                SetRect(priceCoin.rectTransform, BottomCenter, BottomCenter, BottomCenter, new Vector2(-42f, 34f), new Vector2(38f, 38f));
                priceCoin.preserveAspect = true;
                TMP_Text price = CreateTmp(itemRect, "Price", string.Empty, 38f, TextAlignmentOptions.Left, true);
                SetRect(price.rectTransform, BottomCenter, BottomCenter, BottomCenter, new Vector2(30f, 34f), new Vector2(90f, 40f));
                price.fontStyle = FontStyles.Bold;
                price.color = new Color(1f, 0.85f, 0.4f);

                var view = itemGo.GetComponent<ShopItemView>();
                var so = new SerializedObject(view);
                SetRefProp(so, "_button", button);
                SetRefProp(so, "_background", itemBg);
                SetRefProp(so, "_border", itemBorder);
                SetRefProp(so, "_icon", icon);
                SetRefProp(so, "_nameLabel", name);
                SetRefProp(so, "_descriptionLabel", desc);
                SetRefProp(so, "_priceLabel", price);
                SetRefProp(so, "_group", itemGo.GetComponent<CanvasGroup>());
                so.ApplyModifiedPropertiesWithoutUndo();
                refs.ShopItems[i] = view;
            }

            Button reroll = CreateButton(rect, "RerollButton", out TMP_Text rerollLabel, out Image rerollBg, "Reroll (2g)", 27f);
            SetRect(((RectTransform)reroll.transform), Half, Half, Half, new Vector2(-470f, -300f), new Vector2(240f, Ui.ButtonSmall));
            ApplySprite(rerollBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Sky.Png"), true);
            Button lockButton = CreateButton(rect, "LockButton", out TMP_Text lockLabel, out Image lockBg, "Lock", 27f);
            SetRect(((RectTransform)lockButton.transform), Half, Half, Half, new Vector2(470f, -300f), new Vector2(240f, Ui.ButtonSmall));
            ApplySprite(lockBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Dark.Png"), true);
            Button start = CreateButton(rect, "StartWaveButton", out TMP_Text startLabel, out Image startBg, "Start Wave 1", 36f);
            SetRect(((RectTransform)start.transform), Half, Half, Half, new Vector2(0f, -300f), new Vector2(468f, Ui.ButtonLarge));
            ApplySprite(startBg, LoadSprite(FhComponents + "Button/Button_01_Mian_l_Bg_Green.png"), true);

            // Sell sits next to the board and only appears for a selected item.
            Button sell = CreateButton(parent, "SellButton", out TMP_Text sellLabel, out Image sellBg, "Sell", 29f);
            SetRect(((RectTransform)sell.transform), BottomLeft, BottomLeft, BottomLeft, new Vector2(300f, 40f), new Vector2(300f, Ui.ButtonSmall));
            ApplySprite(sellBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Orange.Png"), true);
            sell.gameObject.SetActive(false);

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
            SetRefProp(panelSo, "_sellButton", sell);
            SetRefProp(panelSo, "_sellLabel", sellLabel);
            panelSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHud(SceneRefs refs, RectTransform parent)
        {
            Sprite barBg = LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Bg.png");
            Sprite barBorder = LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Border.png");

            var hudGo = new GameObject("HUD", typeof(RectTransform), typeof(HUDView));
            hudGo.transform.SetParent(parent, false);
            Stretch((RectTransform)hudGo.transform);

            // No panel behind the HUD - the readouts float over the scene, so nothing splits the screen.
            var barGo = new GameObject("TopBar", typeof(RectTransform));
            barGo.transform.SetParent(hudGo.transform, false);
            var bar = (RectTransform)barGo.transform;
            SetRect(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, Ui.TopBarHeight));

            Image goldCoin = CreateSpriteImage(bar, "Coin", Skin.CoinIcon, false, false);
            SetRect(goldCoin.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(36f, 0f), new Vector2(44f, 44f));
            goldCoin.preserveAspect = true;
            TMP_Text gold = CreateTmp(bar, "GoldLabel", "0", 38f, TextAlignmentOptions.Left, true);
            SetRect(gold.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(88f, 0f), new Vector2(150f, 46f));
            gold.fontStyle = FontStyles.Bold;
            gold.color = new Color(1f, 0.87f, 0.45f);

            Image hpBack = CreateSpriteImage(bar, "HpBarBack", barBg, false, true);
            SetRect(hpBack.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(258f, 0f), new Vector2(300f, 40f));
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
            TMP_Text hpText = CreateTmp(hpBack.rectTransform, "HpText", string.Empty, Ui.BodyText, TextAlignmentOptions.Center, true);
            Stretch(hpText.rectTransform);
            hpText.fontStyle = FontStyles.Bold;
            hpText.color = Color.white;

            TMP_Text level = CreateTmp(bar, "LevelLabel", "Lv.1", 33f, TextAlignmentOptions.Left, true);
            SetRect(level.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(582f, 0f), new Vector2(96f, 46f));
            level.fontStyle = FontStyles.Bold;
            level.color = HudText;

            Image xpBack = CreateSpriteImage(bar, "XpBarBack", barBg, false, true);
            SetRect(xpBack.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(682f, 0f), new Vector2(224f, 34f));
            Image xpFill = CreateSpriteImage(xpBack.rectTransform, "XpFill", LoadSprite(FhComponents + "Slider/Slider_Border_Tapered_01_Fill_Purple.png"), false, true);
            SetInset(xpFill.rectTransform, 3f);
            MakeFilled(xpFill);
            Image xpBorder = CreateSpriteImage(xpBack.rectTransform, "BarBorder", barBorder, false, true);
            Stretch(xpBorder.rectTransform);
            TMP_Text xpText = CreateTmp(xpBack.rectTransform, "XpText", string.Empty, Ui.LabelText, TextAlignmentOptions.Center, true);
            Stretch(xpText.rectTransform);
            xpText.fontStyle = FontStyles.Bold;
            xpText.color = Color.white;

            TMP_Text wave = CreateTmp(bar, "WaveLabel", "Wave 0/10", 35f, TextAlignmentOptions.Left, true);
            SetRect(wave.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(940f, 0f), new Vector2(240f, 46f));
            wave.fontStyle = FontStyles.Bold;
            wave.color = HudText;

            TMP_Text phase = CreateTmp(bar, "PhaseLabel", "SHOPPING PHASE", 26f, TextAlignmentOptions.Left, true);
            SetRect(phase.rectTransform, LeftCenter, LeftCenter, LeftCenter, new Vector2(1190f, 0f), new Vector2(340f, 46f));
            phase.fontStyle = FontStyles.Bold;
            phase.color = HudText;

            Button mute = CreateButton(bar, "MuteButton", out TMP_Text muteLabel, out Image muteBg, string.Empty, Ui.LabelText);
            SetRect(((RectTransform)mute.transform), RightCenter, RightCenter, RightCenter, new Vector2(-24f, 0f), new Vector2(88f, 88f));
            ApplySprite(muteBg, LoadSprite(FhComponents + "Button/Button_01_Mian_s_Bg_Dark.Png"), true);
            Object.DestroyImmediate(muteLabel.gameObject);
            Image muteIcon = CreateSpriteImage((RectTransform)mute.transform, "Icon", Skin.SoundOnIcon, false, false);
            SetRect(muteIcon.rectTransform, Half, Half, Half, new Vector2(0f, 2f), new Vector2(42f, 42f));
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
            TMP_Text bannerLabel = CreateTmp((RectTransform)bannerGo.transform, "Label", string.Empty, 66f, TextAlignmentOptions.Center, true);
            SetRect(((RectTransform)bannerGo.transform), TopCenter, TopCenter, TopCenter, new Vector2(0f, -230f), new Vector2(1200f, 100f));
            Stretch(bannerLabel.rectTransform);
            bannerLabel.fontStyle = FontStyles.Bold;
            bannerLabel.color = new Color(1f, 1f, 1f, 0f);
            refs.Banner = bannerGo.GetComponent<BannerView>();
            var bannerSo = new SerializedObject(refs.Banner);
            SetRefProp(bannerSo, "_label", bannerLabel);
            bannerSo.ApplyModifiedPropertiesWithoutUndo();

            var toastGo = new GameObject("Toast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(ToastView));
            toastGo.transform.SetParent(parent, false);
            SetRect(((RectTransform)toastGo.transform), TopCenter, TopCenter, TopCenter, new Vector2(0f, -132f), new Vector2(760f, 84f));
            Image toastBg = toastGo.GetComponent<Image>();
            // A clean 9-slice frame - BubbleFrame's borders are taller than any toast, so it always squashed.
            ApplySprite(toastBg, LoadSprite(FhComponents + "Frame/BaseFrame_Border_Rectangle_H60_Bg.png"), true);
            toastBg.color = new Color(0.35f, 0.3f, 0.26f, 0.97f);
            toastBg.raycastTarget = false;
            TMP_Text toastLabel = CreateTmp((RectTransform)toastGo.transform, "Label", string.Empty, 28f, TextAlignmentOptions.Center, true);
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
            SetRect(refs.DragGhost.rectTransform, Half, Half, Half, Vector2.zero, new Vector2(96f, 96f));
            refs.DragGhost.preserveAspect = true;
            refs.DragGhost.enabled = false;
        }

        private static void BuildModal(SceneRefs refs, RectTransform parent, RectTransform safeArea)
        {
            refs.Modal = parent.gameObject.AddComponent<ModalView>();
            UiSkin skin = Skin;

            var rootGo = new GameObject("ModalRoot", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var root = (RectTransform)rootGo.transform;
            Stretch(root);

            // The dim covers the whole screen (outside the safe area too); the content stays inside it.
            Image dim = CreateImage(root, "Dim", new Color(0.03f, 0.03f, 0.05f, 0.88f), true);
            Stretch(dim.rectTransform);
            safeArea.SetParent(root, false);
            RectTransform content = safeArea;

            Image titleFlag = TitleFlag(content, LoadSprite(FhComponents + "Title/Title_Flag_01_Purple.Png"), 660f);
            SetRect(titleFlag.rectTransform, Half, Half, Half, new Vector2(0f, 344f), titleFlag.rectTransform.sizeDelta);
            TMP_Text title = CreateTmp(titleFlag.rectTransform, "Title", string.Empty, 48f, TextAlignmentOptions.Center, true);
            SetRect(title.rectTransform, Half, Half, Half, new Vector2(0f, 10f), new Vector2(600f, 66f));
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            TMP_Text subtitle = CreateTmp(content, "Subtitle", string.Empty, 26f, TextAlignmentOptions.Center, true);
            SetRect(subtitle.rectTransform, Half, Half, Half, new Vector2(0f, 248f), new Vector2(1000f, 42f));
            subtitle.color = new Color(0.78f, 0.74f, 0.68f);

            RectTransform choicesRoot = CenterRect(content, "ChoicesRoot");
            choicesRoot.anchoredPosition = new Vector2(0f, 10f);
            for (int i = 0; i < 3; i++)
            {
                var choiceGo = new GameObject("Choice" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UpgradeChoiceView));
                choiceGo.transform.SetParent(choicesRoot, false);
                var rect = (RectTransform)choiceGo.transform;
                SetRect(rect, Half, Half, Half, new Vector2((i - 1) * 350f, 0f), new Vector2(330f, 420f));
                Image bg = choiceGo.GetComponent<Image>();
                ApplySprite(bg, skin.CardBg(ItemGroup.Economy), true);
                var button = choiceGo.GetComponent<Button>();
                button.targetGraphic = bg;
                Image cardBorder = CreateSpriteImage(rect, "CardBorder", skin.CardBorder(ItemGroup.Economy), false, true);
                Stretch(cardBorder.rectTransform);

                TMP_Text group = CreateTmp(rect, "Group", string.Empty, 22f, TextAlignmentOptions.Center, true);
                SetRect(group.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -28f), new Vector2(290f, 32f));
                group.fontStyle = FontStyles.Bold;
                group.color = new Color(1f, 1f, 1f, 0.85f);
                TMP_Text name = CreateTmp(rect, "Name", string.Empty, 33f, TextAlignmentOptions.Center, true);
                SetRect(name.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -66f), new Vector2(300f, 84f));
                name.fontStyle = FontStyles.Bold;
                name.color = Color.white;
                TMP_Text desc = CreateTmp(rect, "Desc", string.Empty, Ui.BodyText, TextAlignmentOptions.Top, true);
                SetRect(desc.rectTransform, TopCenter, TopCenter, TopCenter, new Vector2(0f, -156f), new Vector2(272f, 226f));
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

            Image statsBg = CreateSpriteImage(content, "StatsPanel", LoadSprite(FhComponents + "Popup/Popup_List_VerticalLayout_Bg.png"), false, true);
            SetRect(statsBg.rectTransform, Half, Half, Half, new Vector2(0f, -30f), new Vector2(880f, 500f));
            Image statsBorder = CreateSpriteImage(statsBg.rectTransform, "Border", LoadSprite(FhComponents + "Popup/Popup_List_VerticalLayout_Border.png"), false, true);
            Stretch(statsBorder.rectTransform);
            TMP_Text statsBody = CreateTmp(statsBg.rectTransform, "Body", string.Empty, 26f, TextAlignmentOptions.TopLeft, true);
            SetInset(statsBody.rectTransform, 38f);
            statsBody.richText = true;
            statsBody.lineSpacing = 10f;
            // The intro rules text and the end-of-run stats have very different lengths - let TMP fit them.
            statsBody.enableAutoSizing = true;
            statsBody.fontSizeMin = 19f;
            statsBody.fontSizeMax = 26f;

            Button action = CreateButton(content, "ActionButton", out TMP_Text actionLabel, out Image actionBg, string.Empty, 38f);
            SetRect(((RectTransform)action.transform), Half, Half, Half, new Vector2(0f, -350f), new Vector2(440f, Ui.ButtonLarge));
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
                ("_audio", refs.Audio), ("_wallView", refs.WallView), ("_ui", refs.Ui),
                ("_screenFitter", refs.Fitter));
            WireArray(refs.Game, "_startingItems", new Object[] { FindItem(itemPool, "Dice"), FindItem(itemPool, "Bow") });

            Wire(refs.Fitter, ("_camera", refs.Camera));
            WireArray(refs.Fitter, "_safeAreaRects", refs.SafeAreaRects);
            Wire(refs.Board, ("_game", refs.Game), ("_economy", refs.Economy), ("_audio", refs.Audio));
            Wire(refs.Economy, ("_config", config));
            Wire(refs.Shop,
                ("_config", config), ("_game", refs.Game), ("_economy", refs.Economy),
                ("_board", refs.Board), ("_audio", refs.Audio));
            WireArray(refs.Shop, "_itemPool", itemPool);
            Wire(refs.Enemies,
                ("_config", config), ("_enemyParent", refs.EnemyPool),
                ("_effects", refs.Effects), ("_audio", refs.Audio));
            WireArray(refs.Enemies, "_enemyTypes", LoadEnemyPool());
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
                ("_arenaSprite", LoadSprite(DungeonArenaPath)), ("_wallSprite", LoadSprite(DungeonWallPath)),
                ("_wallBody", refs.WallBody), ("_wallHitFlash", refs.WallHitFlash), ("_ground", refs.Ground),
                ("_boardBackdrop", refs.BoardBackdrop), ("_shieldGlow", refs.ShieldGlow),
                ("_crackLow", refs.CrackLow), ("_crackHigh", refs.CrackHigh));
            Wire(refs.FloatingText,
                ("_config", config), ("_palette", palette), ("_prefab", floatingTextPrefab),
                ("_poolParent", refs.WorldUiRoot), ("_goldTarget", refs.GoldLabel.rectTransform));
            Wire(refs.Ui,
                ("_hud", refs.Hud), ("_shopPanel", refs.ShopPanel),
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
                refs.Ui, refs.Hud, refs.ShopPanel, refs.Modal, refs.Banner, refs.Toast,
                refs.FloatingText, refs.Fitter
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

        /// <summary>HUD labels sit straight on the dark field now, so they must be light.</summary>
        private static readonly Color HudText = new Color(0.96f, 0.94f, 0.88f);

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

        /// <summary>
        /// Title ribbons are drawn as one piece (their 9-slice border spans the whole sprite height),
        /// so they are scaled uniformly instead of stretched - stretching squashed the ribbon art.
        /// </summary>
        private static Image TitleFlag(RectTransform parent, Sprite sprite, float width)
        {
            Image image = CreateImage(parent, "TitleFlag", Color.white, false);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            float aspect = sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
            image.rectTransform.sizeDelta = new Vector2(width, width * aspect);
            return image;
        }

        /// <summary>
        /// Layer Lab 9-slice art is authored for tall shapes (the main button sprite is 68x178 with an
        /// 89px vertical border). Drawn 1:1 on a short widget, the caps eat the whole rect and Unity
        /// squeezes them - the button reads as one fat bevel with no face. Raising
        /// <see cref="Image.pixelsPerUnitMultiplier"/> shrinks the border so a real middle stays visible.
        /// Fully stretched images (border overlays) inherit their frame's multiplier so the two still line up.
        /// </summary>
        private const float SliceBorderBudget = 0.62f;

        private static void BalanceSlicedBorders(Transform node, float inherited)
        {
            float current = inherited;
            var image = node.GetComponent<Image>();
            if (image != null && image.sprite != null && image.type == Image.Type.Sliced)
            {
                current = SliceMultiplier(image, inherited);
                image.pixelsPerUnitMultiplier = current;
            }
            for (int i = 0; i < node.childCount; i++)
            {
                BalanceSlicedBorders(node.GetChild(i), current);
            }
        }

        private static float SliceMultiplier(Image image, float inherited)
        {
            Vector4 border = image.sprite.border;
            RectTransform rect = image.rectTransform;
            float multiplier = 1f;
            bool measured = false;
            // Only fixed-size axes can pinch the border; stretched axes follow the parent and are always wide.
            if (Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x) && rect.sizeDelta.x > 1f && border.x + border.z > 0f)
            {
                multiplier = Mathf.Max(multiplier, (border.x + border.z) / (rect.sizeDelta.x * SliceBorderBudget));
                measured = true;
            }
            if (Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y) && rect.sizeDelta.y > 1f && border.y + border.w > 0f)
            {
                multiplier = Mathf.Max(multiplier, (border.y + border.w) / (rect.sizeDelta.y * SliceBorderBudget));
                measured = true;
            }
            return measured ? multiplier : inherited;
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
            tmp.font = DisplayFont; // single font for every text in the game
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

