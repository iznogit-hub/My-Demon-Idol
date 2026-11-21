using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;
using DemonIdol.Cutscenes;

namespace DemonIdol.UI
{
    /// <summary>
    /// Interactive Movelist & How-To-Play screen generated deterministically from
    /// BBCharacterProfile plus BBSignatureTable.
    /// Accessible from the in-game pause modal and from character select.
    /// </summary>
    public class BBMovelistModal : MonoBehaviour
    {
        public static BBMovelistModal Instance { get; private set; }

        public static BBMovelistModal Show(string characterId, Transform parent = null)
        {
            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                Instance.Populate(characterId);
                return Instance;
            }

            Transform targetParent = parent != null ? parent : UiHierarchy.ResolveCanvasParent(null);
            if (targetParent == null)
            {
                var canvasGo = new GameObject("MovelistCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = canvasGo.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 600;
                var s = canvasGo.GetComponent<CanvasScaler>();
                s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1920f, 1080f);
                targetParent = canvasGo.transform;
            }

            var modalGo = new GameObject("Modal_BBMovelist", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BBMovelistModal));
            modalGo.transform.SetParent(targetParent, false);
            var modal = modalGo.GetComponent<BBMovelistModal>();
            modal.BuildBaseHierarchy();
            modal.Populate(characterId);
            return modal;
        }

        private GameObject _box;
        private Text _titleText;
        private Text _subTitleText;
        private Text _bodyText;
        private Button _btnClose;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildBaseHierarchy()
        {
            RectTransform rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.88f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Modal Box
            _box = new GameObject("MovelistBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _box.transform.SetParent(transform, false);
            RectTransform boxRt = _box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(1100f, 720f);
            boxRt.anchoredPosition = Vector2.zero;
            Image boxImg = _box.GetComponent<Image>();
            boxImg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

            // Title
            var titleGo = new GameObject("Text_Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleGo.transform.SetParent(_box.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 320f);
            titleRt.sizeDelta = new Vector2(1000f, 40f);
            _titleText = titleGo.GetComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 28;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(1f, 0.85f, 0.35f, 1f);

            // Subtitle
            var subGo = new GameObject("Text_SubTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subGo.transform.SetParent(_box.transform, false);
            RectTransform subRt = subGo.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0f, 285f);
            subRt.sizeDelta = new Vector2(1000f, 30f);
            _subTitleText = subGo.GetComponent<Text>();
            _subTitleText.font = font;
            _subTitleText.fontSize = 18;
            _subTitleText.alignment = TextAnchor.MiddleCenter;
            _subTitleText.color = new Color(0f, 0.90f, 1f, 1f);

            // Scroll View / Content Box for text
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_box.transform, false);
            RectTransform sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, 15f);
            sRt.sizeDelta = new Vector2(1020f, 480f);
            Image sImg = scrollGo.GetComponent<Image>();
            sImg.color = new Color(0.04f, 0.05f, 0.07f, 0.95f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform vpRt = viewportGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(10f, 10f);
            vpRt.offsetMax = new Vector2(-10f, -10f);
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform cRt = contentGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            _bodyText = contentGo.GetComponent<Text>();
            _bodyText.font = font;
            _bodyText.fontSize = 17;
            _bodyText.lineSpacing = 1.25f;
            _bodyText.color = Color.white;
            _bodyText.alignment = TextAnchor.UpperLeft;

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.content = cRt;
            sr.viewport = vpRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            // Close Button
            var btnGo = new GameObject("Btn_CloseMovelist", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_box.transform, false);
            RectTransform bRt = btnGo.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, -300f);
            bRt.sizeDelta = new Vector2(260f, 50f);
            Image bImg = btnGo.GetComponent<Image>();
            bImg.color = new Color(0.85f, 0.18f, 0.22f, 1f);
            _btnClose = btnGo.GetComponent<Button>();
            _btnClose.onClick.AddListener(Close);

            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btRt = btnTextGo.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;
            Text btnLabel = btnTextGo.GetComponent<Text>();
            btnLabel.font = font;
            btnLabel.text = "CLOSE [ESC / B]";
            btnLabel.fontSize = 20;
            btnLabel.fontStyle = FontStyle.Bold;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.color = Color.white;
        }

        public void Populate(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) characterId = "C01";
            var profile = FindProfile(characterId);
            bool hasSig = BBSignatureTable.TryBase(characterId, out var signature);
            var card = BBPresentationLibrary.Find(characterId);
            var charDef = CharacterRegistry.GetCharacterById(characterId);

            string displayName = card != null && !string.IsNullOrEmpty(card.displayName)
                ? card.displayName
                : (charDef != null ? charDef.displayName : characterId);

            string archetype = charDef != null ? charDef.archetype.ToUpperInvariant() : "COMBATANT";

            if (_titleText != null) _titleText.text = $"COMMAND LIST — {displayName.ToUpperInvariant()} ({characterId})";
            if (_subTitleText != null) _subTitleText.text = $"ARCHETYPE: {archetype}   |   DRIVE: {(hasSig ? signature.driveName : "Standard Drive").ToUpperInvariant()}";

            var sb = new System.Text.StringBuilder();

            // 1. TUTORIAL / HOW TO PLAY
            sb.AppendLine("=== HOW TO PLAY ===");
            sb.AppendLine(DemonLordAssetCatalog.TUTORIAL_HOW_TO_PLAY.Trim());
            sb.AppendLine();

            // 2. NORMALS (5A/5B/5C/2A/2B/2C/j.A/j.B/j.C)
            sb.AppendLine("=== NORMALS (GATLING CHAIN: A -> B -> C) ===");
            sb.AppendLine("  5A     Standing Light Attack     [Fast startup, close-range poke]");
            sb.AppendLine("  5B     Standing Medium Attack    [Mid-range strike, cancels into 5C]");
            sb.AppendLine("  5C     Standing Heavy Attack     [High damage, launcher / wall push]");
            sb.AppendLine("  2A     Crouching Light Attack    [Low poke, rapid chain starter]");
            sb.AppendLine("  2B     Crouching Medium Attack   [Crouching mid, solid confirm]");
            sb.AppendLine("  2C     Crouching Heavy Attack    [Low sweep, causes knockdown]");
            sb.AppendLine("  j.A    Jumping Light Attack      [Air-to-air quick priority]");
            sb.AppendLine("  j.B    Jumping Medium Attack     [Air-to-ground jump-in strike]");
            sb.AppendLine("  j.C    Jumping Heavy Attack      [Heavy air overhead attack]");
            sb.AppendLine();

            // 3. DRIVE (D Button)
            string driveName = hasSig ? signature.driveName : "Archetype Drive";
            sb.AppendLine($"=== DRIVE MECHANIC: {driveName.ToUpperInvariant()} ===");
            sb.AppendLine($"  5D     {driveName} Standing      [Fighter-unique mechanic & properties]");
            sb.AppendLine($"  2D     {driveName} Low           [Crouching drive attack]");
            sb.AppendLine($"  6D     {driveName} Forward       [Advancing drive skill]");
            sb.AppendLine($"  j.D    {driveName} Aerial        [Airborne drive execution]");
            sb.AppendLine();

            // 4. SPECIALS (With motion notation)
            sb.AppendLine("=== SPECIAL MOVES (MOTION NOTATION) ===");
            if (hasSig)
            {
                sb.AppendLine($"  ★ {signature.specialId}   Signature Special Move [Scale: {signature.damageScale:P0}, Reach: {signature.reachScale:P0}]");
            }
            sb.AppendLine("  236A   Quarter-Circle Forward + A  [Fireball / Forward Assault]");
            sb.AppendLine("  623B   Dragon Punch + B            [Anti-Air Uppercut / Rising Cleave]");
            sb.AppendLine("  214A   Quarter-Circle Back + A     [Defensive Counter / Advancing Flurry]");
            sb.AppendLine("  22C    Down-Down + C               [Ground Slam / Trap Placement]");
            sb.AppendLine();

            // 5. SUPER (Distortion Drive)
            string superId = hasSig && !string.IsNullOrEmpty(signature.superId) ? signature.superId : "236236C";
            sb.AppendLine("=== SUPER MOVE (DISTORTION DRIVE — 50 HEAT) ===");
            sb.AppendLine($"  {superId}   Distortion Drive Super     [Cinematic Domain Arts, Invulnerable Startup]");
            sb.AppendLine();

            // 6. THROWS & UNIVERSAL MECHANICS
            sb.AppendLine("=== UNIVERSAL MECHANICS ===");
            sb.AppendLine("  B + C    Normal Throw / Forward Throw  [Unblockable ground throw at point-blank]");
            sb.AppendLine("  4B + C   Back Throw                    [Throws opponent backward]");
            sb.AppendLine("  j.B + C  Air Throw                     [Aerial unblockable throw]");
            sb.AppendLine("  A + B    Rapid Cancel (50 Heat)        [Cancels active attack back to neutral]");
            sb.AppendLine("  Hold 4   Stand Guard                   [Blocks High & Mid attacks]");
            sb.AppendLine("  Hold 1   Crouch Guard                  [Blocks Low & Mid attacks]");

            if (_bodyText != null) _bodyText.text = sb.ToString();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private static BBCharacterProfile FindProfile(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            var def = CharacterRegistry.GetCharacterById(characterId);
            if (def != null && def.blazBlueProfile is BBCharacterProfile p) return p;

            var profiles = Resources.FindObjectsOfTypeAll<BBCharacterProfile>();
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] != null && !string.IsNullOrEmpty(profiles[i].characterId) && profiles[i].characterId.StartsWith(characterId))
                    return profiles[i];
            }
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("BBProfile_" + characterId + " t:BBCharacterProfile");
            for (int i = 0; i < guids.Length; i++)
            {
                var prof = UnityEditor.AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
                if (prof != null) return prof;
            }
#endif
            return null;
        }
    }
}