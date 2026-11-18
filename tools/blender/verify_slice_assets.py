"""Measure exported slice assets: bounds, pivot, door/window clearance (raycast). Renders a lineup preview.
Run: blender -b --factory-startup -P tools/blender/verify_slice_assets.py
Writes art/exports/_slice_report.txt and _slice_preview.png
"""
import bpy, os, sys, math
from mathutils import Vector
sys.path.append(os.path.dirname(__file__))
import mudfront_style as S

MODELS = os.path.join(S.ROOT, "Mudfront", "Assets", "_Mudfront", "Art", "Models")
REPORT = os.path.join(S.ROOT, "art", "exports", "_slice_report.txt")
S.reset_scene()
lines = []
x = 8.0  # keep lineup clear of the origin, where each new import is measured
objs = []
for f in sorted(os.listdir(MODELS)):
    if not f.endswith(".fbx"): continue
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(MODELS, f))
    new = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    o = new[0]
    bpy.context.view_layer.update()
    ws = [o.matrix_world @ Vector(c) for c in o.bound_box]
    mn = Vector((min(v.x for v in ws), min(v.y for v in ws), min(v.z for v in ws)))
    mx = Vector((max(v.x for v in ws), max(v.y for v in ws), max(v.z for v in ws)))
    size = mx - mn
    lines.append(f"{f:36s} size {size.x:5.2f} x {size.y:5.2f} x {size.z:5.2f} m   pivot->min z {mn.z - o.location.z:+.2f}")
    if "Door" in f or "Window" in f:
        depsg = bpy.context.evaluated_depsgraph_get()
        # sweep horizontal rays through the opening at several heights (ray along wall thickness axis)
        thick_axis_y = size.y < size.x
        clear_w = None
        for zc in ([0.3, 1.0, 1.8, 2.4] if "Door" in f else [1.2, 1.6, 2.0]):
            hits = []
            for i in range(-200, 201):
                p = i * 0.01
                origin = Vector((p, -5, zc)) if thick_axis_y else Vector((-5, p, zc))
                d = Vector((0, 1, 0)) if thick_axis_y else Vector((1, 0, 0))
                hit = bpy.context.scene.ray_cast(depsg, origin + o.location, d)[0]
                hits.append(hit)
            # longest run of misses
            best = cur = 0
            for h in hits:
                cur = 0 if h else cur + 1; best = max(best, cur)
            lines.append(f"    clear width at z={zc:.1f}: {best * 0.01:.2f} m")
        # clear height at centre
        for zc_i in range(0, 400):
            z = zc_i * 0.01 + 0.05
            origin = Vector((0, -5, z)) if thick_axis_y else Vector((-5, 0, z))
            d = Vector((0, 1, 0)) if thick_axis_y else Vector((1, 0, 0))
            if bpy.context.scene.ray_cast(depsg, origin + o.location, d)[0] and z > 0.5:
                lines.append(f"    clear height (centre): {z:.2f} m"); break
    o.location.x += x
    x += size.x + 1.5
    objs.append(o)

open(REPORT, "w").write("\n".join(lines))
print("\n".join(lines))

# lineup render
for o in objs:
    for mat in o.data.materials:
        if mat: mat.diffuse_color = mat.diffuse_color
bpy.ops.mesh.primitive_plane_add(size=200, location=(x / 2, 0, 0))
bpy.context.active_object.data.materials.append(S.mat("terrain.grass"))
sc = bpy.context.scene
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam)
cam.location = (x / 2, -32, 12)
cam.rotation_euler = (Vector((x / 2, 0, 1.2)) - cam.location).to_track_quat("-Z", "Y").to_euler()
cam.data.lens = 22
sc.camera = cam
sc.render.engine = "BLENDER_WORKBENCH"
sh = sc.display.shading
sh.light = "STUDIO"; sh.color_type = "MATERIAL"; sh.show_shadows = True; sh.show_object_outline = True
sh.show_cavity = True; sh.cavity_type = "BOTH"
sc.view_settings.view_transform = "Standard"
sc.render.resolution_x, sc.render.resolution_y = 2000, 800
sc.render.filepath = os.path.join(S.ROOT, "art", "exports", "_slice_preview.png")
bpy.ops.render.render(write_still=True)
print("VERIFY DONE")
