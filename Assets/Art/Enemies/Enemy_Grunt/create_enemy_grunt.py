import bpy
import math
import os

# Enemy_Grunt v2: reproducible visual-only rigid rig. Blender is Z-up and the
# robot faces -Y, which exports to Unity -Z.
OUT_DIR = os.path.dirname(os.path.abspath(__file__))
BLEND_PATH = os.path.join(OUT_DIR, "Enemy_Grunt.blend")
FBX_PATH = os.path.join(OUT_DIR, "Enemy_Grunt.fbx")


def material(name, color, metallic, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
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
    obj.name, obj.scale = name, tuple(value * 0.5 for value in scale)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Small mechanical bevel", 'BEVEL')
        modifier.width, modifier.segments = bevel, 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
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
    obj.name, obj.scale = name, scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    return obj


def add_joint(name, loc, radius=0.105):
    return add_cylinder(name, loc, radius, 0.13, DARK, 10, (math.pi / 2, 0, 0))


def make_armature(root):
    data = bpy.data.armatures.new("Enemy_Grunt_Rig")
    armature = bpy.data.objects.new("Enemy_Grunt_Rig", data)
    bpy.context.collection.objects.link(armature)
    armature.parent = root
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    # Root has no deformation or animation. All other bones rigidly own parts.
    definitions = (
        ("GR_Root", (0, 0, 0), (0, 0, .25), None, False),
        ("GR_Pelvis", (0, 0, .90), (0, 0, 1.10), "GR_Root", True),
        ("GR_Torso", (0, 0, 1.08), (0, 0, 1.61), "GR_Pelvis", True),
        ("GR_Arm_L", (-.56, 0, 1.43), (-.56, 0, .67), "GR_Torso", True),
        ("GR_Arm_R", (.56, 0, 1.43), (.56, 0, .67), "GR_Torso", True),
        ("GR_Thigh_L", (-.27, .035, .94), (-.27, .035, .64), "GR_Pelvis", True),
        ("GR_Shin_L", (-.27, .035, .64), (-.27, .035, .27), "GR_Thigh_L", True),
        ("GR_Foot_L", (-.27, .055, .27), (-.27, -.14, .11), "GR_Shin_L", True),
        ("GR_Thigh_R", (.27, .035, .94), (.27, .035, .64), "GR_Pelvis", True),
        ("GR_Shin_R", (.27, .035, .64), (.27, .035, .27), "GR_Thigh_R", True),
        ("GR_Foot_R", (.27, .055, .27), (.27, -.14, .11), "GR_Shin_R", True),
    )
    for name, head, tail, parent, deform in definitions:
        bone = data.edit_bones.new(name)
        bone.head, bone.tail, bone.use_deform = head, tail, deform
        if parent:
            bone.parent, bone.use_connect = data.edit_bones[parent], False
    bpy.ops.object.mode_set(mode='OBJECT')
    armature.select_set(False)
    return armature


def create_walk_action(armature):
    action = bpy.data.actions.new("Walk")
    action.use_frame_range, action.frame_start, action.frame_end, action.use_cyclic = True, 1, 25, True
    armature.animation_data_create()
    armature.animation_data.action = action
    # contact, passing, opposite contact, passing, repeated contact.
    # GR_Root is deliberately absent: no root rotation or translation exists.
    poses = {
        "GR_Pelvis": [(0, 0), (.018, .025), (0, 0), (.018, -.025), (0, 0)],
        "GR_Torso": [(0, 0), (0, -.035), (0, 0), (0, .035), (0, 0)],
        "GR_Arm_L": [(-.34, 0), (0, 0), (.34, 0), (0, 0), (-.34, 0)],
        "GR_Arm_R": [(.34, 0), (0, 0), (-.34, 0), (0, 0), (.34, 0)],
        "GR_Thigh_L": [(.34, 0), (0, 0), (-.34, 0), (0, 0), (.34, 0)],
        "GR_Thigh_R": [(-.34, 0), (0, 0), (.34, 0), (0, 0), (-.34, 0)],
        "GR_Shin_L": [(.05, 0), (.38, 0), (.05, 0), (.12, 0), (.05, 0)],
        "GR_Shin_R": [(.05, 0), (.12, 0), (.05, 0), (.38, 0), (.05, 0)],
        "GR_Foot_L": [(-.16, 0), (-.26, 0), (-.16, 0), (.04, 0), (-.16, 0)],
        "GR_Foot_R": [(-.16, 0), (.04, 0), (-.16, 0), (-.26, 0), (-.16, 0)],
    }
    for bone_name, values in poses.items():
        pose_bone = armature.pose.bones[bone_name]
        pose_bone.rotation_mode = 'XYZ'
        for frame, (x_rotation, z_rotation) in zip((1, 7, 13, 19, 25), values):
            pose_bone.rotation_euler = (x_rotation, 0, z_rotation)
            pose_bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone_name)
    # Blender's default Bezier interpolation gives the intended gentle, looping
    # mechanical motion. (Action fcurve access differs between Blender 4/5.)
    return action


bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
root = bpy.data.objects.new("Enemy_Grunt", None)
bpy.context.collection.objects.link(root)
root.empty_display_type = 'PLAIN_AXES'
armature = make_armature(root)
parts = []


def part(obj, bone_name):
    obj.parent = root
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    parts.append(obj)
    return obj


# Feet and legs retain the v1 silhouette; each component receives one full weight.
for x, side in ((-.27, "L"), (.27, "R")):
    foot, shin, thigh = "GR_Foot_" + side, "GR_Shin_" + side, "GR_Thigh_" + side
    part(add_box("Foot_" + side, (x, -.055, .085), (.27, .43, .17), DARK, .025), foot)
    part(add_box("ToeArmor_" + side, (x, -.18, .14), (.22, .19, .11), IVORY, .018), foot)
    part(add_joint("Ankle_" + side, (x, .055, .255), .085), foot)
    part(add_box("Shin_" + side, (x, .035, .44), (.17, .19, .30), DARK, .02), shin)
    part(add_box("ShinArmor_" + side, (x, -.075, .46), (.19, .06, .22), IVORY, .014), shin)
    part(add_joint("Knee_" + side, (x, -.015, .63), .105), shin)
    part(add_box("Thigh_" + side, (x, .035, .81), (.21, .22, .28), DARK, .02), thigh)
    part(add_box("ThighArmor_" + side, (x, -.095, .84), (.23, .055, .20), IVORY, .014), thigh)

part(add_box("Pelvis", (0, .03, .96), (.66, .34, .20), DARK, .035), "GR_Pelvis")
part(add_box("PelvisPlate", (0, -.165, .99), (.40, .045, .13), IVORY, .012), "GR_Pelvis")
part(add_uvsphere("TorsoShell", (0, .02, 1.30), (.48, .31, .43), IVORY, 12, 7), "GR_Torso")
part(add_box("TorsoLower", (0, .04, 1.14), (.77, .46, .23), IVORY, .04), "GR_Torso")
part(add_box("Backpack", (0, .265, 1.31), (.46, .13, .35), DARK, .025), "GR_Torso")
part(add_cylinder("EyeHousing", (0, -.325, 1.35), .195, .105, DARK, 12, (math.pi / 2, 0, 0)), "GR_Torso")
part(add_cylinder("RedFrontSensor", (0, -.39, 1.35), .115, .035, RED, 12, (math.pi / 2, 0, 0)), "GR_Torso")
part(add_box("ChestStripe", (0, -.305, 1.56), (.08, .024, .23), RED, .006), "GR_Torso")

for x, side in ((-.56, "L"), (.56, "R")):
    arm = "GR_Arm_" + side
    part(add_joint("Shoulder_" + side, (x, 0, 1.43), .15), arm)
    part(add_box("UpperArm_" + side, (x, -.01, 1.20), (.16, .18, .28), DARK, .02), arm)
    part(add_box("UpperArmArmor_" + side, (x, -.105, 1.22), (.18, .035, .19), IVORY, .012), arm)
    part(add_joint("Elbow_" + side, (x, -.005, 1.02), .09), arm)
    part(add_box("Forearm_" + side, (x, -.02, .86), (.15, .17, .24), DARK, .018), arm)
    part(add_box("Hand_" + side, (x, -.05, .70), (.18, .14, .12), DARK, .02), arm)
    part(add_cylinder("ArmLight_" + side, (x, -.115, .88), .035, .018, RED, 8, (math.pi / 2, 0, 0)), arm)

part(add_cylinder("Antenna", (0, .04, 1.67), .018, .16, DARK, 8), "GR_Torso")
part(add_uvsphere("AntennaTip", (0, .04, 1.76), (.04, .04, .04), RED, 8, 4), "GR_Torso")

# Preserve three material sections after joining, retaining all rigid groups.
meshes = []
parts_by_material = {mat: [obj for obj in parts if obj.data.materials[0] == mat]
                     for mat in (IVORY, DARK, RED)}
for mat, name in ((IVORY, "Enemy_Grunt_Ivory"), (DARK, "Enemy_Grunt_Dark"), (RED, "Enemy_Grunt_Red")):
    selected = parts_by_material[mat]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in selected:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = selected[0]
    bpy.ops.object.join()
    mesh = selected[0]
    mesh.name, mesh.parent = name, root
    modifier = mesh.modifiers.new("Enemy_Grunt_Rig", 'ARMATURE')
    modifier.object = armature
    meshes.append(mesh)

walk = create_walk_action(armature)
root["asset_contract"] = "Visual-only skinned robot; no colliders, rigidbodies, scripts, or gameplay motion. Walk animates local bones only; GR_Root has no animation. Front is -Y in Blender / -Z in Unity."
root["unity_height_units"] = 1.8
root["rig_type"] = "Generic rigid mechanical rig"
root["animation"] = "Walk, 24 fps, looped, visual-only"
bpy.context.scene.render.fps = 24
bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 25
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
armature.select_set(True)
for mesh in meshes:
    mesh.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.fbx(
    filepath=FBX_PATH, use_selection=True, object_types={'EMPTY', 'ARMATURE', 'MESH'},
    use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=True,
    bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
    # There is exactly one authored action; exporting all actions names the FBX
    # take after it (Walk) instead of Blender's generic scene AnimStack.
    bake_anim_use_all_actions=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS', path_mode='AUTO')
print("EXPORTED", BLEND_PATH)
print("EXPORTED", FBX_PATH)
print("ROOT", root.name, "meshes", len(meshes), "bones", len(armature.data.bones), "action", walk.name)
