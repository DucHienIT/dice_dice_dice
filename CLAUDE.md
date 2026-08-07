# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Required Reading Before Writing Code

**Before writing or modifying any script, read `docs/CODE_RULES.md` first.** It defines the mandatory folder structure (`Assets/Scripts/` layout), C# naming conventions, architecture rules (ScriptableObject-driven balancing data, logic/presentation separation, central phase state machine, object pooling), and Unity-specific do/don'ts. Notably: no runtime bootstrapping — no `AddComponent`/`GetComponent`; every system must exist as an object in the scene or a prefab, with all references wired via `[SerializeField]` in the Inspector. It also mandates performance-first patterns: zero GC allocation in hot paths, manager-ticked updates instead of per-object `Update()`, prewarmed object pools, and split Canvases for uGUI. All first-party code must follow it.

## Project Overview

**DICE DICE DICE!** — a single-screen (1920x1080, side-view) Tower Defense + Merge + Roguelite game being built in Unity. Enemies march right-to-left toward a wall; the player manages an 8-slot item board (2 columns × 4 rows) where Dice generate gold economy and combat items auto-attack.

- **Unity version:** 6000.3.9f1 (Unity 6) — URP, new Input System, uGUI
- **Current state:** MVP implemented and playable — full 10-wave run (shop → wave → level-up → boss → win/lose), 11 items, 9 enemy types, 22 roguelike upgrades. Verified end-to-end via Unity MCP play-test (win reached, 0 console errors).
- **Main scene:** `Assets/Scenes/SampleScene.unity` (in Build Settings)

## Code Map (first-party)

All runtime code is in `Assets/Scripts/` under namespace `DiceDiceDice`, following `docs/CODE_RULES.md`:

- `Core/` — `GameManager` (central phase state machine + the single `Update()` that ticks every system in fixed order), `BaseWall` (HP/shield), `WallView` (world visuals + shake), `GamePhase`, `RunStats`.
- `Board/` — `BoardModel` (pure logic, 8 slots as `const`), `BoardController` (move/merge/sell with phase rules), `BoardMoveResult`.
- `Items/` — `ItemInstance`, `ItemTicker` (dice roll timers — wave-only, progress carries over — and combat cooldowns; fires events for UI), enums.
- `Combat/` — `CombatContext` (bundle passed to item `Fire`), `ProjectileManager`/`Projectile` (pooled, list-based hit tests, no physics), `EffectManager`/`VisualEffect`/`LightningBolt` (pooled), `AuraService` (Anvil/Hourglass auras, recomputed on board change).
- `Enemies/` — `Enemy` (pooled, no own Update; one prefab per enemy type in `Assets/Prefabs/Enemies/`, each embedding a FantasyMonsters `Monster` view that `Enemy` drives via `SetState` Walk/Run/Idle + `Attack`), `EnemyManager` (single tick loop, per-type pools keyed by `EnemyDefinition`, targeting API, damage/resist/armor/DoT), `WaveSpawner`.
- `Economy/` — `EconomyController` (gold/XP/level events). `Shop/` — `ShopController` (buy/reroll/lock, weighted dice frequency). `Roguelike/` — `UpgradeSystem` (applies `UpgradeDefinition` stat+op+value to `RunModifiers`).
- `Data/` — ScriptableObject classes: `GameConfig` (all balance knobs + world layout), `PaletteConfig` (all colors), `UiSkin` (state-dependent Layer Lab sprites: slot frame per rarity, upgrade card per group, mute icons, coin), `ItemDefinition` hierarchy (each combat item = own class with `Fire()` override — add item #12 by adding a class + asset, never a switch; `IconSprite` holds the authored Layer Lab icon), `EnemyDefinition`, `WaveSet`, `UpgradeDefinition`.
- `UI/` — `UIController` (sole glue: subscribes to system events, feeds dumb views), `HUDView`, `BoardSlotView` (uGUI drag/drop + DOTween roll/merge feedback), `ShopPanelView`/`ShopItemView`, `InfoPanelView`, `ModalView` (intro/level-up/game-over), `BannerView`, `ToastView`, `FloatingTextManager`/`FloatingText` (pooled gold/CRIT popups).
- `Utils/` — `ObjectPool<T>`, `SpriteFactory` (ALL sprites procedural, white + tinted, 64px = 1 world unit), `AudioSynth`+`AudioManager` (all SFX/ambient synthesized in memory), `SaveSystem` (PlayerPrefs, `DDD_` prefix), `RarityMath` (2^tier).
- `Editor/` — `GameInstaller` (partial, 2 files): menu **Tools ▸ DICE DICE DICE ▸ Install All** rebuilds `Assets/Data/*` balance assets, `Assets/Prefabs/*` and the whole scene idempotently (assets updated in place, GUIDs stable; scene roots `GameSystems`/`WorldVisuals`/`UICanvas`/`EventSystem` are deleted and rebuilt; wiring is verified and logs errors for any null ref).

Key conventions established:
- Balance data lives in `Assets/Data/` assets; edit values there (or via `GameInstaller` for bulk changes — it overwrites data assets on re-run).
- World↔UI mapping: camera ortho size 5.4 at (0,0), canvas 1920x1080 ⇒ 1 world unit = 100 canvas px; board slot world positions come from `GameConfig.SlotWorldPosition`.
- **Mobile-first landscape.** The 1920x1080 box is a *design box*, not the screen: `ScreenFitter` (ticked by `GameManager`) grows the camera on any other aspect and the CanvasScaler runs in **Expand** mode, so the box always fits whole and 1 world unit stays exactly 100 canvas px. Edge-anchored chrome (HUD, shop, info, modal content) lives under `SafeArea` rects that `ScreenFitter` insets from `Screen.safeArea`; the board is never inset (it must track the world). Touch targets are >= 88px tall at the 1920x1080 reference and body text >= 17pt.
- 9-slice sizing: Layer Lab button/frame art is authored tall (the main button sprite is 68x178 with an 89px vertical border), so on a short widget the caps eat the whole rect. The installer's `BalanceSlicedBorders` pass sets `Image.pixelsPerUnitMultiplier` on every sliced image so borders take at most ~62% of a fixed axis; sprites whose border spans the full sprite (title ribbons) are drawn `Simple` + `preserveAspect` instead.
- **UI uses the Layer Lab GUI Pro-FantasyHero skin** (frames/buttons/sliders/popups/icons), authored into the scene by the installer; state-swapped sprites live in `Assets/Data/UiSkin.asset`. The Dice icon comes from the GUI Pro-CasualGame pack (same vendor). Layer Lab panels are light parchment, so UI body text defaults to dark brown.
- **HARD RULE — English only:** no Vietnamese anywhere in source code or in-game text (strings, data assets, labels). All game strings must be plain ASCII (no `—`, `·`, `→`, `×` — use `-`, `|`, `->`, `x`), because of the font rule below.
- **HARD RULE — single font:** every TMP text in the game uses exactly one font asset: `LilitaOne-Regular SDF` (Layer Lab GUI Pro-CasualGame) — a rounded casual display face chosen for legibility at small sizes. It is assigned centrally in the installer's `CreateTmp` (`GameInstaller.GameFont`); never assign another font or a per-label variant. Its static atlas is ASCII-only, which is why game strings must be ASCII.
- World visuals (projectiles, effects, dice faces) and all audio remain procedural (`SpriteFactory`/`AudioSynth`); `PaletteConfig` still owns world/rarity/group colors. **Enemies are the exception:** their visuals come from the `Assets/FantasyMonsters` pack — the installer nests one monster prefab per enemy type (mapping in `InstallEnemies`), auto-scales it to the gameplay radius (bounds-fit against `Enemy.BodyVisualScale`; pack art already faces left, the march direction) and pins its `SortingGroup` to order 10. `EnemyDefinition.BodyColor` only tints death-pop effects now.
- Play-test drivers via `mcp__UnityMCP__execute_code`: set `Application.runInBackground = true` before Play; UI buttons can be invoked through `SerializedObject` lookups (see git history of this build for examples).

## The Design Spec Is Authoritative

`docs/DICE_DICE_DICE_Game_Design_Spec.md` (written in Vietnamese) defines all gameplay rules and player experience. **Section 32 lists non-negotiable principles** — read it before implementing or changing any mechanic. The most load-bearing rules:

- Exactly 8 board slots (2 cols × 4 rows), left of the wall; never remove this limit.
- Dice are economy items bought in the Shop: they occupy a slot, roll automatically **only while a wave is active** (roll progress carries over between waves), and generate gold equal to the roll. They never attack and never trigger adjacent items.
- Two identical items of the same rarity merge into one item of the next rarity (Common → Rare → Epic → Legendary), freeing a slot. Merge never produces a random item.
- The game alternates **shopping phase** (Shop open: buy / reroll / lock / sell) and **wave phase** (Shop fully closed). Rearranging and merging on the board is allowed during waves; buying/selling is not.
- Enemies grant XP; leveling up pauses the game and offers a choice of 3 roguelike upgrades.
- Do not invent mechanics that change the core role of Dice.

The spec deliberately excludes technical implementation (architecture, patterns, folder structure) — those decisions are yours, but the gameplay rules are not.

## Working with Unity

The Unity Editor is automated via **Unity MCP** (`mcp__UnityMCP__*` tools — the `com.coplaydev.unity-mcp` package). Use it rather than asking the user to click through the editor:

- Compile-check after script changes: `mcp__UnityMCP__validate_script` and `mcp__UnityMCP__read_console` (check for errors before declaring work done).
- Run tests (Unity Test Framework is installed): `mcp__UnityMCP__run_tests` (edit/play mode).
- Scene/GameObject/prefab/asset manipulation: `manage_scene`, `manage_gameobject`, `manage_prefabs`, `manage_asset`, etc.
- If UnityMCP tools are missing or disconnected, the `unity-mcp-connect` skill diagnoses and fixes the connection (each open editor uses its own port).

There is no CLI build script; builds go through the Unity Editor (or MCP `manage_build`).

## Third-Party Assets (do not modify)

- `Assets/FantasyMonsters/` — animated monster prefabs (all enemy visuals; driven via its `Monster` script API)
- `Assets/JMO Assets/` — Toony Colors Pro 2 (stylized/toon shading)
- `Assets/Plugins/Demigiant/` — DOTween + DOTween Pro (tweening; settings in `Assets/Resources/DOTweenSettings.asset`)
- `Assets/Layer Lab/` — GUI Pro packs (FantasyHero, CasualGame) for UI art
- `Assets/TextMesh Pro/` — text rendering

Every asset needs its `.meta` file committed; never edit `.meta` GUIDs by hand. `Library/`, `Temp/`, `Logs/`, `obj/` and solution files are generated — never commit or hand-edit them.
