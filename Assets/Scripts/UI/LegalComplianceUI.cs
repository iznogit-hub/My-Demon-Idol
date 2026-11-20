using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Firebase;

namespace DemonIdol.UI
{
    public enum LegalTab
    {
        TermsOfService,
        PrivacyPolicy,
        DataDeletion
    }

    /// <summary>
    /// Master Legal Compliance, Privacy Policy & Account Deletion Controller for Demon Idol.
    /// Strictly complies with:
    /// - Apple App Store Guideline 5.1.1(v) (Mandatory In-App Account & Data Deletion).
    /// - Apple App Store Guideline 5.1.1 (Privacy Policy disclosure of telemetry & multiplayer data).
    /// - Google Play Data Safety Section requirements (Account deletion URL + in-app purge).
    /// - GDPR / CCPA right-to-be-forgotten data erasure.
    /// Features dynamic fallback generation for 100% scene independence.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LegalComplianceUI : MonoBehaviour
    {
        public static LegalComplianceUI Instance { get; private set; }

        public const string TERMS_OF_SERVICE_URL = "https://mydemonidol.com/terms";
        public const string PRIVACY_POLICY_URL = "https://mydemonidol.com/privacy";
        public const string DATA_DELETION_URL = "https://mydemonidol.com/data-deletion";

        public const string TERMS_TEXT =
            "DEMON IDOL — TERMS OF SERVICE\nLast Updated: November 2026\n\n" +
            "1. ACCEPTANCE OF TERMS\n" +
            "By downloading, accessing, or playing Demon Idol ('Game'), you agree to be bound by these Terms of Service. If you do not agree, do not play the Game.\n\n" +
            "2. FAIR PLAY & MULTIPLAYER CONDUCT\n" +
            "Demon Idol utilizes Photon Quantum deterministic rollback simulation. Players agree not to inject memory hacks, modify tick rate buffers, automate inputs, or utilize unauthorized third-party network proxies. Violations result in immediate ELO forfeiture and account termination.\n\n" +
            "3. VIRTUAL CURRENCY & PROGRESSION\n" +
            "Blood Coins and Soul Shards are virtual gameplay progression items earned through combat and story achievements. Virtual currency has no real-world monetary value and cannot be transferred, refunded, or redeemed outside the Game.\n\n" +
            "4. INTELLECTUAL PROPERTY\n" +
            "All characters, combat animations, Sumi-e visual tokens, shaders, and audio synthesized assets are the exclusive property of Iznogit Studios and its licensors.\n\n" +
            "5. DISCLAIMER & LIMITATION OF LIABILITY\n" +
            "The Game is provided 'as is' without warranty. Iznogit Studios is not liable for indirect or consequential damages arising from online service interruptions.";

        public const string PRIVACY_TEXT =
            "DEMON IDOL — PRIVACY POLICY & DATA SAFETY\nLast Updated: November 2026\n\n" +
            "1. INFORMATION WE COLLECT\n" +
            "Demon Idol is designed with privacy-first data minimization:\n" +
            "• Account Credentials: Anonymous Guest UID or player-provided Email and Codename via Firebase Authentication.\n" +
            "• Gameplay Data: Win/Loss record, ELO rating, unlocked character roster, and story checkpoint index via Cloud Firestore.\n" +
            "• Network Diagnostics: Round-trip ping latency and packet statistics for Photon Quantum rollback synchronization.\n" +
            "• Device Tokens: Firebase Cloud Messaging token for opt-in tournament and daily reward alerts.\n\n" +
            "2. INFORMATION WE NEVER COLLECT\n" +
            "• We DO NOT collect GPS or precise real-time location.\n" +
            "• We DO NOT collect contacts, photos, or personal device files.\n" +
            "• We DO NOT sell, rent, or trade player data to third-party ad brokers.\n\n" +
            "3. THIRD-PARTY PARTNERS & ASSET LICENSORS\n" +
            "• Photon Quantum (Exit Games): Multiplayer rollback relay synchronization.\n" +
            "• Firebase (Google Cloud): Authentication, cloud persistence, and messaging.\n" +
            "• BuildVerse: College Raimon 3D environment assets under Unity Asset Store EULA.\n\n" +
            "4. YOUR PRIVACY RIGHTS (GDPR / CCPA)\n" +
            "You have the absolute right to view, export, or delete your account data at any time via the in-game 'Data Deletion' tab or at mydemonidol.com/data-deletion.";

        public const string DATA_DELETION_TEXT =
            "ACCOUNT & DATA DELETION REQUEST\nApple App Store Guideline 5.1.1(v) & Google Play Compliance\n\n" +
            "You have the right to request the permanent deletion of your Demon Idol account and all associated personal data stored on our servers.\n\n" +
            "WHAT WILL BE PERMANENTLY ERASED:\n" +
            "• Your Firebase Authentication account and login tokens.\n" +
            "• Your Cloud Firestore player profile, ELO rating, and rank tier.\n" +
            "• Your complete online match history and Win/Loss records.\n" +
            "• All Story Mode checkpoint progression and unlocked roster rewards.\n" +
            "• All local cached settings and device identification keys.\n\n" +
            "WARNING: This action is permanent and cannot be undone.";

        [Header("UI Panels & Tabs")]
        [SerializeField] private CanvasGroup _modalCanvasGroup;
        [SerializeField] private Text _tabTitleText;
        [SerializeField] private Text _contentBodyText;
        [SerializeField] private Button _btnTabTerms;
        [SerializeField] private Button _btnTabPrivacy;
        [SerializeField] private Button _btnTabDeletion;
        [SerializeField] private Button _btnOpenExternalUrl;
        [SerializeField] private Text _btnExternalUrlLabel;
        [SerializeField] private Button _btnExecuteDeleteAccount;
        [SerializeField] private Button _btnClose;

        [Header("Confirmation Modal")]
        [SerializeField] private GameObject _confirmDeleteModal;
        [SerializeField] private Button _btnConfirmDeleteFinal;
        [SerializeField] private Button _btnCancelDelete;

        private LegalTab _activeTab = LegalTab.TermsOfService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
            gameObject.SetActive(false);
        }

        public void Open(LegalTab initialTab = LegalTab.TermsOfService)
        {
            EnsureUIHierarchy();
            gameObject.SetActive(true);
            if (_modalCanvasGroup != null) _modalCanvasGroup.alpha = 1f;

            SwitchTab(initialTab);
        }

        public void Close()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_confirmDeleteModal != null) _confirmDeleteModal.SetActive(false);
            gameObject.SetActive(false);
        }

        public void SwitchTab(LegalTab tab)
        {
            _activeTab = tab;
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();

            if (_btnExecuteDeleteAccount != null)
            {
                _btnExecuteDeleteAccount.gameObject.SetActive(tab == LegalTab.DataDeletion);
            }

            switch (tab)
            {
                case LegalTab.TermsOfService:
                    if (_tabTitleText != null) _tabTitleText.text = "TERMS OF SERVICE";
                    if (_contentBodyText != null) _contentBodyText.text = TERMS_TEXT;
                    if (_btnExternalUrlLabel != null) _btnExternalUrlLabel.text = "VIEW TERMS ONLINE";
                    break;

                case LegalTab.PrivacyPolicy:
                    if (_tabTitleText != null) _tabTitleText.text = "PRIVACY POLICY & DATA SAFETY";
                    if (_contentBodyText != null) _contentBodyText.text = PRIVACY_TEXT;
                    if (_btnExternalUrlLabel != null) _btnExternalUrlLabel.text = "VIEW PRIVACY ONLINE";
                    break;

                case LegalTab.DataDeletion:
                    if (_tabTitleText != null) _tabTitleText.text = "ACCOUNT & DATA DELETION";
                    if (_contentBodyText != null) _contentBodyText.text = DATA_DELETION_TEXT;
                    if (_btnExternalUrlLabel != null) _btnExternalUrlLabel.text = "ONLINE DELETION REQUEST";
                    break;
            }
        }

        public void OnClickOpenExternalUrl()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            string url = _activeTab switch
            {
                LegalTab.TermsOfService => TERMS_OF_SERVICE_URL,
                LegalTab.PrivacyPolicy => PRIVACY_POLICY_URL,
                LegalTab.DataDeletion => DATA_DELETION_URL,
                _ => TERMS_OF_SERVICE_URL
            };
            Application.OpenURL(url);
        }

        public void OnClickPromptDeleteAccount()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_confirmDeleteModal != null)
            {
                _confirmDeleteModal.SetActive(true);
            }
        }

        public void OnClickCancelDelete()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();
            if (_confirmDeleteModal != null)
            {
                _confirmDeleteModal.SetActive(false);
            }
        }

        public async void OnClickConfirmDeleteAccountFinal()
        {
            DemonIdol.Audio.CombatAudioDirector.Instance?.PlayButtonClick();

            string userId = FirebaseAuthService.Instance?.CurrentUser?.UserId;

            // 1. Purge Firestore documents
            if (FirestoreService.Instance != null && !string.IsNullOrEmpty(userId))
            {
                await FirestoreService.Instance.PurgeAllUserDataAsync(userId);
            }

            // 2. Delete Auth record & clear local storage
            if (FirebaseAuthService.Instance != null)
            {
                await FirebaseAuthService.Instance.DeleteAccountAsync();
            }

            if (_confirmDeleteModal != null) _confirmDeleteModal.SetActive(false);
            Close();

            // Notify player and return to clean intro state
            if (SettingsPanel.Instance != null)
            {
                SettingsPanel.Instance.ShowToast("ACCOUNT PERMANENTLY DELETED.");
            }

            if (BloodTechUIManager.Instance != null)
            {
                BloodTechUIManager.Instance.TransitionToModeSelect();
            }
        }

        public void EnsureUIHierarchy()
        {
            if (_modalCanvasGroup != null) return;

            BloodlinesUIAtlas.EnsureInstance();
            var atlas = BloodlinesUIAtlas.Instance;

            _modalCanvasGroup = GetComponent<CanvasGroup>();
            if (_modalCanvasGroup == null) _modalCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            Font font = atlas != null ? atlas.BloodlinesFont : null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            RectTransform rt = UiHierarchy.RequireRectTransform(gameObject);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Background
            Image bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0.04f, 0.04f, 0.06f, 0.96f);
            }

            // Main Window Panel — Bloodlines frame
            GameObject winObj = new GameObject("Window_Legal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            winObj.transform.SetParent(transform, false);
            RectTransform wrt = winObj.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0.5f, 0.5f);
            wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(760f, 540f);
            Image winImg = winObj.GetComponent<Image>();
            if (atlas != null) atlas.StylePanel(winImg, false);
            else winImg.color = new Color(0.10f, 0.10f, 0.13f, 1f);

            // Header Title
            _tabTitleText = CreateText(winObj.transform, "Text_Title", "TERMS OF SERVICE", new Vector2(0f, 235f), new Vector2(500f, 40f), 20, SumiStyleTokens.BrushGold, font, FontStyle.Bold);

            // Tab Buttons Container
            GameObject tabsObj = new GameObject("Tabs_Container", typeof(RectTransform));
            tabsObj.transform.SetParent(winObj.transform, false);
            RectTransform trt = tabsObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 190f);
            trt.sizeDelta = new Vector2(700f, 36f);

            _btnTabTerms = CreateTabButton(tabsObj.transform, "Btn_TabTerms", "TERMS", new Vector2(-220f, 0f), () => SwitchTab(LegalTab.TermsOfService), font);
            _btnTabPrivacy = CreateTabButton(tabsObj.transform, "Btn_TabPrivacy", "PRIVACY POLICY", new Vector2(0f, 0f), () => SwitchTab(LegalTab.PrivacyPolicy), font);
            _btnTabDeletion = CreateTabButton(tabsObj.transform, "Btn_TabDeletion", "DATA DELETION", new Vector2(220f, 0f), () => SwitchTab(LegalTab.DataDeletion), font);

            // Content Scroll/Body Text
            GameObject scrollObj = new GameObject("Content_Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            scrollObj.transform.SetParent(winObj.transform, false);
            RectTransform crt = scrollObj.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(0f, 15f);
            crt.sizeDelta = new Vector2(680f, 280f);
            _contentBodyText = scrollObj.GetComponent<Text>();
            _contentBodyText.font = font;
            _contentBodyText.text = TERMS_TEXT;
            _contentBodyText.fontSize = 13;
            _contentBodyText.alignment = TextAnchor.UpperLeft;
            _contentBodyText.color = SumiStyleTokens.SumiOffWhite;

            // External Link Button
            _btnOpenExternalUrl = CreateActionButton(winObj.transform, "Btn_ExternalUrl", "VIEW ONLINE", new Vector2(-120f, -220f), new Vector2(220f, 42f), new Color(0.2f, 0.25f, 0.35f, 1f), OnClickOpenExternalUrl, font);
            _btnExternalUrlLabel = _btnOpenExternalUrl.GetComponentInChildren<Text>();

            // Account Deletion Button (Initially hidden until Deletion tab)
            _btnExecuteDeleteAccount = CreateActionButton(winObj.transform, "Btn_DeleteAccount", "DELETE MY ACCOUNT", new Vector2(130f, -220f), new Vector2(240f, 42f), SumiStyleTokens.ToriiRed, OnClickPromptDeleteAccount, font);
            _btnExecuteDeleteAccount.gameObject.SetActive(false);

            // Close Button (Top Right)
            _btnClose = CreateActionButton(winObj.transform, "Btn_Close", "✕", new Vector2(350f, 235f), new Vector2(40f, 40f), new Color(0.3f, 0.3f, 0.35f, 1f), Close, font);

            // Build Confirmation Modal
            BuildConfirmDeleteModal(winObj.transform, font);
        }

        private void BuildConfirmDeleteModal(Transform parent, Font font)
        {
            _confirmDeleteModal = new GameObject("Modal_ConfirmDelete", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _confirmDeleteModal.transform.SetParent(parent, false);
            RectTransform crt = _confirmDeleteModal.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.sizeDelta = Vector2.zero;
            Image cimg = _confirmDeleteModal.GetComponent<Image>();
            cimg.color = new Color(0.02f, 0.02f, 0.03f, 0.95f);

            CreateText(_confirmDeleteModal.transform, "Text_ConfirmTitle", "PERMANENT ACCOUNT DELETION", new Vector2(0f, 60f), new Vector2(500f, 35f), 18, SumiStyleTokens.ToriiRed, font, FontStyle.Bold);
            CreateText(_confirmDeleteModal.transform, "Text_ConfirmDesc", "Are you sure? This will wipe your ELO, match records, coins, and story saves immediately from our servers.", new Vector2(0f, 0f), new Vector2(480f, 60f), 14, Color.white, font, FontStyle.Normal);

            _btnConfirmDeleteFinal = CreateActionButton(_confirmDeleteModal.transform, "Btn_FinalDelete", "YES, PURGE EVERYTHING", new Vector2(-120f, -80f), new Vector2(220f, 42f), SumiStyleTokens.ToriiRed, OnClickConfirmDeleteAccountFinal, font);
            _btnCancelDelete = CreateActionButton(_confirmDeleteModal.transform, "Btn_CancelDelete", "CANCEL", new Vector2(120f, -80f), new Vector2(160f, 42f), new Color(0.3f, 0.3f, 0.35f, 1f), OnClickCancelDelete, font);

            _confirmDeleteModal.SetActive(false);
        }

        private Button CreateTabButton(Transform parent, string name, string label, Vector2 pos, Action onClick, Font font)
        {
            return CreateActionButton(parent, name, label, pos, new Vector2(200f, 32f), new Color(0.18f, 0.18f, 0.22f, 1f), onClick, font, 13);
        }

        private Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, Action onClick, Font font, int fontSize = 14)
        {
            var atlas = BloodlinesUIAtlas.Instance;
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.GetComponent<Image>();
            Button btn = obj.GetComponent<Button>();
            btn.targetGraphic = img;

            bool isRed = name.Contains("Delete") || col == SumiStyleTokens.ToriiRed;
            if (atlas != null) atlas.StyleButton(btn, 2, isRed);
            else img.color = col;

            if (onClick != null) btn.onClick.AddListener(() => onClick());

            CreateText(obj.transform, "Text", label, Vector2.zero, size, fontSize, Color.white, font, FontStyle.Bold);
            return btn;
        }

        private Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, int fontSize, Color col, Font font, FontStyle style)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(parent, false);
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text txt = txtObj.GetComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = col;
            return txt;
        }
    }
}
