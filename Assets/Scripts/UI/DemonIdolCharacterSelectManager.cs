using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.UI
{
    /// <summary>
    /// Master Character Selection Screen UI Controller for Assets/Scenes/CharacterSelect.unity.
    /// Manages:
    /// - 25-Fighter Roster Grid (C01, C02, C04-C14, SK02, SK03, SK05, SK06, SK08, EX01-EX07)
    /// - Big 3D character preview pedestal playing idle animation
    /// - P1 cursor (WASD / J confirm / K back / gamepad 1 / touch)
    /// - P2 cursor in Versus (arrows / numpad / gamepad 2) or CPU pick in Arcade
    /// - Fixed stage ST07 (College Raimon)
    /// - Session initialization via LaunchServices.Session.BeginMatch
    /// - Brief VS Screen overlay before transitioning into BattleArena
    /// </summary>
    public class DemonIdolCharacterSelectManager : MonoBehaviour
    {
        public static DemonIdolCharacterSelectManager Instance { get; private set; }

        public static readonly string[] RosterIds = new string[]
        {
            "C01", "C02", "C04", "C05", "C06",
            "C07", "C08", "C09", "C10", "C11",
            "C12", "C13", "C14", "SK02", "SK03",
            "SK05", "SK06", "SK08", "EX01", "EX02",
            "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        [Header("Selection State")]
        public int p1Index = 0; // Default C01
        public int p2Index = 18; // Default EX01 (Hugo)
        public bool p1Locked = false;
        public bool p2Locked = false;
        public bool isVersus = false;

        [Header("UI Grid & Cards")]
        public RectTransform gridRoot;
        public Button[] characterButtons;
        public Image[] p1Borders;
        public Image[] p2Borders;
        public Text[] characterLabels;

        [Header("Dossier UI")]
        public Text p1StatusText;
        public Text p2StatusText;
        public Text dossierName;
        public Text dossierArchetype;
        public Text dossierBio;

        [Header("3D Preview Pedestal")]
        public Transform previewPedestal;
        public Camera previewCamera;
        private GameObject _activePreviewModel;
        private string _activePreviewId = "";

        [Header("VS Overlay")]
        public GameObject vsOverlay;
        public Image vsP1Portrait;
        public Image vsP2Portrait;
        public Text vsP1Name;
        public Text vsP2Name;

        private float _lastInputTime = 0f;
        private const float InputRepeatDelay = 0.16f;

        private void Awake()
        {
            Instance = this;
            string mode = PlayerPrefs.GetString("MDI_GameMode", "Arcade");
            isVersus = string.Equals(mode, "LocalVersus", StringComparison.OrdinalIgnoreCase);
        }

        private void Start()
        {
            // Restore previous picks if valid
            string savedP1 = PlayerPrefs.GetString("MDI_P1_CharacterId", "C01");
            string savedP2 = PlayerPrefs.GetString("MDI_P2_CharacterId", "EX01");
            int idx1 = Array.IndexOf(RosterIds, savedP1);
            int idx2 = Array.IndexOf(RosterIds, savedP2);
            if (idx1 >= 0) p1Index = idx1;
            if (idx2 >= 0) p2Index = idx2;

            if (vsOverlay != null) vsOverlay.SetActive(false);
            UpdateHighlights();
            UpdatePreviewModel(RosterIds[p1Index]);
            UpdateDossier(RosterIds[p1Index]);
        }

        private void Update()
        {
            if (p1Locked && (p2Locked || !isVersus)) return;

            float now = Time.unscaledTime;
            bool canNavigate = now - _lastInputTime >= InputRepeatDelay;

            // P1 Navigation (WASD / Dpad)
            if (!p1Locked && canNavigate)
            {
                int newP1 = p1Index;
                if (UnityEngine.Input.GetKeyDown(KeyCode.W) || UnityEngine.Input.GetAxisRaw("Vertical") > 0.5f)
                {
                    if (newP1 >= 5) newP1 -= 5;
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.S) || UnityEngine.Input.GetAxisRaw("Vertical") < -0.5f)
                {
                    if (newP1 + 5 < RosterIds.Length) newP1 += 5;
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.A) || UnityEngine.Input.GetAxisRaw("Horizontal") < -0.5f)
                {
                    if (newP1 % 5 > 0) newP1 -= 1;
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.D) || UnityEngine.Input.GetAxisRaw("Horizontal") > 0.5f)
                {
                    if (newP1 % 5 < 4 && newP1 + 1 < RosterIds.Length) newP1 += 1;
                }

                if (newP1 != p1Index)
                {
                    p1Index = newP1;
                    _lastInputTime = now;
                    UpdateHighlights();
                    UpdatePreviewModel(RosterIds[p1Index]);
                    UpdateDossier(RosterIds[p1Index]);
                }
            }

            // P1 Confirm (J / Space / Enter / KeypadEnter / South button)
            if (!p1Locked && (UnityEngine.Input.GetKeyDown(KeyCode.J) || UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0)))
            {
                p1Locked = true;
                UpdateHighlights();
                if (!isVersus)
                {
                    StartCoroutine(LaunchMatchSequence());
                    return;
                }
            }

            // P1 Back (K / Escape / Backspace / East button)
            if (UnityEngine.Input.GetKeyDown(KeyCode.K) || UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.Backspace) || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                if (p1Locked)
                {
                    p1Locked = false;
                    UpdateHighlights();
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                    return;
                }
            }

            // P2 Navigation in Versus (Arrow keys / Numpad)
            if (isVersus && !p2Locked && canNavigate)
            {
                int newP2 = p2Index;
                if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (newP2 >= 5) newP2 -= 5;
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (newP2 + 5 < RosterIds.Length) newP2 += 5;
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    if (newP2 % 5 > 0) newP2 -= 1;
                }
                else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
                {
                    if (newP2 % 5 < 4 && newP2 + 1 < RosterIds.Length) newP2 += 1;
                }

                if (newP2 != p2Index)
                {
                    p2Index = newP2;
                    _lastInputTime = now;
                    UpdateHighlights();
                }
            }

            // P2 Confirm in Versus (Num0 / Keypad 0 / Keypad Enter)
            if (isVersus && !p2Locked && (UnityEngine.Input.GetKeyDown(KeyCode.Keypad0) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                p2Locked = true;
                UpdateHighlights();
                if (p1Locked)
                {
                    StartCoroutine(LaunchMatchSequence());
                    return;
                }
            }
        }

        public void SelectSlot(int index, bool isP1)
        {
            if (index < 0 || index >= RosterIds.Length) return;
            if (isP1)
            {
                p1Index = index;
                p1Locked = true;
                UpdateHighlights();
                UpdatePreviewModel(RosterIds[p1Index]);
                UpdateDossier(RosterIds[p1Index]);
                if (!isVersus) StartCoroutine(LaunchMatchSequence());
            }
            else
            {
                p2Index = index;
                p2Locked = true;
                UpdateHighlights();
                if (p1Locked) StartCoroutine(LaunchMatchSequence());
            }
        }

        public void UpdateHighlights()
        {
            for (int i = 0; i < RosterIds.Length; i++)
            {
                if (p1Borders != null && i < p1Borders.Length && p1Borders[i] != null)
                {
                    p1Borders[i].gameObject.SetActive(i == p1Index);
                    p1Borders[i].color = p1Locked ? new Color(0.1f, 0.9f, 0.4f, 1f) : new Color(0.2f, 0.8f, 1f, 1f);
                }

                if (p2Borders != null && i < p2Borders.Length && p2Borders[i] != null)
                {
                    p2Borders[i].gameObject.SetActive(i == p2Index);
                    p2Borders[i].color = p2Locked ? new Color(0.1f, 0.9f, 0.4f, 1f) : new Color(1f, 0.25f, 0.25f, 1f);
                }
            }

            string p1Id = RosterIds[p1Index];
            string p2Id = RosterIds[p2Index];
            var def1 = CharacterRegistry.GetCharacterById(p1Id);
            var def2 = CharacterRegistry.GetCharacterById(p2Id);

            if (p1StatusText != null)
            {
                p1StatusText.text = $"P1: {(def1 != null ? def1.displayName : p1Id)} {(p1Locked ? "[LOCKED]" : "[CHOOSING]")}";
            }

            if (p2StatusText != null)
            {
                string tag = isVersus ? (p2Locked ? "[LOCKED]" : "[CHOOSING]") : "[CPU]";
                p2StatusText.text = $"P2: {(def2 != null ? def2.displayName : p2Id)} {tag}";
            }
        }

        public void UpdateDossier(string charId)
        {
            var def = CharacterRegistry.GetCharacterById(charId);
            if (def == null) return;

            if (dossierName != null) dossierName.text = $"{def.displayName.ToUpper()} ({charId})";
            if (dossierArchetype != null) dossierArchetype.text = $"ROLE: {def.archetype}";
            if (dossierBio != null) dossierBio.text = string.IsNullOrEmpty(def.description) ? "Elite martial combatant tuned for planar arena battles." : def.description;
        }

        public void UpdatePreviewModel(string charId)
        {
            if (_activePreviewId == charId && _activePreviewModel != null) return;
            _activePreviewId = charId;

            if (_activePreviewModel != null)
            {
                Destroy(_activePreviewModel);
                _activePreviewModel = null;
            }

            var def = CharacterRegistry.GetCharacterById(charId);
            if (def == null || def.modelPrefab == null) return;

            Transform parent = previewPedestal != null ? previewPedestal : transform;
            _activePreviewModel = Instantiate(def.modelPrefab, parent.position, parent.rotation, parent);
            _activePreviewModel.name = $"Preview_{charId}";

            // Ensure preview model plays idle animation
            Animator anim = _activePreviewModel.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
                if (def.animatorController != null)
                {
                    anim.runtimeAnimatorController = def.animatorController;
                }
            }
        }

        private IEnumerator LaunchMatchSequence()
        {
            string p1Id = RosterIds[p1Index];
            string p2Id = RosterIds[p2Index];

            // Save player preferences and session
            PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
            PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.Save();

            LaunchServices.Session.BeginMatch(p1Id, p2Id, "ST07");

            // Show VS Screen
            if (vsOverlay != null)
            {
                vsOverlay.SetActive(true);
                var def1 = CharacterRegistry.GetCharacterById(p1Id);
                var def2 = CharacterRegistry.GetCharacterById(p2Id);
                if (vsP1Name != null) vsP1Name.text = def1 != null ? def1.displayName.ToUpper() : p1Id;
                if (vsP2Name != null) vsP2Name.text = def2 != null ? def2.displayName.ToUpper() : p2Id;

                Sprite mug1 = Resources.Load<Sprite>($"UI/Portraits/Mugshots/Mugshot_{p1Id}_{(def1 != null ? def1.displayName : "")}");
                Sprite mug2 = Resources.Load<Sprite>($"UI/Portraits/Mugshots/Mugshot_{p2Id}_{(def2 != null ? def2.displayName : "")}");
                if (vsP1Portrait != null && mug1 != null) vsP1Portrait.sprite = mug1;
                if (vsP2Portrait != null && mug2 != null) vsP2Portrait.sprite = mug2;

                yield return new WaitForSecondsRealtime(0.5f);
            }
            else
            {
                yield return null;
            }

            SceneManager.LoadScene("BattleArena");
        }
    }
}
