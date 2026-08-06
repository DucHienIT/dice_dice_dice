# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Required Reading Before Writing Code

**Before writing or modifying any script, read `docs/CODE_RULES.md` first.** It defines the mandatory folder structure (`Assets/Scripts/` layout), C# naming conventions, architecture rules (ScriptableObject-driven balancing data, logic/presentation separation, central phase state machine, object pooling), and Unity-specific do/don'ts. Notably: no runtime bootstrapping — no `AddComponent`/`GetComponent`; every system must exist as an object in the scene or a prefab, with all references wired via `[SerializeField]` in the Inspector. It also mandates performance-first patterns: zero GC allocation in hot paths, manager-ticked updates instead of per-object `Update()`, prewarmed object pools, and split Canvases for uGUI. All first-party code must follow it.

## Project Overview

**DICE DICE DICE!** — a single-screen (1920x1080, side-view) Tower Defense + Merge + Roguelite game being built in Unity. Enemies march right-to-left toward a wall; the player manages an 8-slot item board (2 columns × 4 rows) where Dice generate gold economy and combat items auto-attack.

- **Unity version:** 6000.3.9f1 (Unity 6) — URP, new Input System, uGUI
- **Current state:** template stage — third-party assets are imported but there is **no first-party game code yet**. Game scripts should be created under a new `Assets/Scripts/` (or similar first-party folder), not inside third-party folders.
- **Main scene:** `Assets/Scenes/SampleScene.unity`

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

- `Assets/JMO Assets/` — Toony Colors Pro 2 (stylized/toon shading)
- `Assets/Plugins/Demigiant/` — DOTween + DOTween Pro (tweening; settings in `Assets/Resources/DOTweenSettings.asset`)
- `Assets/Layer Lab/` — GUI Pro packs (FantasyHero, CasualGame) for UI art
- `Assets/TextMesh Pro/` — text rendering

Every asset needs its `.meta` file committed; never edit `.meta` GUIDs by hand. `Library/`, `Temp/`, `Logs/`, `obj/` and solution files are generated — never commit or hand-edit them.
