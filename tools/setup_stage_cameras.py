#!/usr/bin/env python3
"""
tools/setup_stage_cameras.py
Injects the 7 stage cameras into Assets/Scenes/ArcadeArena.unity,
each calibrated to a different area of College Raimon.
"""

import sys
import re

SCENE_PATH = "Assets/Scenes/ArcadeArena.unity"

def main():
    with open(SCENE_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    if "Stage_Cameras" in content:
        print("Stage_Cameras already present in ArcadeArena.unity.")
        return

    # Find highest ID
    ids = [int(x) for x in re.findall(r"--- !u!\d+ &(\d+)", content) if len(x) < 10]
    next_id = max(ids) + 100

    root_go_id = next_id; next_id += 1
    root_tf_id = next_id; next_id += 1
    root_mgr_id = next_id; next_id += 1

    # Stage specifications
    stages = [
        {
            "id": "ST01",
            "name": "Camera_ST01_Courtyard",
            "displayName": "Central Courtyard",
            "pos": (-0.05, 1.93, -6.5),
            "rot": (0.024432179, 0, 0, 0.9997015),
            "focal": 29.405,
            "fov": 38.0,
            "lensShift": (0, 0.035),
            "screenFraction": 0.40,
            "desc": "Crisp daylight framing of the academic clock tower facade."
        },
        {
            "id": "ST02",
            "name": "Camera_ST02_MainGate",
            "displayName": "Sakura Main Gate",
            "pos": (0.0, 1.70, -6.8),
            "rot": (0.01919749, 0, 0, 0.9998157), # ~2.2 deg
            "focal": 27.5,
            "fov": 40.5,
            "lensShift": (0, 0.030),
            "screenFraction": 0.38,
            "desc": "Lower, wider framing capturing classical school gates and sakura cherry blossoms."
        },
        {
            "id": "ST03",
            "name": "Camera_ST03_Rooftop",
            "displayName": "Rooftop Observatory",
            "pos": (0.0, 2.30, -6.2),
            "rot": (0.03664366, 0, 0, 0.9993284), # ~4.2 deg
            "focal": 32.0,
            "fov": 35.2,
            "lensShift": (0, 0.040),
            "screenFraction": 0.42,
            "desc": "Steeper downward pitch emphasizing rooftop drop-off and sprawling city skyline."
        },
        {
            "id": "ST04",
            "name": "Camera_ST04_Dojo",
            "displayName": "Martial Arts Dojo",
            "pos": (0.0, 1.80, -6.2),
            "rot": (0.02181489, 0, 0, 0.999762), # ~2.5 deg
            "focal": 30.5,
            "fov": 36.8,
            "lensShift": (0, 0.035),
            "screenFraction": 0.42,
            "desc": "Intimate cinematic framing of traditional wooden columns and warm paper lanterns."
        },
        {
            "id": "ST05",
            "name": "Camera_ST05_Stadium",
            "displayName": "Athletic Stadium",
            "pos": (0.0, 2.10, -7.2),
            "rot": (0.02617695, 0, 0, 0.9996573), # ~3.0 deg
            "focal": 25.5,
            "fov": 43.2,
            "lensShift": (0, 0.025),
            "screenFraction": 0.36,
            "desc": "Expansive sports broadcast wide-angle showing the running track and bleachers."
        },
        {
            "id": "ST06",
            "name": "Camera_ST06_Promenade",
            "displayName": "Transit Promenade",
            "pos": (0.0, 1.60, -6.6),
            "rot": (0.01745241, 0, 0, 0.9998477), # ~2.0 deg
            "focal": 28.5,
            "fov": 39.0,
            "lensShift": (0, 0.030),
            "screenFraction": 0.39,
            "desc": "Street-level modern perspective framing campus walkways and illuminated kiosks."
        }
    ]

    # Fighter IDs in scene
    # Fighter_P1_Fina: 169567839
    # Fighter_P2_StellarWitch: 167226525
    f1_id = 169567839
    f2_id = 167226525

    child_tf_ids = []
    generated_blocks = []
    stage_camera_entries_yaml = []

    for s in stages:
        c_go = next_id; next_id += 1
        c_tf = next_id; next_id += 1
        c_cam = next_id; next_id += 1
        c_al = next_id; next_id += 1
        c_fcam = next_id; next_id += 1
        c_uacd = next_id; next_id += 1

        child_tf_ids.append(c_tf)

        block = f"""--- !u!1 &{c_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {c_tf}}}
  - component: {{fileID: {c_cam}}}
  - component: {{fileID: {c_fcam}}}
  - component: {{fileID: {c_al}}}
  - component: {{fileID: {c_uacd}}}
  m_Layer: 0
  m_Name: {s['name']}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!4 &{c_tf}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {c_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: {s['rot'][0]}, y: {s['rot'][1]}, z: {s['rot'][2]}, w: {s['rot'][3]}}}
  m_LocalPosition: {{x: {s['pos'][0]}, y: {s['pos'][1]}, z: {s['pos'][2]}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {root_tf_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!20 &{c_cam}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {c_go}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {{r: 0.126157, g: 0.20337336, b: 0.254717, a: 0}}
  m_projectionMatrixMode: 2
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: {s['focal']}
  m_BladeCount: 5
  m_Curvature: {{x: 2, y: 11}}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {{x: 36, y: 20.25}}
  m_LensShift: {{x: {s['lensShift'][0]}, y: {s['lensShift'][1]}}}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.2
  far clip plane: 120
  field of view: {s['fov']}
  orthographic: 0
  orthographic size: 2.1
  m_Depth: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!81 &{c_al}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {c_go}}}
  m_Enabled: 0
--- !u!114 &{c_fcam}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {c_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: f6f2545719eee4045a44911be8bd3ba7, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::DemonIdol.Combat.FighterCamera2D
  fighter1: {{fileID: {f1_id}}}
  fighter2: {{fileID: {f2_id}}}
  usePhysicalCamera: 1
  sensorSize: {{x: 36, y: 20.25}}
  focalLength: {s['focal']}
  lensShift: {{x: {s['lensShift'][0]}, y: {s['lensShift'][1]}}}
  standardFighterHeight: 1.75
  targetCharacterScreenFraction: {s['screenFraction']}
  closeCombatScreenFraction: 0.45
  wideSpacingScreenFraction: 0.35
  zoomFactor: 0.45
  offset: {{x: {s['pos'][0]}, y: {s['pos'][1]}, z: {s['pos'][2]}}}
  pitchAngle: 2.8
  aggressionLeadAmount: 0.35
  verticalLaunchThresholdY: 2.5
  minDistanceZ: -6.5
  maxDistanceZ: -13.5
  smoothSpeed: 8
  smoothSpeedY: 3.5
  smoothSpeedZ: 5.5
  deadzoneX: 0.06
  deadzoneY: 0.1
  deadzoneZ: 0.12
  hitAccentZoomZ: -9.2
  counterHitZoomZ: -7.5
  launchTrackLiftY: 1.4
  wallAccentZoomZ: -7.8
  throwAccentZoomZ: -7.2
  ultimateEntryZoomZ: -5.8
  koCinematicZoomZ: -5.5
  traumaDecay: 6
  maxShakeOffset: 0.035
  maxShakeAngle: 0
  masterShakeScale: 0.25
--- !u!114 &{c_uacd}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {c_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: a79441f348de89743a2939f4d699eac1, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.UniversalAdditionalCameraData
  m_RenderShadows: 1
  m_RequiresDepthTextureOption: 2
  m_RequiresOpaqueTextureOption: 2
  m_CameraType: 0
  m_Cameras: []
  m_RendererIndex: -1
  m_VolumeLayerMask:
    serializedVersion: 2
    m_Bits: 1
  m_VolumeTrigger: {{fileID: 0}}
  m_VolumeFrameworkUpdateModeOption: 2
  m_RenderPostProcessing: 0
  m_Antialiasing: 0
  m_AntialiasingQuality: 2
  m_StopNaN: 0
"""
        generated_blocks.append(block)
        stage_camera_entries_yaml.append(f"""    - stageId: {s['id']}
      stageName: {s['displayName']}
      camera: {{fileID: {c_cam}}}
      fighterCamera: {{fileID: {c_fcam}}}
      basePosition: {{x: {s['pos'][0]}, y: {s['pos'][1]}, z: {s['pos'][2]}}}
      baseEulerAngles: {{x: 2.8, y: 0, z: 0}}
      focalLength: {s['focal']}
      fieldOfView: {s['fov']}
      sensorSize: {{x: 36, y: 20.25}}
      lensShift: {{x: {s['lensShift'][0]}, y: {s['lensShift'][1]}}}
      characterScreenFraction: {s['screenFraction']}
      description: {s['desc']}""")

    # Main Camera is ST07
    main_cam_id = 1840361298
    main_fcam_id = 1840361297
    child_tf_ids.append(1840361299)
    stage_camera_entries_yaml.append(f"""    - stageId: ST07
      stageName: College Raimon (Midnight Rain Courtyard)
      camera: {{fileID: {main_cam_id}}}
      fighterCamera: {{fileID: {main_fcam_id}}}
      basePosition: {{x: -0.05, y: 1.93, z: -6.5}}
      baseEulerAngles: {{x: 2.8, y: 0, z: 0}}
      focalLength: 29.405136
      fieldOfView: 37.999996
      sensorSize: {{x: 36, y: 20.25}}
      lensShift: {{x: 0, y: 0.035}}
      characterScreenFraction: 0.40
      description: User-calibrated baseline camera framing the midnight thunderstorm courtyard.""")

    children_yaml = "\n".join([f"  - {{fileID: {cid}}}" for cid in child_tf_ids])
    entries_yaml = "\n".join(stage_camera_entries_yaml)

    root_block = f"""--- !u!1 &{root_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {root_tf_id}}}
  - component: {{fileID: {root_mgr_id}}}
  m_Layer: 0
  m_Name: Stage_Cameras
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{root_tf_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{children_yaml}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{root_mgr_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: ca3e8a719bee4045a44911be8bd3ba71, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::DemonIdol.CameraControl.StageCameraManager
  _stageCameras:
{entries_yaml}
  _activeStageId: ST07
  _activeCamera: {{fileID: {main_cam_id}}}
"""

    # Update Main Camera father to point to Stage_Cameras root
    old_mc_father = "  m_Father: {fileID: 0}"
    new_mc_father = f"  m_Father: {{fileID: {root_tf_id}}}"

    # Target specific Main Camera transform (1840361299)
    mc_tf_pattern = r"(--- !u!4 &1840361299\nTransform:.*?\n  m_Father: )\{fileID: 0\}"
    if re.search(mc_tf_pattern, content):
        content = re.sub(mc_tf_pattern, r"\g<1>{fileID: " + str(root_tf_id) + "}", content, count=1)
    else:
        print("Note: Main Camera father could not be re-parented via regex, continuing...")

    # Append new root and camera blocks to the scene
    content += "\n" + root_block + "\n" + "\n".join(generated_blocks) + "\n"

    with open(SCENE_PATH, "w", encoding="utf-8") as f:
        f.write(content)

    print(f"Successfully added Stage_Cameras and 6 distinct stage cameras to {SCENE_PATH}.")

if __name__ == "__main__":
    main()
