# Reference study: BFH "Village" (Victory_Village), 16-player

Read-only study of the local Rising Hub install. Source files were opened **in memory only**. Nothing was extracted, copied or placed in the project. **No EA geometry, textures or object placements go into the build.** These are measurements and ratios used to set functional targets.

Sources (all inside `F:\RisingHUB\game\mods\bfheroes\Levels\village\`):
`Info/*.desc`, `server.zip → Heightdata.con, Init.con, StaticObjects.con, GameModes/gpm_hoth/16/*.con`, `client.zip` (file listing only).

## Units
- Engine units are **consistent with metres**. Evidence: modular pieces are named by length (`highbrickwall_4m/8m`, `wooden_fence_8m`, `sandbags_4m`, `cable_8m/16m`, `road_post_8m`), and the spawn heights line up with terrain heights in metres.
- **Not yet verified against mesh bounds** (the `.staticmesh` format isn't decoded). Treat as an assumption with high confidence.
- Axes: engine Y = up; X/Z = ground plane.

## Terrain (exact, from Heightdata.con)
| Value | Data |
|---|---|
| Primary heightmap | 257×257 samples, 2 m spacing → **512 × 512 m** |
| Height scale | 0.0025 × 16-bit → max **~164 m** range |
| Outer ring | 8 tiles of 129×129 at 4 m spacing (backdrop, unplayable) |
| Sea level | 12 m |
| Object heights in use | y 12.0 – 72.5 m → **~60 m relief** in the play space |

## Layout (exact positions → derived distances)
| Item | Value |
|---|---|
| Static objects | 516 placed |
| Object spread | x −140…179, z −176…191 → **~320 × 365 m** of dressed area inside the 512 m tile |
| Base A ↔ Base B | **~230 m** straight line |
| Base → central objective | **~130 m** / **~112 m** (deliberately ~15% asymmetrical) |
| Objective ↔ 2nd objective | **~80 m** |
| Spawns per base | 4 (side A), 9 (side B), spread **10–40 m** apart |
| Players | 16 (8v8) |

## Objectives (exact, gpm_hoth)
- 2 capturable points ("Church Square", "Road Block") + 2 locked bases.
- Capture radius **4 m** (very tight → fights over a small spot).
- Defend radius **100 m**.
- Time to capture **0.1 s**, time to lose **4 s**. Capturing is near-instant; holding is what counts.
- Tickets 600 per team; low-ticket warning at 30.
- Each base has a tank spawner; a rocket/weapon pickup spawns at each objective.

## Modularity (from object names/counts)
- Walls in **4 m and 8 m** segments (plus an 8 m arch variant). Fences 8 m with endpieces. Sandbags 4 m. Cables 8/16 m.
- → a **4 m grid** for walls and cover.
- Dressing mix (top items): ~80 leaf/cypress trees, ~50 brick-wall segments, ~27 cable poles, ~26 lamp posts, ~19 tank traps, ~40 town houses (small/medium, 3 variants each), crates/barrels, ~25 ambient FX (smoke, fireflies, water).
- Houses come in **2 sizes × 3 variants**, so few unique buildings with many reuses.

## Lighting/textures (listing only, not opened)
- Terrain = 2×2 colour-map tiles + detail maps + **baked lightmaps** (incl. per-object lightmaps ~32 MB).
- Separate env map, "lightWarp" ramps for bundled/skinned meshes → **ramp-based (toon-ish) shading**.
- Colours: **not measured** (no texture decoded). Stored tint vs rendered colour still unknown.

## Unknowns
- Exact building/prop bounds, doorway sizes, cover heights (needs a mesh parser or in-game measurement).
- Route travel times (needs gameplay capture).
- Vehicle and character dimensions.
- Actual screen colours.

## Takeaways → our targets (see `docs/TARGETS.md`)
- Small play space (~350 m dressed) inside a bigger backdrop.
- Base→objective ≈ 110–130 m, about 20–25 s of sprinting.
- Small capture zones + quick capture = intense spot fights.
- 4 m modular grid; few building types, many variants.
