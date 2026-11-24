using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DemonIdol.Combat;
using DemonIdol.UI;

namespace DemonIdol.Launch
{
    /// <summary>
    /// Project-owned after-match choices on the UFE scene. Rematch, next fight, and menu
    /// stay here instead of falling through to the vendor versus menu.
    /// </summary>
    public class MatchResultsOverlay : MonoBehaviour
    {
        public static MatchResultsOverlay Instance { get; private set; }

        public void Show(int winningPlayer, string winnerName, int grantedXp, bool firstGrant)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Show(winningPlayer, winnerName, grantedXp, firstGrant);
                return;
            }
            Instance = this;
            Build(winningPlayer, winnerName, grantedXp, firstGrant);
        }

        public void ShowFightResult(int winningPlayer, string winnerName, string quote, DemonIdol.BlazBlue.BBMatchStats stats, DemonIdol.BlazBlue.BBMatchFlow flow)
        {
            if (Instance != null && Instance != this)
            {
                Instance.ShowFightResult(winningPlayer, winnerName, quote, stats, flow);
                return;
            }
            Instance = this;
            Build(winningPlayer, winnerName, 0, false);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string statsLine = stats == null
                ? "Stats pending."
                : "DMG " + stats.P1Damage + " / " + stats.P2Damage
                  + "   COMBO " + stats.P1MaxCombo + " / " + stats.P2MaxCombo
                  + "   SUPERS " + stats.P1Supers + " / " + stats.P2Supers;
            CreateLabel(transform, "ResultsQuote", string.IsNullOrEmpty(quote) ? "Quote slot empty." : quote, new Vector2(0f, 10f), 20, font);
            CreateLabel(transform, "ResultsStats", statsLine, new Vector2(0f, -30f), 18, font);
            CreateLabel(transform, "ResultsPose", "WINNER POSE CAMERA", new Vector2(0f, 200f), 16, font);
            if (flow != null)
            {
                flow.WinnerQuoteHook = quote;
                flow.Screen = DemonIdol.BlazBlue.BBFlowScreen.MatchEnd;
            }
        }

        public static MatchResultsOverlay CreateOrReuse()
        {
            if (Instance != null)
                return Instance;
            var host = new GameObject("MatchResultsOverlay", typeof(RectTransform));
            return host.AddComponent<MatchResultsOverlay>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Build(int winningPlayer, string winnerName, int grantedXp, bool firstGrant)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            Transform parent = UiHierarchy.ResolveCanvasParent(null);
            if (parent == null)
            {
                var canvasObject = new GameObject("MatchResultsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                parent = canvasObject.transform;
            }
            transform.SetParent(parent, false);
            RectTransform root = UiHierarchy.RequireRectTransform(gameObject);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            if (GetComponent<SafeAreaFitter>() == null)
                gameObject.AddComponent<SafeAreaFitter>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            string title = winningPlayer == 0 ? "MATCH DRAW" : "VICTORY: " + (winnerName ?? "FIGHTER").ToUpperInvariant();
            string reward = firstGrant
                ? "Cosmetic XP +" + grantedXp + " (once for this match)"
                : "Reward already recorded for this match";
            CreateLabel(transform, "ResultsTitle", title, new Vector2(0f, 160f), 36, font);
            CreateLabel(transform, "ResultsReward", reward, new Vector2(0f, 90f), 22, font);
            CreateLabel(transform, "ResultsStage", StageNote(), new Vector2(0f, 40f), 18, font);

            CreateButton(transform, "Btn_Rematch", "REMATCH", new Vector2(-330f, -80f), Rematch, font);
            CreateButton(transform, "Btn_Next", "NEXT FIGHT", new Vector2(-110f, -80f), Next, font);
            CreateButton(transform, "Btn_CharSelect", "CHAR SELECT", new Vector2(110f, -80f), CharacterSelect, font);
            CreateButton(transform, "Btn_Menu", "MAIN MENU", new Vector2(330f, -80f), Menu, font);
        }

        static string StageNote()
        {
            string stageId = LaunchServices.Session.StageId;
            return "Stage " + stageId + " [ 2.5D Homegrown Combat Arena ]";
        }

        void Rematch()
        {
            LaunchServices.Session.BeginRematch();
            if (DemonIdol.Combat.HomegrownCombatDirector.Instance != null)
                DemonIdol.Combat.HomegrownCombatDirector.Instance.Rematch();
            Destroy(gameObject);
        }

        void Next()
        {
            OfflineMatchSession session = LaunchServices.Session;
            if (session.Mode == PlayModeKind.Arcade && session.TryAdvanceArcade(out _))
            {
                session.BeginMatch(session.Player1Id, session.Player2Id, session.StageId);
                if (DemonIdol.Combat.HomegrownCombatDirector.Instance != null)
                {
                    DemonIdol.Combat.HomegrownCombatDirector.Instance.SetMatchContext(session.Player1Id, session.Player2Id, session.StageId, true, false);
                    DemonIdol.Combat.HomegrownCombatDirector.Instance.Rematch();
                    Destroy(gameObject);
                    return;
                }
                CharacterSelect();
                return;
            }

            CharacterSelect();
        }

        void CharacterSelect()
        {
            LaunchServices.Session.MarkMenu();
            PlayerPrefs.SetInt("DemonIdol_ReturnToCharSelect", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("MainMenu");
        }

        void Menu()
        {
            LaunchServices.Session.MarkMenu();
            PlayerPrefs.SetInt("DemonIdol_ReturnToCharSelect", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene("MainMenu");
        }

        static void CreateLabel(Transform parent, string name, string text, Vector2 position, int size, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900f, 50f);
            Text label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        static void CreateButton(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200f, 64f);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.16f, 0.94f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(go.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text label = textObject.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }
    }
}
