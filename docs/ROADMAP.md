# Roadmap – Project Mudfront

Pace: solo, ~3 sessions/week + AI. Times are rough estimates.

## Active economy roadmap — revision 2 (2026-11-19)

Entry point: [START_HERE.md](START_HERE.md). Product rules: [DECISIONS.md](DECISIONS.md). Executable jobs and live status: [ECONOMY_TASKS.md](ECONOMY_TASKS.md). Work one job at a time in that file's dependency order, including letter-suffixed jobs. Original ECON-01–03 are complete and ECON-04 is in progress; migration gates ECON-03R/03A must pass before accepting ECON-04.

| Milestone | Jobs | Delivered behavior |
|---|---|---|
| Data and persistent authority | 01–06, including 03R/03A | Updated catalog, one-time 10,000 account wallet, durable recovery, Fish-Net, validated combat and inventory |
| Roles and working equipment | 06A–11, including 07A/B/C and 09A | Persistent equipment unlocks; guns/ammo, attachments, armour, frag/smoke, bandages, large medical kit, revive and parachute |
| Traders and ground vehicles | 12–19, including 18A | HQ/FOB-compatible shop framework, atomic purchases, jeep seats, buyer locks/public unlock, configurable death policy, ride/follow-up rewards and repair |
| Aircraft | 19A–19F | Transport flight/fuel/service, door-gun variant, attack helicopter/front gunner/rockets, flares, safe air deliveries and crew attribution |
| FOBs, feedback and lifecycle | 20–23 | Spotting, equipment-based construction, real friendly FOB traders, economy HUD, persistent account/reconnect and round cleanup |
| Release verification | 24–25 | Multi-client abuse/regression checks, aircraft/equipment end-to-end scenarios and measured balance pass |

No Hot Zone, seasonal reset, gold market or cosmetics in this queue. Exact unlock fees, aircraft prices, XP thresholds and flight parameters remain data-driven tuning choices. The expanded work includes persistent backend concerns and aircraft gameplay; the old week/month estimates below are historical, not a delivery estimate for this scope.

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
- Equipment-defined roles, unlock requirements and tool abilities (data-driven)
- Buy menu + money
- Tool-based building (ghost, grid, HP, repair, FOB spawn)
- Control zone + score + match flow
**Done when:** a full round is playable solo (with dummies).

## Phase 4 – Multiplayer (week 8–14)
- Fish-Net, server-authoritative
- Sync movement, combat, money, building, zone
- 3 teams, lobby/server list (simple), dedicated server build
**Done when:** 6+ people play a round together without breakage.

## Phase 5 – First real map + art (month 4–6)
- 1 full map 400×400 m, zone locations
- Characters (3 factions with readable equipment-defined roles), Mixamo animations
- UI/HUD, sounds, effects
**Done when:** closed playtest with friends.

## Phase 6 – Polish & content (month 6–10)
- Balance, performance (30p), 2nd map, bots
- Settings, menus, tutorial
**Done when:** Steam Playtest / Early Access-ready.

## Later
Heavy ground combat vehicles, 60 players, seasons/gold, cosmetics, Hot Zone, more maps and official hosting expansion. Persistent cash, equipment unlocks and specified helicopters belong to the active economy queue.
