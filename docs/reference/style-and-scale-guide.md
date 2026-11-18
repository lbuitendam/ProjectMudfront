# Style & Scale Guide – Reference Study

Three reference games, studied for **style, feel and systems**. Everything we make is modelled new; this guide is the reference, not a source of assets.

---

## 1. Battlefield Heroes (2009, EA/DICE) – studied from local files

**Look**
- Pixar-like cartoon: **big angular heads, square jaws, large hands, slim legs**. Faces are low-poly with clear shapes, hand-painted textures and almost no fine detail.
- Characters are **instantly readable**: each class/faction has distinctive headwear (cap, beret, slouch hat, wide-brim hat with scarf).
- Palette: warm and saturated. Blue sky, golden explosions, **faction colours** khaki/tan vs blue-grey. Soft lighting, no gritty post-processing.
- Setting: a cheerful, pastiche WW2: villages, farms, coast, snow and night variants.

**Scale & layout (from game data)**
- Maps ~**16 players** (8v8), modes: Team Deathmatch and "Hold the Hill" (capture points).
- Map assets split per level: client ~18 MB (art), server <0.1 MB (logic/collision), so maps are small and compact.
- ~20 base maps × variants (day/night/snow/doom).
- 3 classes: Commando, Soldier, Gunner. Third-person camera.

**Feel:** forgiving, slapstick, abilities > aim, quick respawn, 10-min rounds.

---

## 2. Heroes of Valor (2024–, Fancy Cat Interactive) – web research

- A **spiritual successor to BFH**: WW2, third person, arcade.
- **Look:** stylised/cartoonish, "Stylized WWII chaos": golden wheat fields, teal-green uniforms, exaggerated smoke columns. More modern than BFH: more detail and better lighting, but just as playful.
- Classes: launch had 3 (soldier with bazooka, sneaky backstabber, medic); later sources mention 4 (Assault, Scout, Engineer, Heavy).
- Vehicles: fighter planes, motorbikes, tanks. Bots/singleplayer, custom matches.
- **Lesson:** this shows the BFH style still works today with modern rendering. It stands as its own game because it has its own characters and art.

---

## 3. WARDOGS (2026 Early Access) – Steam page

- **Opposite style:** realistic, modern, first-person, gritty Eastern European industrial mountains, destructible.
- **3 teams, up to 100 players**, King-of-the-Hill: random 2×2 km Control Zone inside a larger map, first to 100 points.
- **Economy/buy menu:** start with $10,000; buy a loadout (weapons, gear, utility, **vehicles**) **every life**; cash **persists between matches**; earn money from teamplay (revive, transport, objective). The Hot Zone pays double.
- **Base building:** build your own forward operating base, fortify chokepoints.
- Vehicles: tanks (including artillery), helis for supplies, APCs; jets planned.
- **What we take:** the **systems** (buy menu per life, persistent money, FOB building, zone mode). We **don't** take its realistic look.

---

## 4. Our mix → design direction

| Element | Take from |
|---|---|
| Art style & tone | BFH / HoV: cartoon, saturated, slapstick |
| Camera | Third person (BFH) |
| Classes + abilities | BFH: 3–4 classes with cooldown abilities |
| Buy menu + money per life | Wardogs |
| Base/FOB building | Wardogs |
| Zone-control mode | Wardogs, scaled down |
| Vehicles | All three: jeep, tank, plane, bike |

**Scale proposal (Godot, 1 unit = 1 m)**
- Character: **1.8 m**, head ≈ 1/5 of body height (realistic is ~1/7.5), hands 1.3×.
- Doors 2.4 m high / 1.4 m wide (oversized so the cartoon bodies fit).
- Houses: 1.3–1.5× real size, slightly crooked, thick walls (0.4 m), chunky roofs.
- Buildables (FOB): wall segment 4×2 m, sandbags 2×1 m, watchtower 3×3×6 m, turret 1.5 m. Snap to a 1 m grid.
- Vehicles: jeep 4 m, tank 7 m, plane 9 m span. Slightly stubby/toylike.
- Map: **400×400 m** for 16–24 players; zone 80×80 m that moves around.
- Match: 10–15 min; respawn 5–8 s.

**Visual rules**
1. Silhouette first: every class recognisable from 50 m.
2. Big shapes, few details; textures hand-painted / flat-shaded.
3. Max 3–4 main colours per faction; our own faction colours (not khaki-vs-blue-grey).
4. Exaggerated effects: big smoke balls, cartoon explosions.
5. Our own factions, names and headgear. Nothing directly modelled after a BFH/HoV character.
