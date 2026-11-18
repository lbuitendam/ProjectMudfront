"""Playable-slice asset set, v3 art direction (docs/ART_DIRECTION.md): crisp, tapered, broken-up, painted shade variation.
Sizes follow docs/TARGETS.md. Asset names are stable (Unity scenes reference them).

Modular kit (4 m grid): wall, wall+door, wall+window, corner post, floor tile, roof slope
Cover: low sandbag line (1.0 m), crate stack (~1.8 m), high plank barricade (2.0 m)
Vehicle: "Mudskipper" two-seat buggy
Character: trooper (~1.85 m, ~6 heads), static placeholder (no rig yet)

Run: blender -b --factory-startup -P tools/blender/build_slice_assets.py
Out: Mudfront/Assets/_Mudfront/Art/Models/*.fbx  + art/source/*.blend
"""
import bpy, math, os, random, sys
sys.path.append(os.path.dirname(__file__))
import mudfront_style as S
from mudfront_style import tbox, prism

random.seed(11)
UNITY_OUT = os.path.join(S.ROOT, "Mudfront", "Assets", "_Mudfront", "Art", "Models")
SRC_OUT = os.path.join(S.ROOT, "art", "source")
os.makedirs(UNITY_OUT, exist_ok=True); os.makedirs(SRC_OUT, exist_ok=True)
S.EXPORT_DIR = UNITY_OUT

DOOR_W, DOOR_H = 1.6, 2.6
WALL_H, WALL_T, MOD = 3.2, 0.4, 4.0
R = math.radians


def j(a): return random.uniform(-a, a)


def save(name):
    S.finish(name)
    src = os.path.join(UNITY_OUT, name + ".blend")
    if os.path.exists(src):
        os.replace(src, os.path.join(SRC_OUT, name + ".blend"))


# ================================================================ modular kit
def stones_row(x0, x1, y, h=0.45, depth=WALL_T + 0.14):
    """Irregular stone plinth between x0..x1."""
    x = x0
    while x < x1 - 0.05:
        w = min(random.uniform(0.45, 0.85), x1 - x)
        tbox("stone", (w - 0.04, depth + j(0.04), h + j(0.05)), (x + w / 2, y + j(0.02), (h + j(0.05)) / 2),
             "terrain.rock", taper=(0.9, 0.92), bevel=0.03, jitter=0.02)
        x += w


def timber(name, size, loc, rot=(0, 0, 0)):
    return tbox(name, size, loc, "props.wood_dark", bevel=0.012, rot=rot, jitter=0.008)


def wall_segments(name, holes):
    """4 m half-timbered wall around rectangular holes [(x_center, width, z_bottom, height)]."""
    S.reset_scene()
    xs = sorted({-MOD / 2, MOD / 2, *[h[0] - h[1] / 2 for h in holes], *[h[0] + h[1] / 2 for h in holes]})
    solids = []
    for a, b in zip(xs, xs[1:]):
        cx, w = (a + b) / 2, b - a
        hole = next((h for h in holes if abs(h[0] - cx) < 1e-6 and abs(h[1] - w) < 1e-6), None)
        if not hole:
            tbox("plaster", (w, WALL_T, WALL_H - 0.45), (cx, 0, 0.45 + (WALL_H - 0.45) / 2), "props.plaster",
                 taper=(1.0, 0.86), bevel=0.01, shade=random.choice((0, 1)))
            solids.append((a, b))
            continue
        _, hw, zb, hh = hole
        if zb > 0:
            tbox("sill_wall", (w, WALL_T, zb - 0.45), (cx, 0, 0.45 + (zb - 0.45) / 2), "props.plaster", bevel=0.01, shade=0)
            tbox("sill", (w + 0.3, WALL_T + 0.16, 0.1), (cx, 0, zb - 0.05), "props.wood", bevel=0.01)
        top = WALL_H - (zb + hh)
        tbox("lintel_wall", (w, WALL_T, top), (cx, 0, zb + hh + top / 2), "props.plaster", taper=(1.0, 0.9), bevel=0.01, shade=0)
        # frame sits outside the hole: clear opening stays exactly w x hh
        timber("lintel", (w + 0.5, WALL_T + 0.12, 0.22), (cx, 0, zb + hh + 0.11))
        for sx in (-1, 1):
            timber("jamb", (0.16, WALL_T + 0.1, hh + (0 if zb == 0 else 0.1)), (cx + sx * (w / 2 + 0.08), 0, zb + hh / 2))
        if zb > 0:  # window: crossbar + one hinged shutter
            timber("mullion", (0.06, 0.06, hh), (cx, 0, zb + hh / 2))
            timber("transom", (w, 0.06, 0.06), (cx, 0, zb + hh * 0.55))
            tbox("shutter", (w * 0.48, 0.07, hh * 0.98), (cx - w * 0.78, -WALL_T / 2 - 0.12, zb + hh / 2),
                 "factions.tidewatch_blue.dark", bevel=0.01, rot=(0, 0, R(-25)))
    # stone plinth (gaps at door openings)
    door_edges = [(h[0] - h[1] / 2 - 0.16, h[0] + h[1] / 2 + 0.16) for h in holes if h[2] == 0]
    cuts = [-MOD / 2] + [e for d in door_edges for e in d] + [MOD / 2]
    for a, b in zip(cuts[0::2], cuts[1::2]):
        if b - a > 0.05:
            stones_row(a, b, 0)
    # half-timbering on both faces: end posts, mid rail on solid parts, braces where wide enough
    for face in (-1, 1):
        y = face * (WALL_T / 2 + 0.02)
        for x in (-MOD / 2 + 0.1, MOD / 2 - 0.1):
            timber("post", (0.2, 0.08, WALL_H - 0.45), (x, y, 0.45 + (WALL_H - 0.45) / 2))
        for a, b in solids:
            if b - a < 0.3:
                continue
            timber("rail", (b - a, 0.08, 0.16), ((a + b) / 2, y, 1.75 + j(0.03)))
            if b - a > 1.0:
                ang = math.atan2(1.2, (b - a) - 0.4) * (1 if random.random() > 0.5 else -1)
                timber("brace", (math.hypot(b - a - 0.4, 1.2), 0.07, 0.14), ((a + b) / 2, y, 2.45), rot=(0, ang, 0))
    timber("topbeam", (MOD + 0.04, WALL_T + 0.16, 0.24), (0, 0, WALL_H - 0.12))
    save(name)


def kit():
    wall_segments("SM_Kit_Wall_4m", [])
    wall_segments("SM_Kit_WallDoor_4m", [(0.0, DOOR_W, 0.0, DOOR_H)])
    wall_segments("SM_Kit_WallWindow_4m", [(0.0, 1.4, 1.0, 1.1)])

    S.reset_scene()
    timber("post", (0.42, 0.42, WALL_H + 0.15), (0, 0, (WALL_H + 0.15) / 2))
    tbox("cap", (0.55, 0.55, 0.12), (0, 0, WALL_H + 0.2), "props.wood_dark", taper=(0.7, 0.7), bevel=0.01)
    for i in range(3):
        tbox("footstone", (0.36, 0.36, 0.25), (j(0.06) + (i - 1) * 0.22, j(0.12), 0.12), "terrain.rock", taper=(0.8, 0.8), bevel=0.02, jitter=0.02)
    save("SM_Kit_CornerPost")

    S.reset_scene()
    n = 10
    for i in range(n):
        w = MOD / n
        tbox("board", (w - 0.025, MOD, 0.1), (-MOD / 2 + w * (i + 0.5), j(0.01), 0.15), "props.wood", bevel=0.008, jitter=0.004)
    for y in (-1.6, 0, 1.6):
        timber("joist", (MOD, 0.16, 0.1), (0, y, 0.05))
    save("SM_Kit_Floor_4m")

    S.reset_scene()
    ang = R(35)
    run = MOD / 2 + 0.5
    slope_len = run / math.cos(ang)
    tbox("deck", (slope_len, MOD + 0.2, 0.08), (run / 2 - 0.25, 0, run * math.tan(ang) / 2), "props.wood_dark", rot=(0, -ang, 0), bevel=0.005)
    rows = 7
    for r in range(rows):
        t = (r + 0.5) / rows
        n_t = 9
        for k in range(n_t):
            # individual shingles, staggered, slightly lifted
            y = -MOD / 2 - 0.05 + (k + (0.5 if r % 2 else 0)) * ((MOD + 0.2) / n_t)
            if y > MOD / 2 + 0.1:
                continue
            x = -0.25 + run * t
            z = run * math.tan(ang) * t + 0.08
            tbox("shingle", (slope_len / rows + 0.08, (MOD + 0.2) / n_t - 0.03, 0.05), (x, y, z), "props.roof_red",
                 rot=(R(j(2)), -ang + R(j(2.5)), 0), bevel=0.006, taper=(1.0, 0.92))
    timber("ridge", (0.2, MOD + 0.3, 0.2), (run - 0.25, 0, run * math.tan(ang) + 0.05))
    save("SM_Kit_RoofSlope_4m")


# ================================================================ cover
def cover():
    S.reset_scene()  # low cover ~1.0 m, 4 m long
    for row in range(4):
        n, off = (8, 0) if row % 2 == 0 else (7, 0.25)
        for i in range(n):
            tbox("bag", (0.5, 0.72 - row * 0.07, 0.26), (-1.75 + i * 0.5 + off + j(0.02), j(0.03), 0.13 + row * 0.245),
                 "props.sandbag", taper=(0.82, 0.78), bevel=0.04, rot=(R(j(3)), R(j(3)), R(j(6))), jitter=0.015)
    for x in (-2.1, 2.1):
        prism("stake", 0.05, 1.1, (x, 0, 0.5), "props.wood_dark", sides=5, taper=0.3)
    save("SM_Cover_SandbagsLow_4m")

    S.reset_scene()  # crate stack
    for (x, y, z, s, r) in [(-0.6, 0, 0, 1.0, 4), (0.55, 0.1, 0, 0.95, -6), (0.0, 0.0, 0.97, 0.8, 12)]:
        rot = (0, 0, R(r))
        cs, sn = math.cos(R(r)), math.sin(R(r))
        off = lambda dx, dy: (x + dx * cs - dy * sn, y + dx * sn + dy * cs)
        tbox("crate", (s - 0.06, s - 0.06, s * 0.95 - 0.04), (x, y, z + s * 0.475), "props.wood_light", rot=rot, bevel=0.01, shade=0)
        for side in (-1, 1):  # plank faces with gaps
            for k in range(3):
                px, py = off(0, side * (s / 2 - 0.01))
                tbox("plank", (s - 0.02, 0.035, s * 0.95 / 3 - 0.03), (px, py, z + (k + 0.5) * s * 0.95 / 3), "props.wood_light", rot=rot, bevel=0.006)
            for cx in (-1, 1):
                px, py = off(cx * (s / 2 - 0.05), side * (s / 2 + 0.01))
                tbox("batten", (0.09, 0.05, s * 0.95), (px, py, z + s * 0.475), "props.wood_dark", rot=rot, bevel=0.006)
            px, py = off(0, side * (s / 2 + 0.02))
            tbox("diag", (s * 1.25, 0.04, 0.09), (px, py, z + s * 0.475), "props.wood_dark", rot=(0, R(43), R(r)), bevel=0.006)
    save("SM_Cover_CrateStack")

    S.reset_scene()  # high barricade 2.0 m: pointed planks
    x = -2.0
    while x < 1.95:
        w = random.uniform(0.32, 0.5)
        h = 1.85 + random.uniform(-0.05, 0.25)
        tbox("plank", (w - 0.03, 0.09, h * 0.88), (x + w / 2, 0, h * 0.44), "props.wood", rot=(0, R(j(2.5)), 0), bevel=0.008, jitter=0.01)
        tbox("point", (w - 0.03, 0.09, h * 0.12), (x + w / 2, 0, h * 0.88 + h * 0.06), "props.wood", taper=(0.15, 1.0),
             rot=(0, R(j(2.5)), 0), bevel=0.004)
        x += w
    for z in (0.55, 1.45):
        timber("rail", (4.1, 0.09, 0.18), (0, -0.1, z + j(0.04)), rot=(0, R(j(2)), 0))
    for sx in (-1.4, 1.4):
        timber("strut", (0.12, 1.75, 0.12), (sx, 0.6, 0.62), rot=(R(-40), 0, 0))
    save("SM_Cover_BarricadeHigh_4m")


# ================================================================ vehicle
def vehicle():
    """'Mudskipper' buggy, ~3.7 m. Forward = -Y in Blender."""
    S.reset_scene()
    team = "factions.muddogs_green"
    tbox("chassis", (1.6, 3.3, 0.35), (0, 0, 0.72), team + ".dark", taper=(0.96, 0.98), bevel=0.012)
    tbox("tub", (1.55, 2.0, 0.5), (0, 0.25, 1.1), team + ".main", taper=(1.05, 1.0), bevel=0.012)
    tbox("hood", (1.35, 1.25, 0.42), (0, -1.15, 1.08), team + ".main", taper=(0.85, 0.8), shear=(0, 0.12), bevel=0.012)
    tbox("grill", (1.0, 0.08, 0.36), (0, -1.79, 1.0), "props.metal", bevel=0.006, shade=-1)
    for k in range(5):
        tbox("slat", (0.04, 0.1, 0.3), (-0.36 + k * 0.18, -1.83, 1.0), "terrain.rock_dark", bevel=0.003)
    for sx in (-0.42, 0.42):
        prism("lamp", 0.12, 0.14, (sx, -1.78, 1.35), "factions.muddogs_green.accent", sides=8, rot=(R(90), 0, 0))
    tbox("windframe", (1.45, 0.06, 0.62), (0, -0.48, 1.62), "props.metal", rot=(R(-18), 0, 0), bevel=0.005)
    tbox("glass", (1.3, 0.03, 0.5), (0, -0.47, 1.62), "#3E5F7A" if False else "factions.tidewatch_blue.dark", rot=(R(-18), 0, 0), bevel=0)
    tbox("seat", (0.55, 0.5, 0.12), (-0.35, 0.15, 1.38), "props.canvas", bevel=0.02)
    tbox("seatback", (0.55, 0.14, 0.55), (-0.35, 0.42, 1.65), "props.canvas", taper=(0.9, 1.0), rot=(R(10), 0, 0), bevel=0.02)
    tbox("seat2", (0.55, 0.5, 0.12), (0.35, 0.15, 1.38), "props.canvas", bevel=0.02)
    tbox("seatback2", (0.55, 0.14, 0.55), (0.35, 0.42, 1.65), "props.canvas", taper=(0.9, 1.0), rot=(R(10), 0, 0), bevel=0.02)
    tbox("bumper", (1.85, 0.16, 0.18), (0, -1.9, 0.62), "props.metal", bevel=0.01)
    for sx in (-1, 1):
        tbox("fenderF", (0.42, 1.1, 0.08), (sx * 0.95, -1.15, 1.08), team + ".dark", rot=(R(-8), R(sx * 10), 0), bevel=0.006)
        tbox("fenderR", (0.42, 1.1, 0.08), (sx * 0.95, 1.15, 1.08), team + ".dark", rot=(R(8), R(sx * 10), 0), bevel=0.006)
        prism("rollpost", 0.05, 1.0, (sx * 0.72, 0.85, 1.75), "props.metal", sides=6)
    prism("rollbar", 0.05, 1.5, (0, 0.85, 2.25), "props.metal", sides=6, rot=(0, R(90), 0))
    for (x, y) in [(-0.95, -1.15), (0.95, -1.15), (-0.95, 1.15), (0.95, 1.15)]:
        prism("tyre", 0.48, 0.4, (x, y, 0.48), "terrain.rock_dark", sides=12, rot=(0, R(90), 0), bevel=0.04)
        for k in range(12):  # tread blocks
            a = k * math.tau / 12
            tbox("tread", (0.42, 0.12, 0.06), (x, y + 0.5 * math.cos(a), 0.48 + 0.5 * math.sin(a)), "terrain.rock_dark",
                 rot=(-a, 0, 0), bevel=0.004, shade=-1)
        prism("hub", 0.2, 0.44, (x, y, 0.48), team + ".accent", sides=6, rot=(0, R(90), 0))
    prism("spare", 0.42, 0.3, (0, 1.85, 1.15), "terrain.rock_dark", sides=12, rot=(R(90), 0, 0), bevel=0.03)
    tbox("jerrycan", (0.18, 0.36, 0.48), (0.55, 1.45, 1.6), team + ".dark", bevel=0.01)
    prism("wheel", 0.2, 0.04, (-0.35, -0.3, 1.72), "terrain.rock_dark", sides=10, rot=(R(55), 0, 0))
    save("SM_Veh_Mudskipper")


# ================================================================ character
def character():
    """Trooper ~1.86 m, ~6 heads. Tapered limbs, V torso, angular head, faction helmet."""
    S.reset_scene()
    team = "factions.tidewatch_blue"
    skin = "props.plaster"
    for sx in (-1, 1):
        x = sx * 0.12
        tbox("boot", (0.15, 0.3, 0.13), (x, -0.04, 0.065), "props.wood_dark", taper=(0.9, 0.75), bevel=0.012, shade=0)
        tbox("shin", (0.14, 0.15, 0.42), (x, 0, 0.34), team + ".dark", taper=(1.15, 1.15), bevel=0.01, shade=0)
        tbox("puttee", (0.15, 0.16, 0.14), (x, 0, 0.2), "props.canvas", taper=(1.05, 1.05), bevel=0.008, shade=-1)
        tbox("thigh", (0.17, 0.19, 0.44), (x, 0, 0.76), team + ".dark", taper=(1.2, 1.15), bevel=0.01, shade=0)
    tbox("hips", (0.4, 0.22, 0.16), (0, 0, 1.0), team + ".dark", taper=(1.05, 1.0), bevel=0.01, shade=0)
    tbox("torso", (0.38, 0.24, 0.48), (0, 0, 1.31), team + ".main", taper=(1.5, 1.12), bevel=0.012, shade=0)
    tbox("belt", (0.42, 0.26, 0.07), (0, 0, 1.1), "props.wood_dark", bevel=0.006, shade=0)
    for sx in (-0.13, 0.0, 0.13):
        tbox("pouch", (0.1, 0.06, 0.1), (sx, -0.15, 1.12), "props.canvas", bevel=0.008, shade=0)
    tbox("strap", (0.06, 0.27, 0.62), (0.05, 0, 1.3), "props.wood_dark", rot=(0, R(-28), 0), bevel=0.004, shade=0)
    tbox("pack", (0.34, 0.14, 0.34), (0, 0.2, 1.33), "props.canvas", taper=(0.9, 0.85), bevel=0.012, shade=0)
    tbox("collar", (0.2, 0.17, 0.06), (0, 0, 1.57), team + ".dark", bevel=0.006, shade=0)
    for sx in (-1, 1):
        sh = sx * 0.31
        tbox("upperarm", (0.12, 0.13, 0.32), (sh + sx * 0.03, 0, 1.38), team + ".main", taper=(0.85, 0.85),
             rot=(0, R(sx * 12), 0), bevel=0.008, shade=0)
        tbox("forearm", (0.1, 0.11, 0.29), (sh + sx * 0.09, -0.02, 1.08), team + ".main", taper=(0.85, 0.85),
             rot=(0, R(sx * 8), 0), bevel=0.008, shade=0)
        tbox("glove", (0.09, 0.11, 0.12), (sh + sx * 0.11, -0.03, 0.89), "props.wood", taper=(0.85, 0.8), bevel=0.01, shade=0)
        tbox("epaulette", (0.13, 0.15, 0.04), (sx * 0.27, 0, 1.55), team + ".accent", bevel=0.004, shade=0)
    # head (angular), ~0.30 m tall
    tbox("neck", (0.1, 0.1, 0.07), (0, 0, 1.62), skin, bevel=0.005, shade=0)
    tbox("cranium", (0.2, 0.23, 0.17), (0, 0.01, 1.76), skin, taper=(0.9, 0.88), bevel=0.012, shade=0)
    tbox("jaw", (0.19, 0.18, 0.1), (0, -0.015, 1.65), skin, taper=(1.12, 1.15), bevel=0.01, shade=0)
    tbox("chin", (0.1, 0.06, 0.05), (0, -0.1, 1.63), skin, bevel=0.006, shade=0)
    tbox("nose", (0.045, 0.06, 0.08), (0, -0.125, 1.72), skin, taper=(0.7, 0.4), shear=(0, -0.01), bevel=0.004, shade=0)
    tbox("brow", (0.17, 0.035, 0.018), (0, -0.112, 1.775), "props.wood_dark", bevel=0.003, shade=0)
    for sx in (-1, 1):
        tbox("eye", (0.03, 0.01, 0.022), (sx * 0.045, -0.118, 1.74), "terrain.rock_dark", bevel=0, shade=-1)
        tbox("ear", (0.03, 0.06, 0.07), (sx * 0.105, 0.0, 1.72), skin, bevel=0.005, shade=0)
    # faction helmet: wide shallow brim + tapered bowl + band
    prism("brim", 0.24, 0.025, (0, 0.0, 1.835), team + ".dark", sides=10, bevel=0.004, shade=0)
    prism("bowl", 0.155, 0.11, (0, 0.0, 1.9), team + ".dark", sides=10, taper=0.6, bevel=0.006, shade=0)
    prism("band", 0.158, 0.03, (0, 0.0, 1.86), team + ".accent", sides=10, bevel=0.002, shade=0)
    # rifle slung on the back (readable silhouette element)
    tbox("rifle", (0.05, 0.05, 0.95), (-0.05, 0.29, 1.3), "props.wood", rot=(0, R(30), 0), bevel=0.004, shade=0)
    tbox("barrel", (0.028, 0.028, 0.3), (-0.05 + 0.6 * math.sin(R(30)), 0.29, 1.3 + 0.6 * math.cos(R(30))), "props.metal",
         rot=(0, R(30), 0), bevel=0, shade=0)
    save("SM_Char_Trooper_Placeholder")


if __name__ == "__main__":
    kit(); cover(); vehicle(); character()
    print("SLICE ASSETS DONE ->", UNITY_OUT)
