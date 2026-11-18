"""Render a quick lineup preview of all exported models.
Run: blender -b -P tools/blender/render_preview.py  -> art/exports/_preview.png
"""
import bpy, os, sys, math
sys.path.append(os.path.dirname(__file__))
import mudfront_style as S

S.reset_scene()
names = ["SM_House_Cottage_A", "SM_Build_WallWood_4m", "SM_Build_Sandbags_2m", "SM_Tree_Round_A", "SM_Prop_Crate_A"]
xs = [-8.5, -1.5, 3.0, 7.0, 10.5]
for n, x in zip(names, xs):
    path = os.path.join(S.EXPORT_DIR, n + ".blend")
    with bpy.data.libraries.load(path) as (src, dst):
        dst.objects = [o for o in src.objects if o == n]
    for o in dst.objects:
        bpy.context.collection.objects.link(o)
        o.location = (x, 0, 0)

# ground
bpy.ops.mesh.primitive_plane_add(size=60, location=(1, 4, 0))
bpy.context.active_object.data.materials.append(S.mat("terrain.grass"))

# 1.8 m reference figure (capsule-ish) for scale
bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.3, depth=1.4, location=(0.8, -2.5, 0.7))
bpy.context.active_object.data.materials.append(S.mat("factions.tidewatch_blue.main"))
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.32, location=(0.8, -2.5, 1.62))
bpy.context.active_object.data.materials.append(S.mat("props.plaster"))

scene = bpy.context.scene
cam_data = bpy.data.cameras.new("cam")
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
cam.location = (2.0, -22.0, 9.0)
cam.rotation_euler = (math.radians(70), 0, 0)
cam_data.lens = 32
scene.camera = cam

scene.render.engine = "BLENDER_WORKBENCH"
sh = scene.display.shading
sh.light = "STUDIO"
sh.color_type = "MATERIAL"
sh.show_shadows = True
sh.show_cavity = True
sh.cavity_type = "BOTH"
sh.show_object_outline = True
sh.background_type = "VIEWPORT"
sh.background_color = (0.37, 0.66, 0.91)
scene.world = scene.world or bpy.data.worlds.new("w")
scene.render.resolution_x, scene.render.resolution_y = 1600, 800
scene.render.filepath = os.path.join(S.EXPORT_DIR, "_preview.png")
bpy.ops.render.render(write_still=True)
print("Preview ->", scene.render.filepath)
