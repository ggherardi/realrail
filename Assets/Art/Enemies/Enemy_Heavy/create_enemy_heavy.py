import bpy
import math
import os

# Enemy_Heavy v1: reproducible visual-only rigid robot. Blender is Z-up and
# the robot faces -Y, which exports to Unity -Z. This file is intentionally
# self-contained so the asset can be regenerated without shared tool code.
OUT_DIR = os.path.dirname(os.path.abspath(__file__))
BLEND_PATH = os.path.join(OUT_DIR, "Enemy_Heavy.blend")
FBX_PATH = os.path.join(OUT_DIR, "Enemy_Heavy.fbx")


def material(name, color, metallic, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


IVORY = material("M_Heavy_Ivory", (0.58, 0.55, 0.48), 0.72, 0.42)
DARK = material("M_Heavy_Mechanical", (0.055, 0.065, 0.07), 0.84, 0.30)
RED = material("M_Heavy_RedSensor", (0.8, 0.025, 0.015), 0.45, 0.25)


def add_box(name, loc, dimensions, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    obj = bpy.context.object
    obj.name, obj.scale = name, tuple(value * 0.5 for value in dimensions)
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


def add_joint(name, loc, radius=0.12):
    return add_cylinder(name, loc, radius, 0.16, DARK, 10, (math.pi / 2, 0, 0))


def make_armature(root):
    data = bpy.data.armatures.new("Enemy_Heavy_Rig")
    armature = bpy.data.objects.new("Enemy_Heavy_Rig", data)
    bpy.context.collection.objects.link(armature)
    armature.parent = root
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    # Root is neither deforming nor animated. The other ten bones each own
    # rigid visual parts; this mirrors the compact Grunt topology.
    definitions = (
        ("HV_Root", (0, 0, 0), (0, 0, .25), None, False),
        ("HV_Pelvis", (0, 0, 1.20), (0, 0, 1.43), "HV_Root", True),
        ("HV_Torso", (0, 0, 1.41), (0, 0, 2.32), "HV_Pelvis", True),
        ("HV_Arm_L", (-.65, 0, 2.08), (-.65, 0, 1.02), "HV_Torso", True),
        ("HV_Arm_R", (.65, 0, 2.08), (.65, 0, 1.02), "HV_Torso", True),
        ("HV_Thigh_L", (-.31, .04, 1.22), (-.31, .04, .76), "HV_Pelvis", True),
        ("HV_Shin_L", (-.31, .04, .76), (-.31, .04, .20), "HV_Thigh_L", True),
        ("HV_Foot_L", (-.31, .06, .20), (-.31, -.16, .07), "HV_Shin_L", True),
        ("HV_Thigh_R", (.31, .04, 1.22), (.31, .04, .76), "HV_Pelvis", True),
        ("HV_Shin_R", (.31, .04, .76), (.31, .04, .20), "HV_Thigh_R", True),
        ("HV_Foot_R", (.31, .06, .20), (.31, -.16, .07), "HV_Shin_R", True),
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
    # Heavy has a deliberate, short-step gait. HV_Root must remain absent:
    # world movement is exclusively EnemyMover's responsibility in Unity.
    poses = {
        "HV_Pelvis": [(0, 0), (.010, .018), (0, 0), (.010, -.018), (0, 0)],
        "HV_Torso": [(0, -.025), (.018, -.045), (0, .025), (-.018, .045), (0, -.025)],
        "HV_Arm_L": [(-.22, 0), (-.04, 0), (.22, 0), (.04, 0), (-.22, 0)],
        "HV_Arm_R": [(.22, 0), (.04, 0), (-.22, 0), (-.04, 0), (.22, 0)],
        "HV_Thigh_L": [(.22, 0), (.04, 0), (-.22, 0), (-.04, 0), (.22, 0)],
        "HV_Thigh_R": [(-.22, 0), (-.04, 0), (.22, 0), (.04, 0), (-.22, 0)],
        "HV_Shin_L": [(.04, 0), (.27, 0), (.04, 0), (.10, 0), (.04, 0)],
        "HV_Shin_R": [(.04, 0), (.10, 0), (.04, 0), (.27, 0), (.04, 0)],
        "HV_Foot_L": [(-.10, 0), (-.18, 0), (-.10, 0), (.025, 0), (-.10, 0)],
        "HV_Foot_R": [(-.10, 0), (.025, 0), (-.10, 0), (-.18, 0), (-.10, 0)],
    }
    for bone_name, values in poses.items():
        pose_bone = armature.pose.bones[bone_name]
        pose_bone.rotation_mode = 'XYZ'
        for frame, (x_rotation, z_rotation) in zip((1, 7, 13, 19, 25), values):
            pose_bone.rotation_euler = (x_rotation, 0, z_rotation)
            pose_bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone_name)
    return action


bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
root = bpy.data.objects.new("Enemy_Heavy", None)
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


# The tall stance is the Heavy's primary crowd-readability cue. It retains a
# broad armored core, but narrows shoulders and feet relative to v1 so width
# does not dominate the silhouette.
for x, side in ((-.31, "L"), (.31, "R")):
    foot, shin, thigh = "HV_Foot_" + side, "HV_Shin_" + side, "HV_Thigh_" + side
    part(add_box("Foot_" + side, (x, -.055, .085), (.34, .52, .17), DARK, .028), foot)
    part(add_box("ToeArmor_" + side, (x, -.20, .14), (.29, .24, .12), IVORY, .020), foot)
    part(add_joint("Ankle_" + side, (x, .055, .205), .11), foot)
    part(add_box("Shin_" + side, (x, .035, .48), (.25, .26, .52), DARK, .028), shin)
    part(add_box("ShinArmor_" + side, (x, -.115, .50), (.29, .065, .42), IVORY, .016), shin)
    part(add_joint("Knee_" + side, (x, -.02, .79), .13), shin)
    part(add_box("Thigh_" + side, (x, .035, 1.01), (.30, .30, .39), DARK, .028), thigh)
    part(add_box("ThighArmor_" + side, (x, -.125, 1.04), (.33, .065, .30), IVORY, .016), thigh)

part(add_box("Pelvis", (0, .03, 1.30), (.76, .44, .22), DARK, .040), "HV_Pelvis")
part(add_box("PelvisPlate", (0, -.21, 1.33), (.52, .050, .15), IVORY, .015), "HV_Pelvis")
# A tall layered chest and raised sensor crown distinguish the Heavy in a crowd.
part(add_box("ChestCore", (0, .04, 1.82), (1.08, .58, .84), DARK, .055), "HV_Torso")
part(add_box("ChestArmor", (0, -.275, 1.85), (.98, .085, .66), IVORY, .025), "HV_Torso")
part(add_box("ChestUpperArmor", (0, -.25, 2.23), (.88, .10, .18), IVORY, .020), "HV_Torso")
part(add_box("BackPowerBlock", (0, .34, 1.84), (.66, .18, .64), DARK, .035), "HV_Torso")
part(add_box("Collar", (0, -.05, 2.28), (.64, .38, .13), IVORY, .025), "HV_Torso")
part(add_box("SensorHousing", (0, -.335, 1.98), (.74, .10, .20), DARK, .018), "HV_Torso")
part(add_box("WideRedSensor", (0, -.395, 1.98), (.58, .026, .075), RED, .008), "HV_Torso")
part(add_cylinder("TopVent", (0, .09, 2.42), .075, .18, DARK, 8), "HV_Torso")
part(add_box("TopArmor", (0, .03, 2.53), (.32, .25, .16), IVORY, .020), "HV_Torso")

for x, side in ((-.65, "L"), (.65, "R")):
    arm = "HV_Arm_" + side
    part(add_box("ShoulderBlock_" + side, (x, .01, 2.12), (.32, .44, .27), IVORY, .035), arm)
    part(add_joint("ShoulderJoint_" + side, (x, .05, 2.00), .15), arm)
    part(add_box("UpperArm_" + side, (x, .015, 1.72), (.24, .28, .43), DARK, .025), arm)
    part(add_box("UpperArmArmor_" + side, (x, -.145, 1.75), (.27, .060, .32), IVORY, .014), arm)
    part(add_joint("Elbow_" + side, (x, 0, 1.45), .11), arm)
    part(add_box("Forearm_" + side, (x, .015, 1.20), (.23, .28, .40), DARK, .025), arm)
    part(add_box("Fist_" + side, (x, -.045, .97), (.28, .24, .17), DARK, .025), arm)
    part(add_box("ArmStripe_" + side, (x, -.145, 1.22), (.13, .025, .05), RED, .006), arm)

# Joining by material keeps the mobile-oriented renderer/material-section count at three.
meshes = []
parts_by_material = {mat: [obj for obj in parts if obj.data.materials[0] == mat]
                     for mat in (IVORY, DARK, RED)}
for mat, name in ((IVORY, "Enemy_Heavy_Ivory"), (DARK, "Enemy_Heavy_Dark"), (RED, "Enemy_Heavy_Red")):
    selected = parts_by_material[mat]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in selected:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = selected[0]
    bpy.ops.object.join()
    mesh = selected[0]
    mesh.name, mesh.parent = name, root
    modifier = mesh.modifiers.new("Enemy_Heavy_Rig", 'ARMATURE')
    modifier.object = armature
    meshes.append(mesh)

walk = create_walk_action(armature)
root["asset_contract"] = "Visual-only skinned robot; no colliders, rigidbodies, scripts, or gameplay motion. Walk animates local bones only; HV_Root has no animation. Front is -Y in Blender / -Z in Unity."
root["unity_height_units"] = 2.61
root["rig_type"] = "Generic rigid mechanical rig"
root["animation"] = "Walk, 24 fps, frames 1-25 looped, visual-only"
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
    # Do not bake unanimated HV_Root channels into the FBX. Static root curves
    # would be misleading in Unity even though they have no displacement.
    bake_anim_use_all_bones=False, bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True, bake_anim_force_startend_keying=False,
    bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS', path_mode='AUTO')
print("EXPORTED", BLEND_PATH)
print("EXPORTED", FBX_PATH)
print("ROOT", root.name, "meshes", len(meshes), "bones", len(armature.data.bones), "action", walk.name)
