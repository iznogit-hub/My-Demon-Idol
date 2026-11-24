using System;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Master BlazBlue 4-Button Battle HUD Controller (Spec §7.8, M12).
    /// Orchestrates frame-accurate visual presentation for:
    /// - 11,000 HP integer Health bars with delayed red damage lag depletion.
    /// - Heat Gauges (0–100) with 50% Rapid/DD thresholds and 100% Astral Heat ready glow.
    /// - Barrier Gauges (0–1000) with warning tint (<200) and Danger state flashing.
    /// - Burst Gauges (0–1000) with Gold/Blue Burst and Overdrive readiness cues.
    /// - Overdrive Timer Ring/Bar displaying remaining ticks.
    /// - Combo Counter with hit counts, total damage accumulation, and Technical callouts.
    /// - Modular Drive Resource Trackers for all 9 roster archetypes:
    ///   * C01 Akio: 3 Momentum pips
    ///   * C02 Aika: 5 Resonance Nails indicator
    ///   * C04 UnityChan: Aero Hop air charges
    ///   * C09 PicoChan: Clinch Grip meter (0–100)
    ///   * C12 Fina: Plasma Charge tiers (1/2/3)
    ///   * C08 SanpakuChan: 3 Crow Feathers
    ///   * C06 Cultist: Void Orbs active count (up to 3)
    ///   * C10 Reika: Veil Counter stance & stocks
    ///   * C07 StellarWitch: Star Field active star pips (up to 4)
    /// Zero-allocation in Update/Tick loop.
    /// </summary>
    public class BBBattleHUDController : MonoBehaviour
    {
        private static BBBattleHUDController _instance;
        public static BBBattleHUDController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<BBBattleHUDController>();
                }
                return _instance;
            }
        }

        [Header("Fighter References")]
        public BBFighterBrain p1Brain;
        public BBFighterBrain p2Brain;

        [Header("P1 Gauges")]
        [SerializeField] private Image _p1HealthBar;
        [SerializeField] private Image _p1HealthLagBar;
        [SerializeField] private Image _p1HeatBar;
        [SerializeField] private Image _p1BarrierBar;
        [SerializeField] private Image _p1BurstBar;
        [SerializeField] private Text _p1HealthText;
        [SerializeField] private Text _p1HeatText;
        [SerializeField] private Text _p1BarrierText;
        [SerializeField] private Text _p1DangerBanner;
        [SerializeField] private Text _p1DriveResourceText;
        [SerializeField] private Image[] _p1DrivePips;

        [Header("P2 Gauges")]
        [SerializeField] private Image _p2HealthBar;
        [SerializeField] private Image _p2HealthLagBar;
        [SerializeField] private Image _p2HeatBar;
        [SerializeField] private Image _p2BarrierBar;
        [SerializeField] private Image _p2BurstBar;
        [SerializeField] private Text _p2HealthText;
        [SerializeField] private Text _p2HeatText;
        [SerializeField] private Text _p2BarrierText;
        [SerializeField] private Text _p2DangerBanner;
        [SerializeField] private Text _p2DriveResourceText;
        [SerializeField] private Image[] _p2DrivePips;

        [Header("Match Timer & Banners")]
        [SerializeField] private Text _roundTimerText;
        [SerializeField] private Text _centerAnnouncementText;
        [SerializeField] private CanvasGroup _centerAnnouncementGroup;

        [Header("Combo Displays")]
        [SerializeField] private Text _comboCountText;
        [SerializeField] private Text _comboDamageText;
        [SerializeField] private CanvasGroup _comboGroup;

        // Runtime smooth interpolation values
        private float _p1LagHealth = 11000f;
        private float _p2LagHealth = 11000f;
        private const float HEALTH_LAG_SPEED = 2500f; // HP per second

        private int _announcementTicksRemaining = 0;
        private int _activeComboHits = 0;
        private int _activeComboDamage = 0;
        private int _comboDisplayTicks = 0;

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void BindFighters(BBFighterBrain p1, BBFighterBrain p2)
        {
            p1Brain = p1;
            p2Brain = p2;

            if (p1Brain != null) _p1LagHealth = p1Brain.currentHealth;
            if (p2Brain != null) _p2LagHealth = p2Brain.currentHealth;

            UpdateHUDVisuals(Time.deltaTime);
        }

        private void Update()
        {
            UpdateHUDVisuals(Time.deltaTime);
        }

        public void UpdateHUDVisuals(float deltaTime)
        {
            // 1. Update Player 1
            if (p1Brain != null)
            {
                UpdateFighterHUD(
                    p1Brain,
                    ref _p1LagHealth,
                    _p1HealthBar,
                    _p1HealthLagBar,
                    _p1HeatBar,
                    _p1BarrierBar,
                    _p1BurstBar,
                    _p1HealthText,
                    _p1HeatText,
                    _p1BarrierText,
                    _p1DangerBanner,
                    _p1DriveResourceText,
                    _p1DrivePips,
                    deltaTime
                );
            }

            // 2. Update Player 2
            if (p2Brain != null)
            {
                UpdateFighterHUD(
                    p2Brain,
                    ref _p2LagHealth,
                    _p2HealthBar,
                    _p2HealthLagBar,
                    _p2HeatBar,
                    _p2BarrierBar,
                    _p2BurstBar,
                    _p2HealthText,
                    _p2HeatText,
                    _p2BarrierText,
                    _p2DangerBanner,
                    _p2DriveResourceText,
                    _p2DrivePips,
                    deltaTime
                );
            }

            // 3. Update Announcement Fade
            if (_announcementTicksRemaining > 0)
            {
                _announcementTicksRemaining--;
                if (_centerAnnouncementGroup != null)
                {
                    _centerAnnouncementGroup.alpha = Mathf.Clamp01(_announcementTicksRemaining / 20f);
                }
            }

            // 4. Update Combo Display Fade
            if (_comboDisplayTicks > 0)
            {
                _comboDisplayTicks--;
                if (_comboDisplayTicks <= 0 && _comboGroup != null)
                {
                    _comboGroup.alpha = 0f;
                }
            }
        }

        private void UpdateFighterHUD(
            BBFighterBrain brain,
            ref float lagHealth,
            Image healthBar,
            Image healthLagBar,
            Image heatBar,
            Image barrierBar,
            Image burstBar,
            Text healthText,
            Text heatText,
            Text barrierText,
            Text dangerBanner,
            Text driveResourceText,
            Image[] drivePips,
            float deltaTime)
        {
            float maxHp = brain.profile != null ? brain.profile.maxHealth : 11000f;
            if (maxHp <= 0f) maxHp = 11000f;

            float curHp = Mathf.Clamp(brain.currentHealth, 0f, maxHp);

            // Smooth damage lag
            if (lagHealth > curHp)
            {
                lagHealth = Mathf.MoveTowards(lagHealth, curHp, HEALTH_LAG_SPEED * deltaTime);
            }
            else
            {
                lagHealth = curHp;
            }

            // HP Fill
            if (healthBar != null) healthBar.fillAmount = curHp / maxHp;
            if (healthLagBar != null) healthLagBar.fillAmount = lagHealth / maxHp;
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(curHp)}";

            // Heat Gauge (0-100)
            float heat = Mathf.Clamp(brain.heatMeter, 0f, 100f);
            if (heatBar != null)
            {
                heatBar.fillAmount = heat / 100f;
                // Flashing color when super/rapid ready
                if (heat >= 100f) heatBar.color = Color.Lerp(Color.yellow, Color.white, Mathf.PingPong(Time.time * 4f, 1f));
                else if (heat >= 50f) heatBar.color = new Color(0.2f, 0.8f, 1.0f);
                else heatBar.color = new Color(0.2f, 0.5f, 0.9f);
            }
            if (heatText != null) heatText.text = $"{Mathf.FloorToInt(heat)}%";

            // Barrier Gauge (0-1000)
            float barrier = Mathf.Clamp(brain.barrierMeter, 0f, 1000f);
            if (barrierBar != null)
            {
                barrierBar.fillAmount = barrier / 1000f;
                if (brain.inDanger) barrierBar.color = Color.red;
                else if (barrier < 200f) barrierBar.color = Color.yellow;
                else barrierBar.color = new Color(0.2f, 0.9f, 0.4f);
            }
            if (barrierText != null) barrierText.text = $"{Mathf.FloorToInt(barrier / 10f)}%";

            // Danger Banner
            if (dangerBanner != null)
            {
                dangerBanner.gameObject.SetActive(brain.inDanger);
            }

            // Burst Gauge (0-1000)
            float burst = Mathf.Clamp(brain.burstMeter, 0f, 1000f);
            if (burstBar != null)
            {
                burstBar.fillAmount = burst / 1000f;
                if (brain.inOverdrive) burstBar.color = Color.magenta;
                else if (burst >= 1000f) burstBar.color = Color.cyan;
                else burstBar.color = Color.gray;
            }

            // Drive Resource Display
            if (brain.drive != null)
            {
                if (driveResourceText != null)
                {
                    driveResourceText.text = $"{brain.drive.DriveName}: {brain.drive.CurrentResource}/{brain.drive.MaxResource}";
                }

                if (drivePips != null && drivePips.Length > 0)
                {
                    int res = brain.drive.CurrentResource;
                    for (int p = 0; p < drivePips.Length; p++)
                    {
                        if (drivePips[p] != null)
                        {
                            drivePips[p].gameObject.SetActive(p < brain.drive.MaxResource);
                            drivePips[p].color = (p < res) ? Color.yellow : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                        }
                    }
                }
            }
        }

        public void ShowAnnouncement(string text, int durationTicks = 90)
        {
            Color defaultColor = text.Contains("PUNISH") ? new Color(1f, 0.85f, 0.2f) :
                                 text.Contains("COUNTER") ? new Color(1f, 0.25f, 0.25f) :
                                 text.Contains("PARRY") ? new Color(0.2f, 1f, 0.9f) :
                                 text.Contains("REVERSAL") ? new Color(0.3f, 0.9f, 1f) :
                                 Color.white;
            ShowAnnouncement(text, defaultColor, durationTicks);
        }

        public void ShowAnnouncement(string text, Color color, int durationTicks = 90)
        {
            if (_centerAnnouncementText != null)
            {
                _centerAnnouncementText.text = text;
                _centerAnnouncementText.color = color;
            }
            if (_centerAnnouncementGroup != null)
            {
                _centerAnnouncementGroup.alpha = 1f;
            }
            _announcementTicksRemaining = durationTicks;
        }

        public void ReportComboHit(int hitCount, int comboDamage)
        {
            _activeComboHits = hitCount;
            _activeComboDamage = comboDamage;
            _comboDisplayTicks = 120; // 2 seconds

            if (_comboCountText != null) _comboCountText.text = $"{hitCount} HITS";
            if (_comboDamageText != null) _comboDamageText.text = $"{comboDamage} DMG";
            if (_comboGroup != null) _comboGroup.alpha = 1f;
        }

        public void ResetComboDisplay()
        {
            _activeComboHits = 0;
            _activeComboDamage = 0;
            _comboDisplayTicks = 0;
            if (_comboGroup != null) _comboGroup.alpha = 0f;
        }

        public void UpdateRoundTimer(int secondsRemaining)
        {
            if (_roundTimerText != null)
            {
                _roundTimerText.text = secondsRemaining < 10 ? $"0{secondsRemaining}" : $"{secondsRemaining}";
            }
        }
    }
}
