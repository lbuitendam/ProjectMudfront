# Tasks – Project Mudfront

Legend: **[FREE]** = suitable for a free/cheap model · **[CLAUDE/GPT]** = strong model · **[YOU]** = manual (install, click, test)
Status: `[ ]` todo · `[~]` busy · `[x]` done. Work top-down per phase.

## Active requested work — Economy (2026-11-19)

Read [START_HERE.md](START_HERE.md), [DECISIONS.md](DECISIONS.md), then execute [ECONOMY_TASKS.md](ECONOMY_TASKS.md) in document/dependency order. Existing status: original ECON-01–03 complete, ECON-04 in progress. Complete migration gates ECON-03R/03A before accepting ECON-04; do not restart the baseline or overwrite active work. Added letter-suffixed tasks preserve existing IDs.

The queue is the execution source for persistent cash, equipment-based roles/unlocks, working gear, HQ/FOB traders, public vehicle unlocking, support bonuses and transport/armed/attack helicopters. Existing generic tasks below are backlog links, not permission to create duplicate systems. Reconcile overlapping weapon, health, construction, FOB, progression and networking completion at handoff. Original phase estimates predate the expanded scope.

## Phase 0 – Setup
- [ ] [YOU] Install Unity Hub + Unity 6 LTS (+ Windows Build Support)
- [ ] [YOU] Install Blender
- [ ] [YOU] VS Code: C# Dev Kit + Unity extension
- [ ] [YOU] Create Unity ID account
- [ ] [YOU] Create a new URP 3D project in `G:\dev\ProjectMudfront\Mudfront`
- [x] [CLAUDE/GPT] Unity `.gitignore` / `.gitattributes` (LFS) + folder structure under `Assets/_Mudfront`
- [ ] [CLAUDE/GPT] Code conventions + assembly definitions
- [ ] [YOU] Install Git LFS, first commit

## Phase 1 – Style test
- [x] [CLAUDE/GPT] Colour palette (factions + terrain) → `art/palette/`
- [x] [CLAUDE/GPT] Blender script: shared style helpers (chunky bevel, crooked, flat colours)
- [~] [CLAUDE/GPT] Blender: house, wall, sandbags, tree, crate → FBX
- [ ] [CLAUDE/GPT] URP toon shader + day lighting preset
- [ ] [FREE] Write the import guide (how FBX goes into Unity)
- [ ] [YOU] Screenshot + feedback on the style

## Phase 2 – Offline prototype
- [ ] [CLAUDE/GPT] Player controller (move, jump, sprint, crouch)
- [ ] [CLAUDE/GPT] Camera: 3rd/1st person toggle
- [ ] [CLAUDE/GPT] Weapon system (hitscan + projectile, ammo, reload)
- [ ] [CLAUDE/GPT] Health/damage/death/respawn
- [ ] [FREE] Target dummy script (takes damage, shows numbers)
- [ ] [FREE] Simple HUD (health, ammo)
- [ ] [CLAUDE/GPT] Grey-box test map
- [ ] [YOU] Test: is walking and shooting fun?

## Phase 3 – Core systems
- [ ] [CLAUDE/GPT] Data architecture: role/unlock data, `ItemData`, equipment behavior data, `BuildableData` (ScriptableObjects)
- [ ] [FREE] Fill data: role tracks, weapons, gadgets, unlock requirements and prices (follow the example format)
- [ ] [CLAUDE/GPT] Equipment ability system (cooldowns, effects; no selected-class gates)
- [ ] [FREE] Ability text/tooltips
- [ ] [CLAUDE/GPT] Money system (earn/spend, events)
- [ ] [CLAUDE/GPT] Buy menu UI + logic
- [ ] [CLAUDE/GPT] Building: ghost, grid snap, validity, construction site, HP, repair
- [ ] [CLAUDE/GPT] Blender: buildables (sandbags, wall, gate, tower, crate, beacon, turret)
- [ ] [CLAUDE/GPT] FOB spawn beacon
- [ ] [CLAUDE/GPT] Control zone: scoring, contested, move, announcement
- [ ] [CLAUDE/GPT] Match flow: warmup → play → end → reset
- [ ] [FREE] Scoreboard UI
- [ ] [YOU] Play a full round solo

## Phase 4 – Multiplayer
- [ ] [CLAUDE/GPT] Fish-Net setup, choice confirmed
- [ ] [CLAUDE/GPT] Network movement + lag compensation for hits
- [ ] [CLAUDE/GPT] Sync money/buy/building/zone (server-authoritative)
- [ ] [CLAUDE/GPT] 3-team logic, team balance
- [ ] [CLAUDE/GPT] Dedicated server build (headless)
- [ ] [FREE] Simple server list UI
- [ ] [YOU] Playtest with friends

## Phase 5+
See ROADMAP.md. Add tasks once phase 4 is done.

## Ideas (parked)
- Heavy ground combat vehicles · bots · 60 players · seasons/gold · cosmetics · Hot Zone · night/snow map variants
