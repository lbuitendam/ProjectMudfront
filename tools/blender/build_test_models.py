"""Phase 1 style test: house, wall, sandbags, tree, crate.

Run (after Blender is installed):
  blender -b -P tools/blender/build_test_models.py
Output: art/exports/*.fbx (+ .blend for tweaking)
"""
import os, sys, random, math
sys.path.append(os.path.dirname(__file__))
import mudfront_style as S

random.seed(42)  # same crooked look every run


def house():
    S.reset_scene()
    W, D, H = 6.0, 5.0, 3.2          # ~1.4x real cottage, chunky
    S.box("walls", (W, D, H), (0, 0, H / 2), "props.plaster", bevel=0.08, wobble=0.06)
    S.box("base", (W + 0.3, D + 0.3, 0.4), (0, 0, 0.2), "props.brick", bevel=0.05)
    S.prism_roof("roof", W, D, 2.0, (0, 0, H), "props.roof_red", overhang=0.4, wobble=0.08)
    S.box("door", (1.4, 0.2, 2.4), (0, -D / 2 - 0.05, 1.2), "props.wood_dark", bevel=0.05)
    for x in (-1.9, 1.9):
        S.box("window", (1.1, 0.15, 1.0), (x, -D / 2 - 0.05, 1.9), "props.wood", bevel=0.04)
        S.box("shutterL", (0.35, 0.12, 1.1), (x - 0.75, -D / 2 - 0.08, 1.9), "factions.ironclads_red.dark", bevel=0.03)
        S.box("shutterR", (0.35, 0.12, 1.1), (x + 0.75, -D / 2 - 0.08, 1.9), "factions.ironclads_red.dark", bevel=0.03)
    S.box("chimney", (0.7, 0.7, 2.2), (1.6, 1.0, H + 1.4), "props.brick", bevel=0.05, wobble=0.04, rot=(0, math.radians(3), 0))
    S.finish("SM_House_Cottage_A")


def wall():
    S.reset_scene()
    # 4 m segment, 2 m high, fits 1 m build grid
    for i, x in enumerate((-1.5, -0.5, 0.5, 1.5)):
        S.box(f"plank{i}", (0.95, 0.35, 2.0 + random.uniform(-0.08, 0.12)), (x, 0, 1.0), "props.wood", bevel=0.05, wobble=0.03)
    for z in (0.5, 1.5):
        S.box("rail", (4.2, 0.2, 0.25), (0, -0.25, z), "props.wood_dark", bevel=0.04, wobble=0.02)
    S.finish("SM_Build_WallWood_4m")


def sandbags():
    S.reset_scene()
    # 2 m x 1 m, 3 rows staggered
    for row in range(3):
        n = 4 if row % 2 == 0 else 3
        off = 0 if row % 2 == 0 else 0.25
        for i in range(n):
            x = -0.75 + i * 0.5 + off
            S.box(f"bag{row}_{i}", (0.5, 0.9 - row * 0.15, 0.28), (x, 0, 0.14 + row * 0.26), "props.sandbag",
                  bevel=0.1, wobble=0.03, rot=(0, 0, math.radians(random.uniform(-6, 6))))
    S.finish("SM_Build_Sandbags_2m")


def tree():
    S.reset_scene()
    S.cylinder("trunk", 0.3, 3.0, (0, 0, 1.5), "props.bark", verts=7)
    blobs = [((0, 0, 3.6), 1.6, "props.leaf"), ((0.8, 0.3, 3.1), 1.1, "props.leaf_light"),
             ((-0.7, -0.4, 3.2), 1.0, "props.leaf"), ((0.1, 0.2, 4.6), 1.0, "props.leaf_light")]
    for i, (loc, r, m) in enumerate(blobs):
        S.ico(f"leaves{i}", r, loc, m, subdiv=1, squash=(1, 1, 0.85))
    S.finish("SM_Tree_Round_A")


def crate():
    S.reset_scene()
    S.box("body", (1.0, 1.0, 1.0), (0, 0, 0.5), "props.wood_light", bevel=0.06, wobble=0.015)
    for y in (-0.51, 0.51):
        S.box("bandT", (1.04, 0.06, 0.14), (0, y, 0.85), "props.wood_dark", bevel=0.02)
        S.box("bandB", (1.04, 0.06, 0.14), (0, y, 0.15), "props.wood_dark", bevel=0.02)
        S.box("cross", (1.25, 0.05, 0.12), (0, y, 0.5), "props.wood_dark", bevel=0.02, rot=(0, math.radians(45), 0))
    S.finish("SM_Prop_Crate_A")


if __name__ == "__main__":
    for build in (house, wall, sandbags, tree, crate):
        build()
    print("All test models built ->", S.EXPORT_DIR)
