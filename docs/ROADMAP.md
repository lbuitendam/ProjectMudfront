# Roadmap – Project Mudfront

Pace: solo, ~3 sessions/week + AI. Times are rough estimates.

2026-11-19 scope update: the requested economy slice now includes functional bought equipment, physical base shops, ground transport, owner locks and teamplay rewards. Follow [ECONOMY_TASKS.md](ECONOMY_TASKS.md), which interleaves the necessary multiplayer foundation with gameplay implementation. The original week estimates below predate this expansion.

## Phase 0 – Setup (week 1)
- Install Unity Hub + Unity 6 LTS, Blender, VS Code extensions
- Unity project (URP) + Git + LFS
- Folder structure, coding conventions
**Done when:** empty project opens, is in Git.

## Phase 1 – Style test (week 1–2)
- Colour palette + terrain colours
- 5 Blender test models (house, wall, sandbags, tree, crate)
- Toon shader + day lighting
**Done when:** 1 screenshot that feels like "our style".

## Phase 2 – Offline prototype (week 2–4)
- Character controller, 3rd/1st person toggle
- Shooting (hitscan + projectile), health, death, respawn
- Grey-box test map
**Done when:** walking and shooting at dummies is fun.

## Phase 3 – Core systems offline (week 4–8)
- 4 classes + abilities (data-driven)
- Buy menu + money
- Engineer building (ghost, grid, HP, repair, FOB spawn)
- Control zone + score + match flow
**Done when:** a full round is playable solo (with dummies).

## Phase 4 – Multiplayer (week 8–14)
- Fish-Net, server-authoritative
- Sync movement, combat, money, building, zone
- 3 teams, lobby/server list (simple), dedicated server build
**Done when:** 6+ people play a round together without breakage.

## Phase 5 – First real map + art (month 4–6)
- 1 full map 400×400 m, zone locations
- Characters (3 factions × 4 classes), Mixamo animations
- UI/HUD, sounds, effects
**Done when:** closed playtest with friends.

## Phase 6 – Polish & content (month 6–10)
- Balance, performance (30p), 2nd map, bots
- Settings, menus, tutorial
**Done when:** Steam Playtest / Early Access-ready.

## Later
Aircraft/heavy vehicles, 60 players, progression, cosmetics, more maps, official servers. Purchased ground transport is now part of the economy slice.
