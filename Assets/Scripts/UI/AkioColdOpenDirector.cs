using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Audio;
using DemonIdol.Cinematics;

namespace DemonIdol.UI
{
    /// <summary>
    /// Phase 3: 0-10s Akio Cold Open & Kinetic Typography Director.
    /// Real-time JJK-style anime cold open:
    /// - 0.0s-3.5s (KineticGreen): Over-the-shoulder close-up of Akio with slamming green words.
    /// - 3.5s-7.0s (KineticBlue): High-angle tracking shot with punchy blue words.
    /// - 7.0s-10.0s (KineticRed Beat Drop): Heavy bass stinger, chromatic flash, camera glides to live menu framing.
    /// - Skip on any key/touch, bypass on return from CharacterSelect or BattleArena.
    /// </summary>
    public class AkioColdOpenDirector : MonoBehaviour
    {
        public static AkioColdOpenDirector Instance { get; private set; }

        public static bool HasPlayedColdOpen = false;

        [Header("Typography UI")]
        public Canvas typographyCanvas;
        public Text txtImpactWord;
        public Text txtSubtitleWord;
        public Image imgFlashOverlay;

        [Header("Menu UI Roots to Reveal")]
        public CanvasGroup menuUIGroup;

        [Header("Lighting References")]
        public Light rimLight;

        [Header("Camera Rig")]
        public UnityEngine.Camera menuCamera;
        public MenuFighterCameraRig cameraRig;

        public bool IsColdOpenPlaying { get; private set; } = false;

        private Coroutine _coldOpenRoutine;

        // Camera Shot Coordinates
        private readonly Vector3 Shot1CamPos = new Vector3(1.20f, 1.35f, -2.4f);
        private readonly Quaternion Shot1CamRot = Quaternion.Euler(4f, -18f, 0f);

        private readonly Vector3 Shot2CamPos = new Vector3(1.40f, 1.70f, -2.8f);
        private readonly Quaternion Shot2CamRot = Quaternion.Euler(14f, -12f, 0f);

        private readonly Vector3 LiveMenuCamPos = new Vector3(0.0f, 1.25f, -4.5f);
        private readonly Quaternion LiveMenuCamRot = Quaternion.Euler(4f, -4f, 0f);

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (HasPlayedColdOpen)
            {
                // Instant bypass when returning from other scenes
                BypassColdOpenInstantly();
            }
            else
            {
                StartColdOpen();
            }
        }

        private void Update()
        {
            if (IsColdOpenPlaying)
            {
                // Any input skips immediately to live menu
                if (UnityEngine.Input.anyKeyDown || (UnityEngine.Input.touchCount > 0 && UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    SkipColdOpen();
                }
            }
        }

        public void StartColdOpen()
        {
            if (_coldOpenRoutine != null) StopCoroutine(_coldOpenRoutine);
            _coldOpenRoutine = StartCoroutine(RunColdOpenChoreography());
        }

        public void SkipColdOpen()
        {
            if (!IsColdOpenPlaying) return;
            if (_coldOpenRoutine != null)
            {
                StopCoroutine(_coldOpenRoutine);
                _coldOpenRoutine = null;
            }
            BypassColdOpenInstantly();
        }

        public void BypassColdOpenInstantly()
        {
            IsColdOpenPlaying = false;
            HasPlayedColdOpen = true;

            if (typographyCanvas != null) typographyCanvas.gameObject.SetActive(false);
            if (imgFlashOverlay != null) imgFlashOverlay.gameObject.SetActive(false);

            if (menuUIGroup != null)
            {
                menuUIGroup.alpha = 1f;
                menuUIGroup.interactable = true;
                menuUIGroup.blocksRaycasts = true;
            }

            if (menuCamera != null)
            {
                menuCamera.transform.position = LiveMenuCamPos;
                menuCamera.transform.rotation = LiveMenuCamRot;
                menuCamera.fieldOfView = 30f;
            }

            if (rimLight != null)
            {
                rimLight.color = new Color(0.4f, 0.7f, 1f);
                rimLight.intensity = 0.85f;
            }
        }

        private IEnumerator RunColdOpenChoreography()
        {
            IsColdOpenPlaying = true;
            HasPlayedColdOpen = true;

            // Hide main menu UI during cold open
            if (menuUIGroup != null)
            {
                menuUIGroup.alpha = 0f;
                menuUIGroup.interactable = false;
                menuUIGroup.blocksRaycasts = false;
            }

            if (typographyCanvas != null) typographyCanvas.gameObject.SetActive(true);
            if (imgFlashOverlay != null) imgFlashOverlay.gameObject.SetActive(true);

            // Audio: Start Haunting Flute cue
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayDomainActivation();
            }

            // =========================================================================
            // BEAT 1 (0.0s - 3.5s): KineticGreen Over-the-shoulder
            // "AT RAIMON COLLEGE" -> "INTELLIGENCE" -> "SUMMONS CURSES" -> "AKIO"
            // =========================================================================
            if (rimLight != null)
            {
                rimLight.color = SumiStyleTokens.KineticGreen;
                rimLight.intensity = 1.4f;
            }

            if (menuCamera != null)
            {
                menuCamera.transform.position = Shot1CamPos;
                menuCamera.transform.rotation = Shot1CamRot;
                menuCamera.fieldOfView = 28f;
            }

            // Word 1: "AT RAIMON COLLEGE" (0.0s - 0.9s)
            SetImpactText("AT RAIMON COLLEGE", "SECRETS OF THE UNDERWORLD", SumiStyleTokens.KineticGreen);
            yield return WaitRespectingSkip(0.9f);

            // Word 2: "INTELLIGENCE" (0.9s - 1.8s)
            SetImpactText("INTELLIGENCE", "SHADOW CURSE TECH", SumiStyleTokens.KineticGreen);
            yield return WaitRespectingSkip(0.9f);

            // Word 3: "SUMMONS CURSES" (1.8s - 2.7s)
            SetImpactText("SUMMONS CURSES", "DOMAIN EXPANSION READY", SumiStyleTokens.KineticGreen);
            yield return WaitRespectingSkip(0.9f);

            // Word 4: "AKIO" (2.7s - 3.5s)
            SetImpactText("AKIO", "PROTAGONIST // GRADE 1 SORCERER", SumiStyleTokens.KineticGreen);
            yield return WaitRespectingSkip(0.8f);

            // =========================================================================
            // BEAT 2 (3.5s - 7.0s): KineticBlue High-Angle Tracking Shot
            // "THE THRONE WAS STOLEN" -> "CURSE WAR BEGINS"
            // =========================================================================
            if (rimLight != null)
            {
                rimLight.color = SumiStyleTokens.KineticBlue;
                rimLight.intensity = 1.6f;
            }

            // Smooth camera transition to high angle
            float t = 0f;
            Vector3 startPos = menuCamera != null ? menuCamera.transform.position : Shot1CamPos;
            Quaternion startRot = menuCamera != null ? menuCamera.transform.rotation : Shot1CamRot;

            SetImpactText("THE THRONE WAS STOLEN", "ANCIENT SEAL BROKEN", SumiStyleTokens.KineticBlue);

            while (t < 1.7f)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / 1.7f);
                if (menuCamera != null)
                {
                    menuCamera.transform.position = Vector3.Lerp(startPos, Shot2CamPos, SumiStyleTokens.EaseOutCubic(progress));
                    menuCamera.transform.rotation = Quaternion.Slerp(startRot, Shot2CamRot, SumiStyleTokens.EaseOutCubic(progress));
                }
                yield return null;
            }

            SetImpactText("CURSE WAR BEGINS", "12 DEMON IDOLS AWAKEN", SumiStyleTokens.KineticBlue);
            yield return WaitRespectingSkip(1.8f);

            // =========================================================================
            // BEAT 3 (7.0s - 10.0s): KineticRed Beat Drop -> Live Menu
            // Bass hit, chromatic pulse, camera glides to live menu framing
            // =========================================================================
            if (rimLight != null)
            {
                rimLight.color = SumiStyleTokens.KineticRed;
                rimLight.intensity = 1.8f;
            }

            // Audio: Heavy Bass drop
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayHitHeavy();
                CombatAudioDirector.Instance.PlayVoiceDomainSuper();
            }

            // Chromatic aberration pulse & 3-frame white/red flash
            if (cameraRig != null)
            {
                cameraRig.PulseChromaticAberration(0.35f, 0.20f);
            }

            if (imgFlashOverlay != null)
            {
                imgFlashOverlay.color = SumiStyleTokens.KineticRed;
                yield return new WaitForSecondsRealtime(0.05f);
                imgFlashOverlay.color = Color.white;
                yield return new WaitForSecondsRealtime(0.05f);
                imgFlashOverlay.color = Color.clear;
            }

            SetImpactText("MY DEMON IDOL", "NEO-SHIBUYA SOUL HARVEST", SumiStyleTokens.KineticRed);

            // Glide camera to live menu framing (7.5s -> 9.5s)
            startPos = menuCamera != null ? menuCamera.transform.position : Shot2CamPos;
            startRot = menuCamera != null ? menuCamera.transform.rotation : Shot2CamRot;
            t = 0f;
            float glideDuration = 2.0f;

            while (t < glideDuration)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / glideDuration);
                float ease = SumiStyleTokens.EaseOutCubic(progress);

                if (menuCamera != null)
                {
                    menuCamera.transform.position = Vector3.Lerp(startPos, LiveMenuCamPos, ease);
                    menuCamera.transform.rotation = Quaternion.Slerp(startRot, LiveMenuCamRot, ease);
                    menuCamera.fieldOfView = Mathf.Lerp(28f, 30f, ease);
                }

                // Fade menu UI in as camera arrives
                if (progress > 0.6f && menuUIGroup != null)
                {
                    float uiEase = (progress - 0.6f) / 0.4f;
                    menuUIGroup.alpha = Mathf.Lerp(0f, 1f, uiEase);
                }

                yield return null;
            }

            // Conclude cold open cleanly into live menu
            BypassColdOpenInstantly();
        }

        private IEnumerator WaitRespectingSkip(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SetImpactText(string impactWord, string subtitle, Color color)
        {
            if (txtImpactWord != null)
            {
                txtImpactWord.text = impactWord;
                txtImpactWord.color = color;
            }
            if (txtSubtitleWord != null)
            {
                txtSubtitleWord.text = subtitle;
                txtSubtitleWord.color = SumiStyleTokens.PaperWhite;
            }
        }
    }
}
