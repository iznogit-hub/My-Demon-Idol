#!/usr/bin/env python3
"""
tools/deploy_college_raimon_scenes.py

Deterministic scene deployment script for College Raimon environment across all 3 scenes:
- Scene 1: Assets/Scenes/MainMenu.unity (Japanese campus backdrop + ST07 Stage Select button & highlights)
- Scene 2: Assets/Scenes/ArcadeArena.unity (ST07_CollegeRaimon.prefab as Backdrop_Diorama)
- Scene 3: Assets/Scenes/RosterShowcase.unity (CollegeRaimon.prefab nested under Pedestal_Environment + MDI_RosterShowcaseController)
"""

import os
import sys
import re
import shutil

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(ROOT_DIR)

MAIN_MENU_PATH = "Assets/Scenes/MainMenu.unity"
ARCADE_ARENA_PATH = "Assets/Scenes/ArcadeArena.unity"
ROSTER_SHOWCASE_PATH = "Assets/Scenes/RosterShowcase.unity"

RAIMON_PREFAB_GUID = "e74149b2c38522a49903fabd76c6afe7"
ST07_PREFAB_GUID = "d707ca11e9e041bb848696298f2a0cb7"

def deploy_main_menu():
    print("--- [1/3] Deploying ST07 to MainMenu.unity ---")
    with open(MAIN_MENU_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # Check CollegeRaimon backdrop
    if "CollegeRaimon" not in content:
        print("  [ERROR] CollegeRaimon_Environment not found in MainMenu.unity!")
        return False
    else:
        print("  [PASS] Verified CollegeRaimon campus backdrop in MainMenu.unity")

    # If Card_Stage_06 is already present, report and skip adding duplicate
    if "Card_Stage_06" in content and "ST07: COLLEGE RAIMON" in content:
        print("  [INFO] Card_Stage_06 already present in MainMenu.unity.")
    else:
        # Re-position Card_Stage_00 to Card_Stage_05 into 4-column layout
        coords = [
            (0.08, 0.26, 0.58, 0.72), # Card 0
            (0.30, 0.48, 0.58, 0.72), # Card 1
            (0.52, 0.70, 0.58, 0.72), # Card 2
            (0.74, 0.92, 0.58, 0.72), # Card 3
            (0.08, 0.26, 0.40, 0.54), # Card 4
            (0.30, 0.48, 0.40, 0.54), # Card 5
        ]
        card_rect_ids = [1628542339, 550547819, 816776613, 2004701879, 931694690, 1103085376]
        for idx, rect_id in enumerate(card_rect_ids):
            c = coords[idx]
            pattern = rf"(--- !u!224 &{rect_id}\nRectTransform:[\s\S]*?m_AnchorMin: \{{x: )[^\n]+(\}}\n\s*m_AnchorMax: \{{x: )[^\n]+(\}})"
            replacement = rf"\g<1>{c[0]}, y: {c[2]}\g<2>{c[1]}, y: {c[3]}\g<3>"
            content, count = re.subn(pattern, replacement, content, count=1)
            if count > 0:
                print(f"  [UPDATED] Repositioned Card_Stage_{idx:02d} to ({c[0]}, {c[2]}) - ({c[1]}, {c[3]})")
            else:
                print(f"  [WARN] Could not find anchor block for RectTransform &{rect_id}")

        # Construct Card_Stage_06 YAML blocks
        # 170700001: GameObject Card_Stage_06
        # 170700002: RectTransform Card_Stage_06
        # 170700003: Image Card_Stage_06
        # 170700004: CanvasRenderer Card_Stage_06
        # 170700005: Button Card_Stage_06
        # 170700011: GameObject Border_Highlight
        # 170700012: RectTransform Border_Highlight
        # 170700013: CanvasRenderer Border_Highlight
        # 170700014: Image Border_Highlight
        # 170700021: GameObject Label
        # 170700022: RectTransform Label
        # 170700023: CanvasRenderer Label
        # 170700024: Text Label
        card_yaml = """--- !u!1 &170700001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 170700002}
  - component: {fileID: 170700004}
  - component: {fileID: 170700003}
  - component: {fileID: 170700005}
  m_Layer: 0
  m_Name: Card_Stage_06
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &170700002
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 170700012}
  - {fileID: 170700022}
  m_Father: {fileID: 263169717}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.52, y: 0.4}
  m_AnchorMax: {x: 0.7, y: 0.54}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &170700003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 0.12, g: 0.15, b: 0.22, a: 0.95}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!222 &170700004
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700001}
  m_CullTransparentMesh: 1
--- !u!114 &170700005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: 170700003}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
--- !u!1 &170700011
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 170700012}
  - component: {fileID: 170700013}
  - component: {fileID: 170700014}
  m_Layer: 0
  m_Name: Border_Highlight
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &170700012
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700011}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 170700002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &170700013
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700011}
  m_CullTransparentMesh: 1
--- !u!114 &170700014
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700011}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0.15}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!1 &170700021
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 170700022}
  - component: {fileID: 170700023}
  - component: {fileID: 170700024}
  m_Layer: 0
  m_Name: Label
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &170700022
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700021}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 170700002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &170700023
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700021}
  m_CullTransparentMesh: 1
--- !u!114 &170700024
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 170700021}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}
    m_FontSize: 16
    m_FontStyle: 1
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 4
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: 'ST07: COLLEGE RAIMON'
"""
        # Append Card_Stage_06 blocks right before the final documents
        content += "\n" + card_yaml

        # Wire into Panel_StageSelect (RectTransform &263169717)
        panel_pattern = r"(--- !u!224 &263169717\nRectTransform:[\s\S]*?m_Children:\n)([\s\S]*?)(  m_Father:)"
        m = re.search(panel_pattern, content)
        if m:
            children_block = m.group(2)
            if "- {fileID: 170700002}" not in children_block:
                # Add after 1103085376 (Card 5)
                new_children = children_block.replace("- {fileID: 1103085376}\n", "- {fileID: 1103085376}\n  - {fileID: 170700002}\n")
                content = content[:m.start(2)] + new_children + content[m.end(2):]
                print("  [WIRED] Added Card_Stage_06 RectTransform to Panel_StageSelect.m_Children")

        # Wire into MainMenuController (MonoBehaviour &99149853)
        # 1. StageButtons: append 170700005
        btn_pattern = r"(StageButtons:\n(?:  - \{fileID: \d+\}\n){6})"
        mb = re.search(btn_pattern, content)
        if mb:
            new_btn_block = mb.group(1) + "  - {fileID: 170700005}\n"
            content = content[:mb.start(1)] + new_btn_block + content[mb.end(1):]
            print("  [WIRED] Added ST07 button to MainMenuController.StageButtons (length 7)")
        else:
            print("  [WARN] Could not find 6 StageButtons in MainMenuController")

        # 2. StageHighlights: append 170700014
        hl_pattern = r"(StageHighlights:\n(?:  - \{fileID: \d+\}\n){6})"
        mh = re.search(hl_pattern, content)
        if mh:
            new_hl_block = mh.group(1) + "  - {fileID: 170700014}\n"
            content = content[:mh.start(1)] + new_hl_block + content[mh.end(1):]
            print("  [WIRED] Added ST07 highlight to MainMenuController.StageHighlights (length 7)")
        else:
            print("  [WARN] Could not find 6 StageHighlights in MainMenuController")

        with open(MAIN_MENU_PATH, "w", encoding="utf-8") as f:
            f.write(content)
        print("  [PASS] MainMenu.unity updated successfully.")

    return True


def deploy_arcade_arena():
    print("\n--- [2/3] Deploying ST07 to ArcadeArena.unity ---")
    with open(ARCADE_ARENA_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # Look for Backdrop_Diorama PrefabInstance 1591863205
    # Replace desert prefab f6962955abbc94fe1a355eff66d12ae2 with ST07_CollegeRaimon d707ca11e9e041bb848696298f2a0cb7
    arena_pattern = r"(--- !u!1001 &1591863205\nPrefabInstance:[\s\S]*?--- !u!4 &1591863206 stripped\nTransform:\n  m_CorrespondingSourceObject: \{fileID: )[^\n]+(\}\n  m_PrefabInstance: \{fileID: 1591863205\}\n  m_PrefabAsset: \{fileID: 0\})"
    
    st07_diorama_block = f"""--- !u!1001 &1591863205
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 685432449}}
    m_Modifications:
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 100000, guid: {ST07_PREFAB_GUID}, type: 3}}
      propertyPath: m_Name
      value: Backdrop_Diorama
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {ST07_PREFAB_GUID}, type: 3}}
--- !u!4 &1591863206 stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 400000, guid: {ST07_PREFAB_GUID}, type: 3}}
  m_PrefabInstance: {{fileID: 1591863205}}
  m_PrefabAsset: {{fileID: 0}}"""

    new_content, count = re.subn(arena_pattern, st07_diorama_block, content, count=1)
    if count > 0:
        with open(ARCADE_ARENA_PATH, "w", encoding="utf-8") as f:
            f.write(new_content)
        print("  [PASS] ArcadeArena.unity Backdrop_Diorama replaced with ST07_CollegeRaimon.prefab")
    else:
        # Check if already pointing to ST07
        if ST07_PREFAB_GUID in content:
            print("  [INFO] ArcadeArena.unity already configured with ST07_CollegeRaimon.")
        else:
            print("  [ERROR] Could not find Backdrop_Diorama PrefabInstance in ArcadeArena.unity!")
            return False

    return True


def deploy_roster_showcase():
    print("\n--- [3/3] Deploying College Raimon to RosterShowcase.unity ---")
    with open(ROSTER_SHOWCASE_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # 1. Add CollegeRaimon_Backdrop under Pedestal_Environment (Transform 2013662035)
    if "CollegeRaimon_Backdrop" in content:
        print("  [INFO] CollegeRaimon_Backdrop already in RosterShowcase.unity.")
    else:
        # Add stripped transform 180700002 to Pedestal_Environment.m_Children
        ped_pattern = r"(--- !u!4 &2013662035\nTransform:[\s\S]*?m_Children:\n)([\s\S]*?)(  m_Father:)"
        m = re.search(ped_pattern, content)
        if m:
            children_block = m.group(2)
            if "- {fileID: 180700002}" not in children_block:
                new_children = children_block + "  - {fileID: 180700002}\n"
                content = content[:m.start(2)] + new_children + content[m.end(2):]
                print("  [WIRED] Added CollegeRaimon_Backdrop to Pedestal_Environment.m_Children")

        raimon_showcase_block = f"""--- !u!1001 &180700001
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 2013662035}}
    m_Modifications:
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: -6
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: -0.6
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 16
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 180
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 7783131211116990044, guid: {RAIMON_PREFAB_GUID}, type: 3}}
      propertyPath: m_Name
      value: CollegeRaimon_Backdrop
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {RAIMON_PREFAB_GUID}, type: 3}}
--- !u!4 &180700002 stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 5269755544991313029, guid: {RAIMON_PREFAB_GUID}, type: 3}}
  m_PrefabInstance: {{fileID: 180700001}}
  m_PrefabAsset: {{fileID: 0}}"""

        content += "\n" + raimon_showcase_block

    # 2. Ensure Showcase_Director with MDI_RosterShowcaseController exists
    if "Showcase_Director" in content:
        print("  [INFO] Showcase_Director already in RosterShowcase.unity.")
    else:
        # 18 FighterDefinition GUIDs for the 6 clans
        fdef_guids = [
            "c7104a221020fbd39eee5d6e95717518", # ASH_MIRA
            "63e4677439dc1f452e879a64fee8f851", # ASH_SHIN
            "7807fe135959051ab3d7fa496d148899", # ASH_VESPER
            "3f0306fb18e314963f4fb4ea10525881", # DEM_DANTE
            "5f2aae721f4e5ae8ccfa104293c6be4b", # DEM_KAEL
            "520a0c7b3a090c4c1db79f8aa0f66fe7", # DEM_NOX
            "e4c00fd5c2efed95f833094565cee5ce", # MED_CHIYO
            "6c2ec6d93ecd69a483eb2ef7204c95a9", # MED_LEON
            "908feba32ba5fb2711d262c14c894ce3", # MED_VICTOR
            "bf5c7ec3795799d009cf7d69ae458184", # NEO_JIN
            "2e12ea055474f84094a477fb8d07dcb0", # NEO_YUKI
            "262bab6110df4829f53d8236d1c97710", # NEO_ZEV
            "bb0914e2eeaa078e5bb58112e530eb08", # OCC_KAI
            "9d5553fb5bcff6d549dbaa61f35fc2b6", # OCC_REIKA
            "fa8ff3ad9d9b5417858db4dab32b77f5", # OCC_REN
            "e03ed299b3c8575b6bbb1c0a43e14f3d", # ORC_ARIN
            "7b2ab302f214748678e25c2ad0e39b51", # ORC_BORAN
            "3bf301c3e559f3b976aa7e4bf929839b", # ORC_KURO
        ]

        fdefs_yaml = "".join([f"  - {{fileID: 11400000, guid: {g}, type: 2}}\n" for g in fdef_guids])

        director_yaml = f"""--- !u!1 &180700011
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 180700012}}
  - component: {{fileID: 180700013}}
  m_Layer: 0
  m_Name: Showcase_Director
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &180700012
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 180700011}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &180700013
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 180700011}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::DemonIdol.Characters.MDI_RosterShowcaseController
  fighterPrefabs: []
  fighterDefinitions:
{fdefs_yaml}  spacingX: 2.4
  spacingZ: 3.6
  showcaseRoot: {{fileID: 0}}
  showcaseCamera: {{fileID: 1431690367}}
  cameraOffset: {{x: 0, y: 1.8, z: -4.5}}
  panSpeed: 6
  titleText: {{fileID: 0}}
  clanText: {{fileID: 0}}
  statsText: {{fileID: 0}}
  metricsText: {{fileID: 0}}"""

        content += "\n" + director_yaml
        print("  [WIRED] Added Showcase_Director with MDI_RosterShowcaseController to RosterShowcase.unity")

    with open(ROSTER_SHOWCASE_PATH, "w", encoding="utf-8") as f:
        f.write(content)
    print("  [PASS] RosterShowcase.unity updated successfully.")

    return True


if __name__ == "__main__":
    print("=============================================================")
    print("  DEPLOYING COLLEGE RAIMON ACROSS ALL 3 SCENES               ")
    print("=============================================================")
    s1 = deploy_main_menu()
    s2 = deploy_arcade_arena()
    s3 = deploy_roster_showcase()

    if s1 and s2 and s3:
        print("\n=============================================================")
        print("  [SUCCESS] All 3 scenes successfully wired for College Raimon!")
        print("=============================================================")
        sys.exit(0)
    else:
        print("\n=============================================================")
        print("  [FAILED] One or more scenes failed to update.")
        print("=============================================================")
        sys.exit(1)
