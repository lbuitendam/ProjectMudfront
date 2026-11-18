"""Playable-slice asset set (original designs, sizes from docs/TARGETS.md).

Modular kit (4 m grid): wall, wall+door, wall+window, corner post, floor tile, roof slope
Cover: low sandbag line (1.0 m), crate stack (1.4 m), high timber barricade (2.0 m)
Vehicle: "Mudskipper" two-seat buggy
Character: chunky placeholder trooper (1.8 m)

Run: blender -b --factory-startup -P tools/blender/build_slice_assets.py
Out: Mudfront/Assets/_Mudfront/Art/Models/*.fbx  + art/source/*.blend
"""
import bpy, math, os, random, sys
from mathutils import Vector
sys.path.append(os.path.dirname(__file__))
import mudfront_style as S

random.seed(11)
UNITY_OUT = os.path.join(S.ROOT, "Mudfront", "Assets", "_Mudfront", "Art", "Models")
SRC_OUT = os.path.join(S.ROOT, "art", "source")
os.makedirs(UNITY_OUT, exist_ok=True); os.makedirs(SRC_OUT, exist_ok=True)
S.EXPORT_DIR = UNITY_OUT  # finish() exports here

DOOR_W, DOOR_H = 1.6, 2.6
WALL_H, WALL_T, MOD = 3.2, 0.4, 4.0


def j(a): return random.uniform(-a, a)


def save(name):
    o = S.finish(name)
    # keep .blend sources out of Assets
    src = os.path.join(UNITY_OUT, name + ".blend")
    if os.path.exists(src):
        os.replace(src, os.path.join(SRC_OUT, name + ".blend"))
    return o


# ---------------- modular kit ----------------
def wall_segments(name, holes):
    """4 m wall built from blocks around rectangular holes [(x_center, width, z_bottom, height)]."""
    S.reset_scene()
    xs = sorted({-MOD / 2, MOD / 2, *[h[0] - h[1] / 2 for h in holes], *[h[0] + h[1] / 2 for h in holes]})
    for a, b in zip(xs, xs[1:]):
        cx, w = (a + b) / 2, b - a
        hole = next((h for h in holes if abs(h[0] - cx) < 1e-6 and abs(h[1] - w) < 1e-6), None)
        if not hole:
            S.box("w", (w, WALL_T, WALL_H), (cx, 0, WALL_H / 2), "props.plaster", bevel=0.05, wobble=0.015)
            continue
        _, hw, zb, hh = hole
        if zb > 0:
            S.box("sill", (w, WALL_T, zb), (cx, 0, zb / 2), "props.plaster", bevel=0.04)
        top = WALL_H - (zb + hh)
        S.box("lintelwall", (w, WALL_T, top), (cx, 0, zb + hh + top / 2), "props.plaster", bevel=0.04)
        # frame sits OUTSIDE the hole so the clear opening stays exactly w x hh
        S.box("lintel", (w + 0.5, WALL_T + 0.12, 0.25), (cx, 0, zb + hh + 0.125), "props.wood_dark", bevel=0.04)
        for sx in (-1, 1):
            S.box("jamb", (0.18, WALL_T + 0.1, hh), (cx + sx * (w / 2 + 0.09), 0, zb + hh / 2), "props.wood_dark", bevel=0.03)
    # plinth + top beam give the cartoon "frame"
    # stone plinth, interrupted at floor-level openings (doors)
    door_edges = [(h[0] - h[1] / 2 - 0.18, h[0] + h[1] / 2 + 0.18) for h in holes if h[2] == 0]
    cuts = [-MOD / 2] + [e for d in door_edges for e in d] + [MOD / 2]
    for a, b in zip(cuts[0::2], cuts[1::2]):
        if b - a > 0.05:
            S.box("plinth", (b - a, WALL_T + 0.16, 0.45), ((a + b) / 2, 0, 0.225), "terrain.rock", bevel=0.08, wobble=0.02)
    S.box("topbeam", (MOD + 0.05, WALL_T + 0.14, 0.28), (0, 0, WALL_H - 0.14), "props.wood_dark", bevel=0.05, wobble=0.01)
    save(name)


def kit():
    wall_segments("SM_Kit_Wall_4m", [])
    wall_segments("SM_Kit_WallDoor_4m", [(0.0, DOOR_W, 0.0, DOOR_H)])
    wall_segments("SM_Kit_WallWindow_4m", [(0.0, 1.4, 1.0, 1.1)])
    S.reset_scene()
    S.box("post", (0.6, 0.6, WALL_H + 0.2), (0, 0, (WALL_H + 0.2) / 2), "props.wood_dark", bevel=0.06, wobble=0.015)
    S.box("base", (0.75, 0.75, 0.5), (0, 0, 0.25), "terrain.rock", bevel=0.08)
    save("SM_Kit_CornerPost")
    S.reset_scene()
    S.box("floor", (MOD, MOD, 0.2), (0, 0, 0.1), "props.wood", bevel=0.03)
    for i in range(5):
        S.box("plankline", (0.04, MOD, 0.02), (-1.6 + i * 0.8, 0, 0.205), "props.wood_dark", bevel=0)
    save("SM_Kit_Floor_4m")
    S.reset_scene()
    ang = math.radians(35)
    run = MOD / 2 + 0.5
    S.box("roof", (run / math.cos(ang), MOD + 0.2, 0.22), (run / 2 - 0.25, 0, run * math.tan(ang) / 2), "props.roof_red",
          bevel=0.05, rot=(0, -ang, 0))
    for r in range(4):
        t = (r + 0.5) / 4
        S.box("tile", (run / math.cos(ang) / 4 + 0.1, MOD + 0.25, 0.1),
              (-0.25 + run * t + 0.06, 0, run * math.tan(ang) * t + 0.15), "props.roof_red" if r % 2 else "props.brick",
              bevel=0.03, rot=(0, -ang + j(0.02), 0))
    save("SM_Kit_RoofSlope_4m")


# ---------------- cover ----------------
def cover():
    S.reset_scene()  # low cover 1.0 m, 4 m long
    for row in range(4):
        n, off = (8, 0) if row % 2 == 0 else (7, 0.25)
        for i in range(n):
            S.box("bag", (0.52, 0.75 - row * 0.08, 0.27), (-1.75 + i * 0.5 + off, 0, 0.135 + row * 0.25),
                  "props.sandbag", bevel=0.1, wobble=0.025, rot=(0, 0, math.radians(j(5))))
    save("SM_Cover_SandbagsLow_4m")

    S.reset_scene()  # crate stack 1.4 m
    for (x, y, z, s, r) in [(-0.6, 0, 0, 1.0, 4), (0.55, 0.1, 0, 1.0, -6), (0.0, 0.0, 1.0, 0.8, 12)]:
        S.box("crate", (s, s, s * 0.95), (x, y, z + s * 0.475), "props.wood_light", bevel=0.06, rot=(0, 0, math.radians(r)))
        S.box("band", (s + 0.04, s * 0.2, s * 0.98), (x, y, z + s * 0.475), "props.wood_dark", bevel=0.03, rot=(0, 0, math.radians(r)))
    save("SM_Cover_CrateStack")

    S.reset_scene()  # high barricade 2.0 m
    for i in range(7):
        S.box("plank", (0.55, 0.18, 2.0 + j(0.15)), (-1.65 + i * 0.55, 0, 1.0), "props.wood", bevel=0.05, wobble=0.03,
              rot=(0, math.radians(j(3)), 0))
    for z in (0.6, 1.5):
        S.box("rail", (4.1, 0.16, 0.22), (0, -0.17, z), "props.wood_dark", bevel=0.04, rot=(0, math.radians(j(2)), 0))
    for sx in (-1.4, 1.4):
        S.box("strut", (0.18, 1.6, 0.18), (sx, 0.55, 0.65), "props.wood_dark", bevel=0.03, rot=(math.radians(-38), 0, 0))
    save("SM_Cover_BarricadeHigh_4m")


# ---------------- vehicle ----------------
def vehicle():
    """'Mudskipper' buggy: stubby toy proportions, 3.6 m long, open seats. Forward = -Y in Blender (Unity +Z)."""
    S.reset_scene()
    S.box("chassis", (1.7, 3.4, 0.45), (0, 0, 0.75), "factions.muddogs_green.main", bevel=0.12, wobble=0.01)
    S.box("hood", (1.5, 1.2, 0.45), (0, -1.05, 1.15), "factions.muddogs_green.main", bevel=0.15)
    S.box("grill", (1.2, 0.12, 0.35), (0, -1.68, 1.05), "props.metal", bevel=0.04)
    S.box("seatback", (1.4, 0.25, 0.7), (0, 0.55, 1.3), "props.canvas", bevel=0.08)
    S.box("cargo", (1.5, 0.9, 0.35), (0, 1.25, 1.15), "factions.muddogs_green.dark", bevel=0.08)
    S.box("windframe", (1.5, 0.1, 0.6), (0, -0.35, 1.55), "props.metal", bevel=0.03, rot=(math.radians(-15), 0, 0))
    S.box("bumper", (1.9, 0.25, 0.25), (0, -1.85, 0.6), "props.metal", bevel=0.06)
    S.box("rollbar", (1.6, 0.12, 0.12), (0, 0.85, 2.05), "props.metal", bevel=0.03)
    for sx in (-0.75, 0.75):
        S.box("rollpost", (0.12, 0.12, 0.9), (sx, 0.85, 1.6), "props.metal", bevel=0.03)
    for (x, y) in [(-0.95, -1.15), (0.95, -1.15), (-0.95, 1.15), (0.95, 1.15)]:
        S.cylinder("tyre", 0.5, 0.45, (x, y, 0.5), "props.rock_dark" if False else "terrain.rock_dark", verts=12, rot=(0, math.pi / 2, 0), bevel=0.08)
        S.cylinder("hub", 0.22, 0.48, (x, y, 0.5), "factions.muddogs_green.accent", verts=8, rot=(0, math.pi / 2, 0))
    S.cylinder("wheelsteer", 0.22, 0.06, (-0.35, -0.15, 1.55), "terrain.rock_dark", verts=10, rot=(math.radians(60), 0, 0))
    save("SM_Veh_Mudskipper")


# ---------------- character ----------------
def character():
    """Chunky trooper, 1.8 m. Big head + hands, short legs: readable silhouette. Static placeholder (no rig yet)."""
    S.reset_scene()
    team = "factions.tidewatch_blue"
    S.box("bootL", (0.26, 0.38, 0.18), (-0.15, -0.05, 0.09), "props.wood_dark", bevel=0.06)
    S.box("bootR", (0.26, 0.38, 0.18), (0.15, -0.05, 0.09), "props.wood_dark", bevel=0.06)
    S.box("legL", (0.24, 0.26, 0.5), (-0.15, 0, 0.42), team + ".dark", bevel=0.06)
    S.box("legR", (0.24, 0.26, 0.5), (0.15, 0, 0.42), team + ".dark", bevel=0.06)
    S.box("torso", (0.66, 0.4, 0.62), (0, 0, 0.98), team + ".main", bevel=0.12, wobble=0.01)
    S.box("belt", (0.7, 0.44, 0.1), (0, 0, 0.72), "props.wood_dark", bevel=0.03)
    S.box("pack", (0.48, 0.2, 0.42), (0, 0.29, 1.0), "props.canvas", bevel=0.07)
    S.box("armL", (0.18, 0.2, 0.55), (-0.45, 0, 0.98), team + ".main", bevel=0.06, rot=(0, math.radians(-8), 0))
    S.box("armR", (0.18, 0.2, 0.55), (0.45, 0, 0.98), team + ".main", bevel=0.06, rot=(0, math.radians(8), 0))
    S.ico("handL", 0.13, (-0.5, 0, 0.66), "props.plaster", subdiv=2)
    S.ico("handR", 0.13, (0.5, 0, 0.66), "props.plaster", subdiv=2)
    S.box("neck", (0.16, 0.16, 0.08), (0, 0, 1.33), "props.plaster", bevel=0.03)
    S.box("head", (0.4, 0.38, 0.4), (0, -0.02, 1.55), "props.plaster", bevel=0.1)
    S.box("jaw", (0.36, 0.32, 0.12), (0, -0.05, 1.38), "props.plaster", bevel=0.05)
    S.box("nose", (0.08, 0.1, 0.12), (0, -0.23, 1.53), "props.plaster", bevel=0.03)
    # signature headgear: wide flat cap with brim (team silhouette marker)
    S.cylinder("cap", 0.25, 0.14, (0, 0, 1.78), team + ".accent", verts=10, bevel=0.03)
    S.box("brim", (0.32, 0.22, 0.04), (0, -0.25, 1.72), team + ".dark", bevel=0.02)
    save("SM_Char_Trooper_Placeholder")


if __name__ == "__main__":
    kit(); cover(); vehicle(); character()
    print("SLICE ASSETS DONE ->", UNITY_OUT)
