# Reference study: BFH "Seaside Skirmish", 16-player

Read-only, in-memory study of `F:\RisingHUB\game\mods\bfheroes\Levels\seaside_skirmish\` (`Info/*.desc`, `server.zip → Heightdata.con, StaticObjects.con, GameModes/*`). **Nothing extracted or copied; no placements are reused.** Same method as `map-study-village.md`.

## Terrain (exact)
| Value | Data |
|---|---|
| Primary heightmap | 513×513 at 2 m → **1024 × 1024 m** (2× Village) |
| Height scale | 0.00305 × 16-bit → **~200 m** max range |
| Outer ring | 129×129 tiles at 8 m spacing (backdrop) |
| Object heights | y 99.5 – 160.6 m → **~60 m relief** in play |
| Dressed area | x −498…491, z −383…450 → ~990 × 830 m (includes far backdrop dressing) |
| Static objects | 734 |

## Modes on this map
Team Deathmatch, Capture the Flag, Hold the Hill, Tutorial (plus an `sl` mode with objects only). All 16 players.

## Hold the Hill layout (exact positions → derived)
| Item | Value |
|---|---|
| HQ A (high side) | ~(−130, 80), **y 147.6** |
| HQ B (low side) | ~(258, −135), **y 127.5** |
| HQ ↔ HQ | **~445 m** |
| Objectives | "Lighthouse" ~(4, −129) y 136 · "Field" ~(73, −48) y 135 |
| Objective ↔ objective | **~106 m** |
| HQ A → objectives | ~248 m / ~240 m |
| HQ B → objectives | ~254 m / ~204 m |
| Height advantage | HQ A sits **~12 m above** the objectives, HQ B **~8 m below**. The asymmetry is balanced by distance |
| Capture radius | **7 m** (Lighthouse), **9 m** (Field) |
| Capture/lose time | **1 s / 1 s** |
| Vehicles per HQ | 2 jeeps, 2 tanks, 1 plane (+ extra spawners) |
| Weapon pickup | a rocket spawner at each objective |

→ Compared with Village, this is a **vehicle map**: twice the size, ~250 m from HQ to an objective (~30 s sprinting, ~15 s by jeep), and planes.

## Dressing mix (counts)
~250 tree objects (leaf trees, clusters, cypress), 27 cable poles + 17 pole clusters, ~60 crates/barrels, 14+10 rock piles, 11 U-shaped sandbag nests, 22 brick-wall segments, fences in 4 m, 17 sea-wake FX → a **green coastal countryside**: tree lines break up sightlines, a lighthouse is the main landmark, and sandbag nests mark the fighting spots.

## Unknowns
Exact coastline shape, sea level relative to the play area, route times, building bounds, colours.

## What we take (functional, not layout)
1. A **big vehicle map** variant: ~2× the infantry map, HQ→objective ~250 m.
2. **High-ground vs. distance** trade-off between bases.
3. One **tall coastal landmark** as the orientation point.
4. Objectives ~100 m apart, each with a pickup.
5. Tree lines + U-shaped cover nests as the main cover language.
