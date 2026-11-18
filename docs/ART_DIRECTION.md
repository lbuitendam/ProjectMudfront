# Art direction – Project Mudfront (v1)

**Problem with v0:** everything was a rounded box with one flat colour → toy blocks / "Roblox".
**Goal:** a stylised, hand-made look: **crisp, angular and tapered**, with painted colour variation. Think illustrated storybook war, not plastic toys.

## 1. Shape language
| Rule | Do | Don't |
|---|---|---|
| **Crisp edges** | small chamfers (1–3 cm), one segment | big round bevels everywhere |
| **Taper & lean** | walls narrower at the top, posts leaning 1–3°, roofs sagging | perfect vertical boxes |
| **Mix of sizes** | big / medium / small rhythm (plank, gap, nail-strip) | equal-sized repeats |
| **Silhouette first** | every prop has 1 recognisable outline feature (chimney, strut, flag) | featureless cubes |
| **Breakup** | split surfaces into planks, stones and tiles with gaps | single solid slabs |

## 2. Proportions (characters)
- **~6 heads tall** (v0 was ~4, which is the "chibi/Roblox" look).
- V-shaped torso: shoulders 1.6× waist. Long arms, hands ~1.2× realistic (not huge balls).
- Tapered limbs (thicker at the top), boots wider than ankles.
- Angular head: square jaw, defined nose, brow ridge. Headgear is the faction silhouette.

## 3. Colour & material
- Every material has **3 shades** (dark / base / light). Planks, stones and tiles pick randomly between them → a "painted" look without textures.
- Environment is **desaturated** compared with characters; faction colours are the most saturated thing on screen.
- Roughness high (matte); no shiny plastic.
- Dark ink outline (inverted hull) on hero assets in the render; in Unity later via a toon/outline shader.

## 4. Lighting
Warm key light (sun), cool ambient shadows, soft fog for depth. Shadows are never black.

## 5. Checks before an asset is "done"
1. Readable from the gameplay camera at 30 m and 60 m (silhouette test in black).
2. No surface larger than ~2 m² without breakup.
3. Matches the size targets in `TARGETS.md` (door 1.6×2.6 m, cover 1.0/2.0 m).
