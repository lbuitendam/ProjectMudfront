# Tasks – Project Mudfront

Legend: **[FREE]** = suitable for a free/cheap model · **[CLAUDE/GPT]** = strong model · **[YOU]** = manual (install, click, test)
Status: `[ ]` todo · `[~]` busy · `[x]` done. Work top-down per phase.

## Active requested work — Economy (2026-11-19)

Execute [ECONOMY_TASKS.md](ECONOMY_TASKS.md) in order, one task per Claude handoff. It contains 25 tasks with dependencies, implementation scope and acceptance checks. Start at **ECON-01**. All are initially unchecked; this session prepared documentation only.

This queue is the detailed execution source for money, inventory, working equipment, base buy stations, purchased ground vehicles, owner locks, passenger rewards and their multiplayer verification. It expands the original v1 vehicle scope at the user's request. Existing generic money/buy/sync tasks below are satisfied only when their corresponding ECON work is implemented and verified; do not build duplicate systems. Overlapping weapon, health, building, FOB and networking work must be reused and completion reconciled at handoff. Ground transport is no longer a parked idea; aircraft/heavy vehicles remain later work.

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
- [ ] [CLAUDE/GPT] Data architecture: `ClassData`, `ItemData`, `AbilityData`, `BuildableData` (ScriptableObjects)
- [ ] [FREE] Fill data: 4 classes, ~12 weapons, gadgets, prices (follow the example format)
- [ ] [CLAUDE/GPT] Ability system (cooldowns, effects)
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
- Aircraft/heavy vehicles · bots · 60 players · progression · cosmetics · night/snow map variants
