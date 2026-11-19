# Project Mudfront – Game Design Document (v0.1)

> Working title. Living document: update when decisions change.

## 1. Pitch
A cartoon WW2-ish **team shooter** for **3 teams × 10 players**. You **buy your loadout every life** with money earned from teamplay, while **Engineers build forward bases** to hold a **moving control zone**. Slapstick, readable and quick to learn.

## 2. Pillars
1. **Readable fun.** Cartoon style, clear silhouettes, forgiving combat.
2. **Every life is a choice.** The buy menu makes each respawn a small decision.
3. **Build and hold.** Engineer bases change how the map plays.
4. **Three-way chaos.** No team is ever safe; there's always a third party.

## 3. Key decisions (2026-11-18)
| Topic | Decision |
|---|---|
| Engine | Unity 6 LTS (URP), C# |
| Art | Blender (scripted models), stylised toon look. See `reference/style-and-scale-guide.md` |
| Setting | Cartoon WW2-ish, **own factions** (no real nations, no copied characters) |
| Teams | 3 teams × ~10 = 30 players. Later goal 60 |
| Camera | Third and first person, toggle |
| Mode | Moving Control Zone (KOTH) |
| Classes | 4: Assault, Scout, Engineer, Medic |
| Building | Engineer only |
| Economy | Buy per life; money resets each match |
| Vehicles | Ground transport included in the economy slice (2026-11-19 request); aircraft/heavy vehicles later |
| Bots | Yes, later (after the core works) |
| Servers | Decide later; build so dedicated + player-hosted are both possible |
| Release | Steam, paid one-off. Cosmetics/DLC maybe later |

## 4. Teams / factions (placeholder)
Three made-up factions, each with its own colour and silhouette:
- **Team Red: "The Ironclads"** (rusty red, helmets with plumes)
- **Team Blue: "The Tidewatch"** (steel blue, caps and coats)
- **Team Green: "The Muddogs"** (olive/mustard, slouch hats)
Names and looks are not final.

## 5. Classes
| Class | Role | Starting weapon | Abilities (cooldown) |
|---|---|---|---|
| **Assault** | Frontline | SMG/rifle | Sprint burst, grenade barrage |
| **Scout** | Flank/recon | Sniper/pistol | Mark targets, short stealth |
| **Engineer** | Building/repair | Shotgun | Build mode, repair, place turret |
| **Medic** | Support | SMG-light | Heal area, revive faster |

Every class can revive (Medic faster). Abilities are data (ScriptableObjects).

## 6. Game mode: Moving Control Zone
- Map ~400×400 m (v1); later larger for 60p.
- One active **zone** (~60–80 m). A team scores per second with the most players inside. Contested = slow points.
- Zone **moves** every X min to a random spot from a list (announced 30 s in advance).
- First to **100 points**, or the most points after 15 min.
- Respawn 5–8 s at the team base or at your own **FOB** (Engineer).

## 7. Economy
- One configurable starting grant per player per match (initial tuning: $1,000); no new grant on death. A functional free rifle/ammo kit is available once each life.
- Earn and count: kills, assists, revives, effective teammate healing, completed passenger transport, zone participation, construction, repairs, spotting assists, resupply and enemy asset destruction. Eligible actions pay extra in the active zone.
- Money is kept during the match, **reset** at the start of the next one.
- Buy at a physical friendly station at each faction HQ; completed friendly FOBs can provide equipment stations. Menu covers class/loadout, weapons, ammo, medical items, utility and supported vehicles. Respawning returns you to a base to equip; a successful revive preserves your current kit.
- Purchases must deliver functional equipment. Initial scope: rifle, sidearm, ammo, bandage, parachute, repair tool and passenger jeep. Unsupported products remain unavailable until implemented.
- Purchased vehicles spawn at a validated delivery pad, locked to their buyer. Only the buyer can unlock for teammates or relock; enemies cannot enter. Destroyed purchased vehicles require another paid purchase.
- Server validates rewards, transactions, inventory, item use, delivery and access permissions. Retries cannot duplicate spending, items or income.
- Everything is data: `ItemData` (id, name, cost, class, category, stats).
- Execution specification and proposed tuning: [ECONOMY_TASKS.md](ECONOMY_TASKS.md). Research and differences from WARDOGS: [reference/wardogs-economy-study.md](reference/wardogs-economy-study.md). Values are playtest defaults; match-only money and four classes remain Mudfront decisions.

## 8. Base building (Engineer)
- Build mode: ghost preview → grid snap (1 m) → validity check → server confirms → spawns as a "construction site" → hammering finishes it.
- Paid from the Engineer's own money.
- Pieces v1: sandbags, wall segment, gate, watchtower, ammo crate, **spawn beacon (FOB)**, MG turret.
- Every piece has HP; can be destroyed/repaired. Limit per player/team.

## 9. Netcode
- **Server-authoritative** (the server decides hits, money, building).
- Library: **Fish-Net** (free, good for 30–60 players). Final choice in phase 1.
- Dedicated server build from day 1 possible (headless).

## 10. Out of scope v1
Aircraft/heavy vehicles, bots, progression/unlocks, cosmetics shop, cross-match cash persistence, more than 1 map, ranked. Ground transport is included in the requested economy slice.
