"""Style test v2: one hero cottage with real cartoon shape language + toon render.
Run: blender -b --factory-startup -P tools/blender/cottage_v2.py -> art/exports/_cottage_v2.png (+ .blend)
"""
import bpy, bmesh, math, os, random, sys
from mathutils import Vector
sys.path.append(os.path.dirname(__file__))
import mudfront_style as S

random.seed(7)
S.reset_scene()
OUT = S.EXPORT_DIR
os.makedirs(OUT, exist_ok=True)

# ---------- toon materials ----------
def lin(h): return S.hex_to_linear(S.color(h) if not h.startswith("#") else h)

_cache = {}
def toon(key, dark=0.55, grad=True):
    """Cel-shaded material: 3-step light ramp, vertical colour gradient (darker at the bottom)."""
    if key in _cache: return _cache[key]
    m = bpy.data.materials.new("T_" + key.replace(".", "_").replace("#", ""))
    m.use_nodes = True
    nt = m.node_tree; N = nt.nodes; L = nt.links
    for n in list(N): N.remove(n)
    out = N.new("ShaderNodeOutputMaterial")
    diff = N.new("ShaderNodeBsdfDiffuse")
    s2r = N.new("ShaderNodeShaderToRGB")
    ramp = N.new("ShaderNodeValToRGB")
    e = ramp.color_ramp; e.interpolation = "CONSTANT"
    e.elements[0].position = 0.0; e.elements[0].color = (0.45, 0.42, 0.55, 1)   # cool shadow
    e.elements[1].position = 0.18; e.elements[1].color = (0.80, 0.78, 0.82, 1)
    el = e.elements.new(0.55); el.color = (1.05, 1.0, 0.92, 1)                  # warm light
    base = lin(key)
    tex = N.new("ShaderNodeTexCoord")
    sep = N.new("ShaderNodeSeparateXYZ")
    mixc = N.new("ShaderNodeMix"); mixc.data_type = "RGBA"
    mixc.inputs[6].default_value = tuple(c * dark for c in base[:3]) + (1,)
    mixc.inputs[7].default_value = base
    L.new(tex.outputs["Generated"], sep.inputs[0])
    if grad: L.new(sep.outputs["Z"], mixc.inputs[0])
    else: mixc.inputs[0].default_value = 1.0
    mul = N.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"
    mul.inputs[0].default_value = 1.0
    L.new(diff.outputs[0], s2r.inputs[0])
    L.new(s2r.outputs[0], ramp.inputs[0])
    L.new(mixc.outputs[2], mul.inputs[6])
    L.new(ramp.outputs[0], mul.inputs[7])
    em = N.new("ShaderNodeEmission")
    L.new(mul.outputs[2], em.inputs[0])
    L.new(em.outputs[0], out.inputs[0])
    m.diffuse_color = base
    _cache[key] = m
    return m

OUTLINE = bpy.data.materials.new("T_outline")
OUTLINE.use_nodes = True
_n = OUTLINE.node_tree.nodes
_n.remove(_n.get("Principled BSDF"))
_em = _n.new("ShaderNodeEmission"); _em.inputs[0].default_value = (0.06, 0.04, 0.035, 1)
OUTLINE.node_tree.links.new(_em.outputs[0], _n.get("Material Output").inputs[0])
OUTLINE.use_backface_culling = True

def finish_obj(o, key, bevel=0.05, segs=2, outline=0.025, grad=True):
    o.data.materials.append(toon(key, grad=grad))
    if bevel:
        b = o.modifiers.new("Bevel", "BEVEL"); b.width = bevel; b.segments = segs
        b.limit_method = "ANGLE"; b.harden_normals = False
    if outline:
        o.data.materials.append(OUTLINE)
        s = o.modifiers.new("Outline", "SOLIDIFY")
        s.thickness = -outline; s.use_flip_normals = True; s.material_offset = 1; s.use_rim = False
    for p in o.data.polygons: p.use_smooth = False
    return o

# ---------- shape helpers ----------
def mesh_box(name, size, loc, rot=(0, 0, 0), cuts=0, deform=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    if cuts: bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    if deform:
        for v in bm.verts: v.co = deform(v.co.copy())
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
    o.location = loc; o.rotation_euler = rot
    return o

def jit(a): return random.uniform(-a, a)

W, D, H = 6.0, 5.0, 3.4

# walls: bulge in the middle, slight taper + lean = hand-made cartoon feel
def wall_deform(c):
    t = (c.z + H / 2) / H
    bulge = 1 + 0.05 * math.sin(math.pi * t)
    taper = 1 - 0.05 * t
    return Vector((c.x * bulge * taper + 0.10 * t, c.y * bulge * taper, c.z))
walls = mesh_box("walls", (W, D, H), (0, 0, H / 2 + 0.35), cuts=4, deform=wall_deform)
finish_obj(walls, "props.plaster", bevel=0.12, segs=3)

# stone foundation: individual chunky stones
for side in range(4):
    length = W if side % 2 == 0 else D
    n = int(length / 0.75)
    for i in range(n + 1):
        u = -length / 2 + i * (length / n)
        x, y = (u, -D / 2 - 0.05) if side == 0 else (u, D / 2 + 0.05) if side == 2 else \
               (-W / 2 - 0.05, u) if side == 3 else (W / 2 - 0.0, u)
        s = mesh_box(f"stone{side}_{i}", (0.7 + jit(0.12), 0.5 + jit(0.08), 0.45 + jit(0.1)),
                     (x, y, 0.22), rot=(0, 0, math.radians(90 if side % 2 else 0) + jit(0.15)))
        finish_obj(s, random.choice(["terrain.rock", "terrain.rock_dark", "terrain.rock"]), bevel=0.12, segs=2, outline=0.02)

# timber frame on the front + sides
beam = "props.wood_dark"
top = 0.35 + H
for x in (-W / 2 + 0.15, W / 2 - 0.05):
    finish_obj(mesh_box("post", (0.28, 0.28, H), (x, -D / 2 - 0.12, 0.35 + H / 2), rot=(0, jit(0.03), 0)), beam, 0.05)
finish_obj(mesh_box("beamTop", (W + 0.4, 0.3, 0.3), (0.08, -D / 2 - 0.14, top - 0.12), rot=(0, 0.015, 0)), beam, 0.05)
finish_obj(mesh_box("beamMid", (W + 0.2, 0.26, 0.24), (0.04, -D / 2 - 0.13, 0.35 + H * 0.55), rot=(0, -0.02, 0)), beam, 0.05)
for x, a in ((-2.15, 0.7), (2.25, -0.7)):
    finish_obj(mesh_box("brace", (0.22, 0.22, 1.9), (x, -D / 2 - 0.1, 0.35 + H * 0.78), rot=(0, a, 0)), beam, 0.04)

# sagging, overhanging roof (body) + chunky tile rows
RW, RD, RH = W / 2 + 0.7, D / 2 + 0.6, 2.6
def roof_deform(c):
    sag = 0.18 * math.cos(math.pi * c.y / (2 * RD)) if abs(c.y) <= RD else 0
    return Vector((c.x, c.y, c.z - sag * (1 - abs(c.x) / RW) * 0.6 + 0.0))
bm = bmesh.new()
pts = [(-RW, -RD, 0), (RW, -RD, 0), (0, -RD, RH), (-RW, RD, 0), (RW, RD, 0), (0, RD, RH)]
v = [bm.verts.new(p) for p in pts]
for f in [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]: bm.faces.new([v[i] for i in f])
bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4, use_grid_fill=True)
for vv in bm.verts: vv.co = roof_deform(vv.co)
me = bpy.data.meshes.new("roof"); bm.to_mesh(me); bm.free()
roof = bpy.data.objects.new("roof", me); bpy.context.collection.objects.link(roof)
roof.location = (0.1, 0, top - 0.05)
sol = roof.modifiers.new("thick", "SOLIDIFY"); sol.thickness = 0.25
finish_obj(roof, "props.roof_red", bevel=0.06, outline=0.03)

ang = math.atan2(RH, RW)
rows = 6
for sgn in (-1, 1):
    for r in range(rows):
        t = (r + 0.5) / rows
        x = sgn * RW * (1 - t)
        z = RH * t
        nx, nz = sgn * math.sin(ang) * 0.12, math.cos(ang) * 0.12
        tile = mesh_box(f"tiles{sgn}_{r}", (RW / math.cos(ang) / rows + 0.12, 2 * RD + 0.1, 0.12),
                        (0.1 + x + nx, 0, top - 0.05 + z + nz),
                        rot=(jit(0.01), sgn * ang + jit(0.03), 0), cuts=3,
                        deform=lambda c: Vector((c.x, c.y, c.z - 0.12 * math.cos(math.pi * c.y / (2 * RD)))))
        finish_obj(tile, "props.roof_red" if r % 2 else "#B9553F", bevel=0.04, outline=0.02, grad=False)

# leaning chimney with cap
ch = mesh_box("chimney", (0.9, 0.9, 2.6), (1.7, 1.2, top + 1.6), rot=(0.05, -0.09, 0), cuts=2,
              deform=lambda c: Vector((c.x * (1 - 0.12 * (c.z + 1.3) / 2.6), c.y * (1 - 0.12 * (c.z + 1.3) / 2.6), c.z)))
finish_obj(ch, "props.brick", bevel=0.08)
finish_obj(mesh_box("chimcap", (1.1, 1.1, 0.25), (1.82, 1.28, top + 2.95), rot=(0.05, -0.09, 0)), "terrain.rock_dark", 0.06)

# arched door
FY = -D / 2 - 0.2
finish_obj(mesh_box("doorframe", (1.7, 0.25, 2.5), (0.05, FY + 0.05, 0.35 + 1.25)), beam, 0.06)
finish_obj(mesh_box("door", (1.3, 0.25, 2.2), (0.05, FY - 0.02, 0.35 + 1.1)), "props.wood", 0.05)
bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.65, depth=0.25, location=(0.05, FY - 0.02, 0.35 + 2.2), rotation=(math.pi / 2, 0, 0))
arch = bpy.context.active_object; finish_obj(arch, "props.wood", 0.04)
for z in (0.9, 1.9):
    finish_obj(mesh_box("doorband", (1.35, 0.1, 0.12), (0.05, FY - 0.16, 0.35 + z)), "props.metal", 0.02, outline=0.015)
bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=6, radius=0.08, location=(0.55, FY - 0.2, 0.35 + 1.15))
finish_obj(bpy.context.active_object, "factions.ironclads_red.accent", 0, outline=0.01)

# windows with crossbars, shutters and flower boxes
for wx in (-1.95, 2.05):
    wz = 0.35 + 2.05
    finish_obj(mesh_box("winframe", (1.35, 0.25, 1.25), (wx, FY + 0.05, wz), rot=(0, jit(0.04), 0)), beam, 0.05)
    finish_obj(mesh_box("glass", (1.05, 0.2, 0.95), (wx, FY - 0.0, wz)), "#3E5F7A", 0, outline=0)
    finish_obj(mesh_box("barV", (0.1, 0.12, 1.0), (wx, FY - 0.1, wz)), beam, 0.02, outline=0.012)
    finish_obj(mesh_box("barH", (1.05, 0.12, 0.1), (wx, FY - 0.1, wz)), beam, 0.02, outline=0.012)
    for sx, a in ((-0.95, 0.35), (0.95, -0.35)):
        finish_obj(mesh_box("shutter", (0.55, 0.12, 1.3), (wx + sx, FY - 0.15, wz), rot=(0, 0, a)), "factions.tidewatch_blue.main", 0.04)
    finish_obj(mesh_box("flowerbox", (1.45, 0.4, 0.3), (wx, FY - 0.3, wz - 0.75)), "props.wood", 0.05)
    for i in range(6):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.17 + jit(0.03),
                                              location=(wx - 0.55 + i * 0.22, FY - 0.32 + jit(0.06), wz - 0.52 + jit(0.04)))
        finish_obj(bpy.context.active_object, random.choice(["props.leaf", "#D94A4A", "#F2C94C", "props.leaf_light"]), 0, outline=0.012, grad=False)

# ---------- scene dressing ----------
bpy.ops.mesh.primitive_plane_add(size=80, location=(0, 0, 0))
g = bpy.context.active_object; g.data.materials.append(toon("terrain.grass", grad=False))
bpy.ops.mesh.primitive_plane_add(size=1, location=(0.05, -6.5, 0.01)); p = bpy.context.active_object
p.scale = (2.0, 4.5, 1); p.data.materials.append(toon("terrain.path", grad=False))
for i, (bx, by) in enumerate(((-4.2, -3.4), (3.9, -3.5), (-4.4, 1.5))):
    for k in range(3):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.55 + jit(0.1), location=(bx + k * 0.45 + jit(0.1), by + jit(0.2), 0.4))
        finish_obj(bpy.context.active_object, random.choice(["props.leaf", "props.leaf_light"]), 0, outline=0.02)

# camera, sun, sky
scene = bpy.context.scene
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); scene.collection.objects.link(cam)
cam.location = (-11.5, -14.5, 5.2)
direction = Vector((0.2, 0, 3.0)) - cam.location
cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
cam.data.lens = 40
scene.camera = cam
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); scene.collection.objects.link(sun)
sun.data.energy = 4.0; sun.data.angle = math.radians(8)
sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
world = bpy.data.worlds.new("sky"); scene.world = world; world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = S.hex_to_linear(S.color("sky.day_top"))
world.node_tree.nodes["Background"].inputs[1].default_value = 1.0

for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
    try: scene.render.engine = eng; break
    except TypeError: pass
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "None"
try: scene.eevee.taa_render_samples = 32
except AttributeError: pass
scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
scene.render.filepath = os.path.join(OUT, "_cottage_v2.png")
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Cottage_v2.blend"))
print("DONE", scene.render.filepath)
