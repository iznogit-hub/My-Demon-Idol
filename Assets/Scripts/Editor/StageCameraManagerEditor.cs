#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DemonIdol.CameraControl;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    [CustomEditor(typeof(StageCameraManager))]
    public class StageCameraManagerEditor : UnityEditor.Editor
    {
        private static readonly string[] s_stageKeys = new[]
        {
            "ST01", "ST02", "ST03", "ST04", "ST05", "ST06", "ST07"
        };

        private static readonly string[] s_stageLabels = new[]
        {
            "ST01: Central Courtyard (Clocktower Plaza)",
            "ST02: Sakura Main Gate (Academy Avenue)",
            "ST03: Rooftop Observatory (Skyline Deck)",
            "ST04: Martial Arts Dojo (Traditional Pavilion)",
            "ST05: Athletic Stadium (Sports Track)",
            "ST06: Transit Promenade (Campus Edge)",
            "ST07: College Raimon (Midnight Courtyard)"
        };

        public override void OnInspectorGUI()
        {
            StageCameraManager manager = (StageCameraManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("=== CAMPUS STAGE CAMERA SWITCHER ===", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click any stage below to immediately activate that stage's camera framing, physical lens parameters, and campus zone alignment in the Scene/Game view.", MessageType.Info);

            EditorGUILayout.Space(4);

            for (int i = 0; i < s_stageKeys.Length; i++)
            {
                string stageId = s_stageKeys[i];
                string label = s_stageLabels[i];
                bool isActive = string.Equals(manager.ActiveStageId, stageId, System.StringComparison.OrdinalIgnoreCase);

                GUI.backgroundColor = isActive ? new Color(0.2f, 0.9f, 0.4f, 1f) : Color.white;
                if (GUILayout.Button(isActive ? $">>> ACTIVE: {label} <<<" : label, GUILayout.Height(30)))
                {
                    Undo.RecordObject(manager, $"Switch to {stageId} Camera");
                    manager.SwitchToStageCamera(stageId);
                    EditorUtility.SetDirty(manager);
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(12);

            if (GUILayout.Button("Instantiate / Refresh All 7 Stage Cameras in Scene", GUILayout.Height(32)))
            {
                // DemonIdolArcadeArenaBuilder.EnsureAll7StageCamerasInScene();
            }

            EditorGUILayout.Space(8);
            DrawDefaultInspector();
        }
    }
}
#endif
