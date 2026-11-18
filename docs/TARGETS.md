# Functional targets – Project Mudfront (v0.1)

Our own numbers, informed by `reference/map-study-village.md`. 1 unit = 1 m.

## Player
| Target | Value | Why |
|---|---|---|
| Character height | 1.8 m (head 0.45 m, chunky) | readable silhouette |
| CharacterController | height 1.8, radius 0.4, step 0.4, slope 45° | |
| Run / sprint | 5.5 / 8 m/s | |
| Jump | 1.2 m | climbs low cover |

## Architecture
| Target | Value |
|---|---|
| Grid | **4 m** modules (walls 4 m, half-piece 2 m) |
| Doorway clearance | **1.6 w × 2.6 h m** (≥ 2× controller radius + margin, ≥ height + 0.8 m camera margin) |
| Wall height | 3.2 m per storey |
| Low cover | 1.0 m (crouch hides, stand shoots over) |
| High cover | 1.8–2.0 m (full hide) |
| Window sill | 1.0 m |

## Map (3 teams × 10 → v1 test slice is smaller)
| Target | Value |
|---|---|
| Test-slice size | 160 × 160 m |
| Full v1 map | dressed ~350 m inside a 512 m tile |
| Base → objective | 110–130 m (full) / ~55 m (slice) |
| Sprint time base → objective | **≤ 25 s** (full) / ~8 s (slice) |
| Routes to objective | ≥ 2 infantry + 1 vehicle lane |
| Relief | 20–60 m elevation range |

## Objective
| Target | Value |
|---|---|
| Zone radius | 8 m (ours is bigger than the reference 4 m, for 30 players) |
| Capture | 3 s uncontested |
| Score | +1/s while held, first to 100 |

## Readability
- Players must read clearly against the background at 60 m from the gameplay camera.
- Faction colour on the torso/helmet; environment stays more muted than the characters.
