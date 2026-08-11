# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Required Reading Before Writing Code

**Before writing or modifying any script, read `docs/CODE_RULES.md` first.** It defines the mandatory folder structure (`Assets/Scripts/` layout), C# naming conventions, architecture rules (ScriptableObject-driven balancing data, logic/presentation separation, central run state machine, object pooling), and Unity-specific do/don'ts. Notably: no runtime bootstrapping — no `AddComponent`/`GetComponent`; every system must exist as an object in the scene or a prefab, with all references wired via `[SerializeField]` in the Inspector. It also mandates performance-first patterns: zero GC allocation in hot paths, manager-ticked updates instead of per-object `Update()`, prewarmed object pools, and split Canvases for uGUI. All first-party code must follow it.

## Project Overview

**Cosmic Critter Quest** — a one-button roguelite auto-battler being built in Unity. The player taps a single ENGAGE button to advance the Star Cycle; each tap rolls a weighted random event (battle, fortune, choice, spring/trap/treasure, sidekick). Battles resolve automatically on a fixed beat; depth comes from build decisions (upgrade cards + sidekick stack) and HP management between fights. 30 rounds per planet, then warp to a new palette and higher difficulty. Death ends the run; best score is saved.

- **Unity version:** 6000.3.9f1 (Unity 6) — URP (2D Renderer), new Input System, uGUI + TMP
- **Current state:** fully playable. All first-party code lives under `Assets/Scripts/` (folders per CODE_RULES: Core/Events/Combat/Enemies/Progression/Sidekicks/Planets/UI/Data/Save/Audio/Utils, editor tooling in `Scripts/Editor/`), namespaces `CCQ.*`.
- **Main scene:** `Assets/Scenes/CosmicCritterQuest.unity` (portrait 1080×1920 — set the Game view to that resolution; `SampleScene.unity` is the old empty template)

## Code Map & Architecture Facts

- **`GameManager`** (`Core/`, execution order −100) is the only hub: state machine `Ready/Traveling/Battling/Choosing/Dead`, ticks every system from its single `Update` (BattleEngine, stage view, floaters, bursts, shaker, background, UI). Pure-logic classes (`BattleEngine`, `EventRoller`, `PeacefulEventResolver`, `EnemyFactory`, `XpSystem`, `SidekickRoster`, `RunState/PlayerState/EnemyState`) have no Unity scene dependencies; views listen to C# events.
- **Config assets** live in `Assets/Data/` (`GameConfig.asset` = every balance knob incl. feel timings; `NarrativeConfig.asset` = the localization **term key** of every flavor line/name pool plus the glyph charset; per-item assets under `Upgrades/`, `Fortunes/`, `Sidekicks/`, `Planets/`). Upgrades/fortunes are data-driven `StatMod[]` interpreted only by `StatModApplier` — a 9th upgrade card is a new `.asset` plus two CSV rows, zero code.
- **All art is procedural but baked at build time, never at runtime.** `Scripts/Editor/Painter.cs` + `Scripts/Editor/CcqSpriteBaker.cs` (editor assembly) paint every sprite and write them as `.png` assets to `Assets/Art/Generated/`; the builder wires them onto prefab `SpriteRenderer`s. Runtime views own zero painting code — they only swap sprites and toggle renderers. The critter is assembled from **layers** (glow / horns / spikes / body / spots / eyes / mouth) instead of one sprite per look combination: `CritterView.Init` picks `_bodySprites[ColorIndex]` + `_eyeSprites[Eyes-1]` and enables the optional layers. Planet backdrops are two parallax layers on their `Planet` asset — `SkyLayer` (static: gradient, nebula, stars, moon) and `GroundLayer` (a seamlessly tiling strip: ground, rocks, lake, flora, drawn as two copies that leap-frog while scrolling). Sidekick orbs live on their `Sidekick` asset (`OrbSprite`). Audio is still synthesized at runtime (`Audio/SfxSynth`, in `AudioManager.Awake`). UI uses GUI Pro-CasualGame sprites/icons + a TMP font asset generated from its LilitaOne TTF (`Assets/Data/UI/LilitaOne SDF.asset`, dynamic atlas). LilitaOne has **no Vietnamese glyphs**, so the builder also generates `RobotoVN SDF` from `Assets/Data/UI/Fonts/Roboto-Bold.ttf` and `LocalizedFontView` (on the `UI` object, wired with every `TextMeshProUGUI`/`TextMeshPro` in the scene) swaps the whole game over to it whenever the language code is in its `_wideCharsetCodes` list. World-space text also swaps its outlined material (`CcqWorldText.mat` ↔ `CcqWorldTextVN.mat`).
- **Builder:** menu **Tools ▸ CCQ ▸ Build Game (Full)** (`Scripts/Editor/CcqGameBuilder.cs`) imports the localization CSV, then regenerates config assets, the baked art in `Assets/Art/Generated/`, prefabs (`Assets/Prefabs/`) and the whole wired scene. Idempotent; it overwrites the scene + content arrays/icons/keys, but leaves numeric tuning on an existing `GameConfig.asset` untouched. Changing a critter color, a planet palette or any painting code requires a rebuild to re-bake the .png. After hand-editing scene objects, know that a rebuild discards those edits — prefer editing prefabs/configs, or extend the builder.
- **Beyond the old HTML5 demo** (kept in `docs/cosmic-critter-quest/` as reference): sidekick #4 forces a swap choice (spec rule the demo skipped), run-stats death screen, settings music/SFX toggles, enrage warning, screen shake/bursts/typewriter text, per-planet synth music.
- **Travel leg (adventure feel, not in the spec doc yet):** ENGAGE no longer fires the event immediately — it enters `Traveling`, where the hero hops in place (`HeroView.SetWalk` on the `Rig` child) while the ground strip scrolls left; the event fires on arrival. Tapping again cuts the leg short. Critters then slide in from off-screen right. Knobs: `GameConfig.TravelDuration/TravelDistance/WalkHopsPerSecond/EnemyEnterDuration/EnemyEnterOffset`.
- Save keys: `ccq_run`, `ccq_best`, `ccq_music`, `ccq_sfx` and `I2 Language` (written by I2, not by `SaveSystem`) — all PlayerPrefs. Speed changes made mid-battle are deliberately not persisted until the next between-events save.
- Play-testing without clicking: drive `GameManager` private members via `mcp__UnityMCP__execute_code` reflection (`OnEngagePressed`, `OnChoicePicked(int)`, fields `_state`, `_run`); set `Application.runInBackground = true` first.

## Localization (English + Vietnamese, I2 Localization)

**`Assets/Localization/CCQ_Localization.csv` is the single source of truth for every player-facing string.** Columns: `Key,English [en],Vietnamese [vi]` — add a language by adding a column (`Japanese [ja]`), nothing else. `Assets/Resources/I2Languages.asset` is a **generated cache** of that CSV; never hand-edit it.

- **Workflow:** edit the CSV → **Tools ▸ CCQ ▸ Localization ▸ Import CSV** (Ctrl+Shift+L). `Import` runs in `Replace` mode, so a key deleted from the CSV disappears from the game, and it logs any language with empty cells. **Export CSV** dumps the asset back out (full I2 `Key,Type,Desc,…` header) for the case where someone edited terms in the I2 window instead. `Build Game (Full)` imports first, so a rebuild never ships stale text.
- **No literal player-facing string may be written in code or in the builder.** Everything goes through `CCQ.Localization`: `Loc.Get(key)` / `Loc.Format(key, args)` / `Loc.Pick(keys)`, keys as constants in `LocKeys`, or — for narrative pools — the key arrays on `NarrativeConfig`. `Loc` is the only file that references `I2.Loc`. A missing key renders as the key itself plus a `[Loc] Missing term:` warning.
- **`LocLine` (key + up to 3 token substitutions) is how console text is stored**, not a finished string: `GameManager._eventLines` holds up to 3 of them (line + suffix + tail, e.g. win text + level-up + planet-cleared) so `OnLanguageChanged` can re-render the *same* sentence in the new language instead of rolling a fresh random one. `EventOutcome.Line/Suffix` carry them out of the resolvers. Banners live ~3 s, so their text is resolved eagerly and is not re-rendered.
- **Language switching** is the 4th button of the settings overlay (`Loc.CycleLanguage`, wraps through the languages in the source). I2 batches the switch by one frame, then `Loc.Changed` fires; `GameManager.OnLanguageChanged` re-applies the font, static captions (`UIController.RefreshStaticText` → `HudView`/`ConsoleView.RefreshStaticText`, `EngageButton.RefreshLabel`), run values, the console line and whichever modal is open. Views that cache a format string for allocation-free `SetText` must re-pull it there.
- **Text length is a real constraint** — the stats-bar captions and the ENGAGE label sit in fixed-size pills, and Vietnamese runs ~20 % longer than English. Check the 1080×1920 Game view after changing them.
- `Localize` components are not used anywhere; every string is set from code. (I2's import did set the `TextMeshPro` scripting define, so they would work if ever needed.)

## The Design Spec Is Authoritative

`docs/COSMIC_CRITTER_QUEST_Game_Design_Spec.md` (written in Vietnamese) defines all gameplay rules, formulas, and tuning direction. Read it before implementing or changing any mechanic. The most load-bearing rules:

- **One button.** Tap ENGAGE → Star Cycle +1 → roll one weighted event. The only other player decision is the occasional 3-card upgrade choice. Do not add extra input mechanics.
- **Event roll** uses `EVENT_WEIGHTS` with an anti-boredom guard: more than `MAX_NONBATTLE_STREAK` (2) consecutive non-battle events forces a battle (effective battle rate ~55-60%).
- **Combat** is turn-based on a `BEAT_MS` beat (hero first, alternating). Damage = `atk × rand(0.85–1.15) − def`, minimum 1. Hero regens 12% maxHP after each won fight. **Enrage**: past 40 beats in one fight, enemy damage +5%/beat — infinite-sustain stalemates must stay impossible.
- **Enemy scaling** is driven by the global difficulty `g = planet×30 + round` (polynomial × 1.015^g exponential); every run must end eventually. Boss every 10th round (HP ×1.8, ATK ×1.15, XP ×3); elites 12% chance.
- **Sidekicks** are passive, max 3 held — picking a 4th always means dropping one. 8 upgrade cards including deliberate trade-off and situational cards.
- **All balance numbers live in the central config** (ScriptableObject, per CODE_RULES) — the spec's `EVENT_WEIGHTS`/`ENRAGE`/`PLAYER`-style names map to config fields. Tune there, never scatter constants in code.
- **Save**: `ccq_run` (written only between events, never mid-fight — reloading mid-fight restores the pre-fight state) and `ccq_best` (total global rounds, overwrite only when higher).

The spec ends with a tuning guide ("Hướng tuning") — when rebalancing, follow those levers (e.g. adjust `ENEMY.hp/atk` first, keep player stats fixed) instead of inventing new ones.

## Working with Unity

The Unity Editor is automated via **Unity MCP** (`mcp__UnityMCP__*` tools — the `com.coplaydev.unity-mcp` package). Use it rather than asking the user to click through the editor:

- Compile-check after script changes: `mcp__UnityMCP__validate_script` and `mcp__UnityMCP__read_console` (check for errors before declaring work done).
- Run tests (Unity Test Framework is installed): `mcp__UnityMCP__run_tests` (edit/play mode).
- Scene/GameObject/prefab/asset manipulation: `manage_scene`, `manage_gameobject`, `manage_prefabs`, `manage_asset`, etc.
- If UnityMCP tools are missing or disconnected, the `unity-mcp-connect` skill diagnoses and fixes the connection (each open editor uses its own port).

## Shipping the WebGL build

**Menu Tools ▸ CCQ ▸ Build WebGL (Release) / (Development) / Build And Run WebGL (Development)** (`Scripts/Editor/CcqWebGLBuilder.cs`) is the only supported way to build. Output goes to `Builds/WebGL/{Release,Development}` (gitignored); the log prints a per-file payload table (that, not total size, is what explains a slow first load). Do not build from the Build Settings window — you would get whatever Player Settings happen to be lying around.

**These Player Settings are owned by the build tool and rewritten on every build — editing them by hand is pointless, the next build overwrites them:**

| Setting | Value |
|---|---|
| `productName` | `Cosmic Critter Quest` (const in `CcqWebGLBuilder`) |
| `WebGL.template` | `PROJECT:CosmicCritterQuestPortrait` |
| `defaultWebScreenWidth/Height` | 1080 × 1920 (the template reads these as `{{{ WIDTH }}}`/`{{{ HEIGHT }}}`) |
| `defaultInterfaceOrientation` + autorotate flags | Portrait only |
| `WebGL.compressionFormat` / `decompressionFallback` | Brotli + fallback for Release, Disabled for Development |
| `WebGL.dataCaching` | on |

To change any of them, edit the constants in `CcqWebGLBuilder.cs`. **`productName` and `companyName` derive the WebGL save path** — changing either after the game is deployed orphans every player's `ccq_run`/`ccq_best`. Settle both before the first public build (`companyName` is still the Unity default `DefaultCompany`).

**Portrait lock has three layers, and all three must agree** — fixing one still leaves other hosts framing the game wrong:

1. `Assets/WebGLTemplates/CosmicCritterQuestPortrait/` sizes the **canvas element** (Unity takes its render target from the element, not the window). Copied from Unity's Default template — the `#if USE_THREADS/USE_WASM/SHOW_DIAGNOSTICS/...` blocks are Unity's preprocessor and must stay.
2. Player Settings orientation — covers a native build of the same scene.
3. `ScreenLockView` (`Scripts/UI/`, wired in the scene by the game builder, ticked from `GameManager.Update`) pillarboxes in-game, covering hosts that ignore 1 and 2: editor Game view, iframes, a default template served by mistake. It sets `camera.rect` (which makes `camera.aspect` the locked value, so `FitCamera`/`ScreenPointToRay` follow for free) and, per Canvas, recomputes the CanvasScaler factor from the **viewport** using the scaler's own authored formula and shrinks the `Frame` child that every screen parents into. Only the wide side gets bars — a window taller than 9:16 is left alone. Verified: on a real 1080×1920 window the factor is exactly 1.0 and Frame covers the screen, i.e. no regression. Turn it off with `ScreenLockView._portraitLock` to fall back to the pre-lock layout.

There is no CLI build script; builds go through the Unity Editor (or MCP `manage_build`).

## Third-Party Assets (do not modify)

- `Assets/JMO Assets/` — Toony Colors Pro 2 (stylized/toon shading)
- `Assets/Plugins/Demigiant/` — DOTween + DOTween Pro (tweening; settings in `Assets/Resources/DOTweenSettings.asset`)
- `Assets/Layer Lab/` — GUI Pro packs (FantasyHero, CasualGame) for UI art
- `Assets/TextMesh Pro/` — text rendering

Every asset needs its `.meta` file committed; never edit `.meta` GUIDs by hand. `Library/`, `Temp/`, `Logs/`, `obj/` and solution files are generated — never commit or hand-edit them.
