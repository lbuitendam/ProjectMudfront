# Project Mudfront – Game Design Document (v0.1)

> Working title. Living document: update when decisions change.

## 1. Pitch
A cartoon WW2-ish **team shooter** for **3 teams × 10 players**. You **buy your loadout every life** with money earned from teamplay, while **players with building tools build forward bases** to hold a **moving control zone**. Slapstick, readable and quick to learn.

## 2. Pillars
1. **Readable fun.** Cartoon style, clear silhouettes, forgiving combat.
2. **Every life is a choice.** The buy menu makes each respawn a small decision.
3. **Build and hold.** Engineer bases change how the map plays.
4. **Three-way chaos.** No team is ever safe; there's always a third party.

## 3. Key decisions (updated 2026-11-19)
| Topic | Decision |
|---|---|
| Engine | Unity 6 LTS (URP), C# |
| Art | Blender (scripted models), stylised toon look. See `reference/style-and-scale-guide.md` |
| Setting | Cartoon WW2-ish, **own factions** (no real nations, no copied characters) |
| Teams | 3 teams × ~10 = 30 players. Later goal 60 |
| Camera | Third and first person, toggle |
| Mode | Moving Control Zone (KOTH) |
| Roles | Equipment-defined roles and persistent unlock tracks; anyone can buy unlocked specialist gear |
| Building | Requires unlocked/purchased building tools; no selected class restriction |
| Economy | Buy per life; persistent account wallet; 10,000 one-time starting balance |
| Vehicles | Ground transport, transport/armed transport helicopters, attack helicopter and flares included in the economy queue |
| Bots | Yes, later (after the core works) |
| Servers | Decide later; build so dedicated + player-hosted are both possible |
| Release | Steam, paid one-off. Cosmetics/DLC maybe later |

## 4. Teams / factions (placeholder)
Three made-up factions, each with its own colour and silhouette:
- **Team Red: "The Ironclads"** (rusty red, helmets with plumes)
- **Team Blue: "The Tidewatch"** (steel blue, caps and coats)
- **Team Green: "The Muddogs"** (olive/mustard, slouch hats)
Names and looks are not final.

## 5. Equipment-defined roles and unlocks
No mandatory class-selection gate. Anyone can use specialist equipment once unlocked and purchased. Medical tools supply larger heals/faster revives; building tools enable construction. Weapons and tools define a player's current role, and a player can combine roles within compatible inventory slots.

Persistent role progression/unlock requirements are data-driven. Initial implementation tracks Assault, Recon, Medical, Support, Driver and Pilot; exact thresholds and fees are tuning defaults. Starter essentials remain available immediately. Every player has a slow basic revive. Old class-specific ability ideas are deferred; do not implement hidden Engineer/Medic restrictions.

## 6. Game mode: Moving Control Zone
- Map ~400×400 m (v1); later larger for 60p.
- One active **zone** (~60–80 m). A team scores per second with the most players inside. Contested = slow points.
- Zone **moves** every X min to a random spot from a list (announced 30 s in advance).
- First to **100 points**, or the most points after 15 min.
- Respawn 5–8 s at the team base or at a friendly completed **FOB**. The smaller Hot Zone is deferred; preserve current capture rules during economy work.

## 7. Economy
- Persistent authoritative account wallet with 10,000 credits granted once. Keep earnings/spending across matches, early leaves, sessions, reconnects and authority restarts. No recurring match/death allowance and no real-money cash purchases.
- Claim functional free starter gear from a friendly trader once per life. Final death removes carried paid gear; a successful revive retains it. Wallet/unlocks persist.
- Earn and count kills, assists, revives, effective healing, qualified passenger delivery, zone participation, construction, repairs, spotting assists, resupply and enemy asset destruction. Revivers/drivers get capped, time-limited bonuses for helped teammates' later kills.
- Physical traders exist at faction HQs and completed friendly FOBs; vehicles require an appropriate delivery pad. Buy guns, finite ammunition, medical tools, armour, grenades, attachments, parachutes, repair tools and supported vehicles. Every enabled product needs real behavior.
- Ground transport and helicopters spawn locked to the buyer. Buyer can unlock to everyone, INCLUDING enemies, or relock. On buyer final death, intact vehicles remain and default to public unlock. Make the death policy configurable to retain ownership/locks later. Purchased wrecks do not respawn for free.
- Aircraft scope: transport helicopter with fuel/damage/repair/refuelling; armed transport with door gunner/ammo; attack helicopter with front gunner/rockets; finite flares with functional countermeasure behavior.
- Server validates cash, XP/unlocks, inventory, transactions, damage, rewards, item use, deliveries and access. Persist acknowledged transactions durably; retries cannot duplicate grants or spending.
- Prices, thresholds, caps, cooldowns and behavior parameters belong in data assets. Seasons, gold, cosmetics and Hot Zone are later.
- Current approved rules: [DECISIONS.md](DECISIONS.md). Implementation queue: [ECONOMY_TASKS.md](ECONOMY_TASKS.md). Research: [reference/wardogs-economy-study.md](reference/wardogs-economy-study.md).

## 8. Base building (equipment-based)
- Build mode: ghost preview → grid snap (1 m) → validity check → server confirms → spawns as a "construction site" → hammering finishes it.
- Paid from the builder's persistent wallet; requires unlocked/purchased building tools, not a selected class.
- Pieces v1: sandbags, wall segment, gate, watchtower, ammo crate, **spawn beacon (FOB)**, MG turret.
- Every piece has HP; can be destroyed/repaired. Limit per player/team.

## 9. Netcode
- **Server-authoritative** (the server decides hits, money, building).
- Library: **Fish-Net** (free, good for 30–60 players). Final choice in phase 1.
- Dedicated server build from day 1 possible (headless).

## 10. Out of scope v1
Seasonal wipes, gold conversion, cosmetics shop, Hot Zone, bots, heavy ground combat vehicles, more than 1 map and ranked. Persistent cash, equipment unlocks and the specified helicopters are now in scope.
