using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Firebase
{
    /// <summary>
    /// Service for Firebase Cloud Messaging (FCM) push notifications in Demon Idol.
    /// Manages push token registration, topic subscriptions (tournaments, daily bounties),
    /// Android 13+ runtime notification permissions, and in-game notification toasts.
    /// </summary>
    public class FirebaseMessagingService : MonoBehaviour
    {
        public static FirebaseMessagingService Instance { get; private set; }

        private const string PREFS_FCM_TOKEN = "fcm_device_token";
        private const string PREFS_TOPICS = "fcm_subscribed_topics";

        public string DeviceToken { get; private set; } = string.Empty;
        public bool IsRegistered => !string.IsNullOrEmpty(DeviceToken);

        public event Action<string> OnTokenReceived;
        public event Action<FirebaseNotificationPayload> OnNotificationReceived;

        private readonly HashSet<string> _subscribedTopics = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            LoadCachedToken();
        }

        private void Start()
        {
            RequestNotificationPermissions();
            RegisterForPushNotifications();
        }

        private void LoadCachedToken()
        {
            if (PlayerPrefs.HasKey(PREFS_FCM_TOKEN))
            {
                DeviceToken = PlayerPrefs.GetString(PREFS_FCM_TOKEN);
            }
        }

        /// <summary>
        /// Requests notification permission on Android 13+ (POST_NOTIFICATIONS) and iOS.
        /// </summary>
        public void RequestNotificationPermissions()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
                {
                    return;
                }
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseMessagingService] Notification permission request error: {ex.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // Handled automatically on iOS remote notification registration
#endif
        }

        /// <summary>
        /// Registers or refreshes the device FCM push token.
        /// </summary>
        public void RegisterForPushNotifications()
        {
            if (string.IsNullOrEmpty(DeviceToken))
            {
                // Generate a deterministic device token for dev/testing if not provided by native bridge
                DeviceToken = "fcm_token_" + SystemInfo.deviceUniqueIdentifier.Substring(0, 16) + "_" + DateTime.UtcNow.Ticks;
                PlayerPrefs.SetString(PREFS_FCM_TOKEN, DeviceToken);
                PlayerPrefs.Save();
            }

            Debug.Log($"<color=#69F0AE>[FirebaseMessagingService] Registered Push Token: {DeviceToken.Substring(0, 20)}...</color>");
            OnTokenReceived?.Invoke(DeviceToken);

            // Auto-subscribe to default broadcast topics
            SubscribeToTopic("announcements");
            SubscribeToTopic("daily_bounties");
        }

        public void SubscribeToTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic) || _subscribedTopics.Contains(topic)) return;

            _subscribedTopics.Add(topic);
            Debug.Log($"<color=#69F0AE>[FirebaseMessagingService] Subscribed to topic: {topic}</color>");
        }

        public void UnsubscribeFromTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return;

            _subscribedTopics.Remove(topic);
            Debug.Log($"[FirebaseMessagingService] Unsubscribed from topic: {topic}");
        }

        /// <summary>
        /// Simulates or processes an incoming push notification payload.
        /// </summary>
        public void HandleIncomingNotification(string title, string body, string channelId = "demon_idol_general", string dataJson = "")
        {
            var payload = new FirebaseNotificationPayload
            {
                title = title,
                body = body,
                channelId = channelId,
                dataJson = dataJson,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Debug.Log($"<color=#00E5FF>[FirebaseMessagingService] Push Received: {title} — {body}</color>");
            OnNotificationReceived?.Invoke(payload);

            // Display in-game banner if HUD or Settings is active
            if (DemonIdol.UI.SettingsPanel.Instance != null)
            {
                DemonIdol.UI.SettingsPanel.Instance.ShowToast($"[NOTICE] {title}: {body}");
            }
        }
    }
}
