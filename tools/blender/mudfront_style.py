"""Shared style helpers for Project Mudfront models (Blender 4.x).

Look: chunky, slightly crooked, bevelled edges, flat colours from palette.json.
Scale: 1 Blender unit = 1 m. Origin at the bottom centre of each model.
"""
import bpy, bmesh, json, math, os, random
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PALETTE = json.load(open(os.path.join(ROOT, "art", "palette", "palette.json"), encoding="utf-8"))
EXPORT_DIR = os.path.join(ROOT, "art", "exports")


def color(path):
    """'props.wood' -> hex from palette."""
    node = PALETTE
    for key in path.split("."):
        node = node[key]
    return node


def hex_to_linear(h):
    h = h.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (*lin, 1.0)


_mats = {}


def mat(path):
    """Flat toon-ish material named after the palette key (Unity remaps by name)."""
    name = "M_" + path.replace(".", "_")
    if name in _mats:
        return _mats[name]
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = hex_to_linear(color(path))
    bsdf.inputs["Roughness"].default_value = 0.9
    m.diffuse_color = hex_to_linear(color(path))
    _mats[name] = m
    return m


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)
    _mats.clear()


def box(name, size, loc, material, bevel=0.04, wobble=0.0, rot=(0, 0, 0)):
    """Bevelled box. size=(x,y,z) in m, loc = centre. wobble = random vertex jitter (crooked look)."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    if wobble:
        for v in o.data.vertices:
            v.co += Vector((random.uniform(-wobble, wobble) for _ in range(3)))
    if bevel:
        mod = o.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
    o.data.materials.append(mat(material))
    return o


def cylinder(name, radius, depth, loc, material, verts=8, rot=(0, 0, 0), bevel=0.02):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    if bevel:
        mod = o.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
    o.data.materials.append(mat(material))
    return o


def ico(name, radius, loc, material, subdiv=1, squash=(1, 1, 1)):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=radius, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = squash
    bpy.ops.object.transform_apply(scale=True)
    o.data.materials.append(mat(material))
    return o


def prism_roof(name, width, depth, height, loc, material, overhang=0.3, wobble=0.0):
    """Gable roof: triangular prism along Y."""
    w, d, h = width / 2 + overhang, depth / 2 + overhang, height
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    v = [bm.verts.new(p) for p in [(-w, -d, 0), (w, -d, 0), (0, -d, h), (-w, d, 0), (w, d, 0), (0, d, h)]]
    for f in [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]:
        bm.faces.new([v[i] for i in f])
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(o)
    o.location = loc
    if wobble:
        for vert in o.data.vertices:
            vert.co += Vector((random.uniform(-wobble, wobble) for _ in range(3)))
    mod = o.modifiers.new("Solidify", "SOLIDIFY")
    mod.thickness = 0.15
    o.data.materials.append(mat(material))
    return o


def finish(model_name):
    """Apply modifiers, flat shade, join into one object, origin bottom-centre, export FBX."""
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        for m in list(o.modifiers):
            bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = model_name
    bpy.ops.object.shade_flat()
    # origin at bottom centre
    zs = [(o.matrix_world @ v.co).z for v in o.data.vertices]
    bpy.context.scene.cursor.location = (o.location.x, o.location.y, min(zs))
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    o.location = (0, 0, 0)
    os.makedirs(EXPORT_DIR, exist_ok=True)
    path = os.path.join(EXPORT_DIR, model_name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True, bake_space_transform=True,
        mesh_smooth_type="FACE", use_mesh_modifiers=True,
    )
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(EXPORT_DIR, model_name + ".blend"))
    print("Exported", path)
    return o
