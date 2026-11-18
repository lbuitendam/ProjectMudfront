"""Toon-shaded showcase of the exported slice assets (how they should read in-game).
Run: blender -b --factory-startup -P tools/blender/render_toon_lineup.py
Out: art/exports/_toon_lineup.png, art/exports/_toon_character.png
"""
import bpy, math, os, sys
from mathutils import Vector
sys.path.append(os.path.dirname(__file__))
import mudfront_style as S

MODELS = os.path.join(S.ROOT, "Mudfront", "Assets", "_Mudfront", "Art", "Models")
OUT = os.path.join(S.ROOT, "art", "exports")
S.reset_scene()

OUTLINE = bpy.data.materials.new("ink")
OUTLINE.use_nodes = True
n = OUTLINE.node_tree.nodes
n.remove(n.get("Principled BSDF"))
em = n.new("ShaderNodeEmission"); em.inputs[0].default_value = (0.05, 0.035, 0.03, 1)
OUTLINE.node_tree.links.new(em.outputs[0], n.get("Material Output").inputs[0])
OUTLINE.use_backface_culling = True


def toonify(m):
    if m is None or m.get("toon"):
        return
    base = (0.8, 0.8, 0.8, 1)
    if m.use_nodes and m.node_tree.nodes.get("Principled BSDF"):
        base = tuple(m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value)
    nt = m.node_tree; N = nt.nodes; L = nt.links
    for x in list(N): N.remove(x)
    out = N.new("ShaderNodeOutputMaterial"); diff = N.new("ShaderNodeBsdfDiffuse")
    s2r = N.new("ShaderNodeShaderToRGB"); ramp = N.new("ShaderNodeValToRGB")
    e = ramp.color_ramp; e.interpolation = "CONSTANT"
    e.elements[0].position = 0.0; e.elements[0].color = (0.50, 0.48, 0.62, 1)
    e.elements[1].position = 0.2; e.elements[1].color = (0.82, 0.80, 0.86, 1)
    e.elements.new(0.55).color = (1.06, 1.0, 0.92, 1)
    mul = N.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1
    mul.inputs[6].default_value = base
    emi = N.new("ShaderNodeEmission")
    L.new(diff.outputs[0], s2r.inputs[0]); L.new(s2r.outputs[0], ramp.inputs[0])
    L.new(ramp.outputs[0], mul.inputs[7]); L.new(mul.outputs[2], emi.inputs[0]); L.new(emi.outputs[0], out.inputs[0])
    m["toon"] = True


def load(name, loc, rot_z=0.0):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(MODELS, name + ".fbx"))
    objs = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    for o in objs:
        o.location = Vector(loc); o.rotation_euler[2] += math.radians(rot_z)
        for m in o.data.materials: toonify(m)
        if OUTLINE.name not in [m.name for m in o.data.materials if m]:
            o.data.materials.append(OUTLINE)
            s = o.modifiers.new("ink", "SOLIDIFY"); s.thickness = -0.018; s.use_flip_normals = True
            s.material_offset = len(o.data.materials) - 1; s.use_rim = False
    return objs


# a small hut from the kit + props + vehicle + troopers
hut = Vector((-7, 4, 0))
load("SM_Kit_WallDoor_4m", hut + Vector((0, -2, 0)))
load("SM_Kit_WallWindow_4m", hut + Vector((-2, 0, 0)), 90)
load("SM_Kit_Wall_4m", hut + Vector((0, 2, 0)), 180)
load("SM_Kit_WallWindow_4m", hut + Vector((2, 0, 0)), -90)
for c in ((-2, -2), (2, -2), (-2, 2), (2, 2)):
    load("SM_Kit_CornerPost", hut + Vector((c[0], c[1], 0)))
load("SM_Kit_RoofSlope_4m", hut + Vector((-1.95, 0, 3.2)))
load("SM_Kit_RoofSlope_4m", hut + Vector((1.95, 0, 3.2)), 180)
load("SM_Cover_SandbagsLow_4m", (-1.5, -3.5, 0), 8)
load("SM_Cover_CrateStack", (2.2, -1.0, 0), -20)
load("SM_Cover_BarricadeHigh_4m", (6.5, 3.5, 0), -15)
load("SM_Veh_Mudskipper", (7.5, -2.0, 0), 35)
load("SM_Char_Trooper_Placeholder", (0.3, -5.2, 0), 15)
load("SM_Char_Trooper_Placeholder", (3.4, -4.4, 0), -25)

bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, 0))
g = bpy.context.active_object; g.data.materials.append(S.mat("terrain.grass")); toonify(g.data.materials[0])
bpy.ops.mesh.primitive_plane_add(size=1, location=(0.5, -5, 0.01)); p = bpy.context.active_object
p.scale = (5, 9, 1); p.rotation_euler[2] = math.radians(20)
p.data.materials.append(S.mat("terrain.path")); toonify(p.data.materials[0])

sc = bpy.context.scene
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 4.0; sun.data.angle = math.radians(6)
sun.rotation_euler = (math.radians(52), math.radians(12), math.radians(-40))
w = bpy.data.worlds.new("sky"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = S.hex_to_linear(S.color("sky.day_top"))
for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
    try: sc.render.engine = eng; break
    except TypeError: pass
sc.view_settings.view_transform = "Standard"
try: sc.eevee.taa_render_samples = 32
except AttributeError: pass

cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam


def shot(file, pos, target, lens, res=(1800, 1000)):
    cam.location = pos
    cam.rotation_euler = (Vector(target) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = lens
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.filepath = os.path.join(OUT, file)
    bpy.ops.render.render(write_still=True)


shot("_toon_lineup.png", (-4, -17, 6.5), (0.5, 0, 1.6), 30)
shot("_toon_character.png", (0.9, -8.4, 1.45), (0.3, -5.2, 1.05), 60, (1200, 1200))
print("TOON DONE")
