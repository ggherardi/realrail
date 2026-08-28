import bpy
import math
import os

# Enemy_Grunt v1 - intentionally simple, static, and editable procedural source.
# Coordinate convention: Z is up, and the robot's front is local -Y.

OUT_DIR = os.path.dirname(os.path.abspath(__file__))
BLEND_PATH = os.path.join(OUT_DIR, "Enemy_Grunt.blend")
FBX_PATH = os.path.join(OUT_DIR, "Enemy_Grunt.fbx")


def material(name, color, metallic, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


IVORY = material("M_Grunt_Ivory", (0.58, 0.55, 0.48), 0.72, 0.42)
DARK = material("M_Grunt_Mechanical", (0.055, 0.065, 0.07), 0.84, 0.3)
RED = material("M_Grunt_RedSensor", (0.8, 0.025, 0.015), 0.45, 0.25)


def add_box(name, loc, scale, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] * 0.5, scale[1] * 0.5, scale[2] * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("Small mechanical bevel", 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(mat)
    return obj


def add_cylinder(name, loc, radius, depth, mat, vertices=10, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    if rotation:
        obj.rotation_euler = rotation
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    obj.data.materials.append(mat)
    return obj


def add_uvsphere(name, loc, scale, mat, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    return obj


def add_joint(name, loc, radius=0.105):
    return add_cylinder(name, loc, radius, 0.13, DARK, 10, (math.pi / 2, 0, 0))


# Clear the default scene, retaining only the geometry authored below.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
    pass

root = bpy.data.objects.new("Enemy_Grunt", None)
bpy.context.collection.objects.link(root)
root.empty_display_type = 'PLAIN_AXES'

parts = []
def part(obj):
    obj.parent = root
    parts.append(obj)
    return obj

# Feet and legs. Their broad spacing gives a clear silhouette from the game camera.
for x, side in ((-0.27, "L"), (0.27, "R")):
    part(add_box("Foot_" + side, (x, -0.055, 0.085), (0.27, 0.43, 0.17), DARK, 0.025))
    part(add_box("ToeArmor_" + side, (x, -0.18, 0.14), (0.22, 0.19, 0.11), IVORY, 0.018))
    part(add_joint("Ankle_" + side, (x, 0.055, 0.255), 0.085))
    part(add_box("Shin_" + side, (x, 0.035, 0.44), (0.17, 0.19, 0.30), DARK, 0.02))
    part(add_box("ShinArmor_" + side, (x, -0.075, 0.46), (0.19, 0.06, 0.22), IVORY, 0.014))
    part(add_joint("Knee_" + side, (x, -0.015, 0.63), 0.105))
    part(add_box("Thigh_" + side, (x, 0.035, 0.81), (0.21, 0.22, 0.28), DARK, 0.02))
    part(add_box("ThighArmor_" + side, (x, -0.095, 0.84), (0.23, 0.055, 0.20), IVORY, 0.014))

# Compact pelvis and torso.
part(add_box("Pelvis", (0, 0.03, 0.96), (0.66, 0.34, 0.20), DARK, 0.035))
part(add_box("PelvisPlate", (0, -0.165, 0.99), (0.40, 0.045, 0.13), IVORY, 0.012))
part(add_uvsphere("TorsoShell", (0, 0.02, 1.30), (0.48, 0.31, 0.43), IVORY, 12, 7))
part(add_box("TorsoLower", (0, 0.04, 1.14), (0.77, 0.46, 0.23), IVORY, 0.04))
part(add_box("Backpack", (0, 0.265, 1.31), (0.46, 0.13, 0.35), DARK, 0.025))

# Front eye points toward -Y / exported Unity -Z.
part(add_cylinder("EyeHousing", (0, -0.325, 1.35), 0.195, 0.105, DARK, 12, (math.pi / 2, 0, 0)))
part(add_cylinder("RedFrontSensor", (0, -0.39, 1.35), 0.115, 0.035, RED, 12, (math.pi / 2, 0, 0)))
part(add_box("ChestStripe", (0, -0.305, 1.56), (0.08, 0.024, 0.23), RED, 0.006))

# Simple arms hang at the side without looking like weapons.
for x, side in ((-0.56, "L"), (0.56, "R")):
    part(add_joint("Shoulder_" + side, (x, 0.0, 1.43), 0.15))
    part(add_box("UpperArm_" + side, (x, -0.01, 1.20), (0.16, 0.18, 0.28), DARK, 0.02))
    part(add_box("UpperArmArmor_" + side, (x, -0.105, 1.22), (0.18, 0.035, 0.19), IVORY, 0.012))
    part(add_joint("Elbow_" + side, (x, -0.005, 1.02), 0.09))
    part(add_box("Forearm_" + side, (x, -0.02, 0.86), (0.15, 0.17, 0.24), DARK, 0.018))
    part(add_box("Hand_" + side, (x, -0.05, 0.70), (0.18, 0.14, 0.12), DARK, 0.02))
    part(add_cylinder("ArmLight_" + side, (x, -0.115, 0.88), 0.035, 0.018, RED, 8, (math.pi / 2, 0, 0)))

# A small antenna preserves a clear top-down cue while keeping overall height at 1.8 units.
part(add_cylinder("Antenna", (0, 0.04, 1.67), 0.018, 0.16, DARK, 8))
part(add_uvsphere("AntennaTip", (0, 0.04, 1.76), (0.04, 0.04, 0.04), RED, 8, 4))

# Apply final transforms and reduce scene hierarchy to three material-separated meshes.
bpy.context.view_layer.objects.active = root
parts_by_material = {
    mat: [p for p in parts if p.data.materials and p.data.materials[0] == mat]
    for mat in (IVORY, DARK, RED)
}
for mat, merged_name in ((IVORY, "Enemy_Grunt_Ivory"), (DARK, "Enemy_Grunt_Dark"), (RED, "Enemy_Grunt_Red")):
    selected = parts_by_material[mat]
    bpy.ops.object.select_all(action='DESELECT')
    for p in selected:
        p.select_set(True)
    bpy.context.view_layer.objects.active = selected[0]
    bpy.ops.object.join()
    selected[0].name = merged_name
    selected[0].parent = root

root["asset_contract"] = "Static visual only; no colliders, rigidbodies, scripts, rigs, or animations. Front is -Y in Blender / -Z in Unity export."
root["unity_height_units"] = 1.8

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
for child in root.children:
    child.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.fbx(
    filepath=FBX_PATH,
    use_selection=True,
    object_types={'EMPTY', 'MESH'},
    use_mesh_modifiers=True,
    add_leaf_bones=False,
    bake_anim=False,
    axis_forward='-Z',
    axis_up='Y',
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',
    path_mode='AUTO',
)

print("EXPORTED", BLEND_PATH)
print("EXPORTED", FBX_PATH)
print("ROOT", root.name, "children", len(root.children))
