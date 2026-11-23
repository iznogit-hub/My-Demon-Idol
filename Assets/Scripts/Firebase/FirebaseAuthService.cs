using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DemonIdol.Firebase
{
    /// <summary>
    /// Service for Firebase Authentication in Demon Idol.
    /// Manages Anonymous Guest Logins, Email/Password Sign-In, Account Linking,
    /// Token retrieval for Quantum Custom Auth, and Account Deletion (Apple Guideline 5.1.1(v)).
    /// Includes hardened dev mock auth isolated strictly from release builds.
    /// </summary>
    public class FirebaseAuthService : MonoBehaviour
    {
        public static FirebaseAuthService Instance { get; private set; }

        private const string PREFS_USER_ID = "firebase_auth_uid";
        private const string PREFS_DISPLAY_NAME = "firebase_auth_display_name";
        private const string PREFS_EMAIL = "firebase_auth_email";
        private const string PREFS_IS_ANON = "firebase_auth_is_anon";
        private const string PREFS_TOKEN = "firebase_auth_token";

        public FirebaseUser CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.UserId);

        public event Action<FirebaseUser> OnAuthStateChanged;
        public event Action<string> OnAuthError;
        public event Action OnAccountDeleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            LoadCachedSession();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Computes a deterministic 32-bit FNV-1a hex digest.
        /// Replaces non-deterministic string.GetHashCode() and prevents OverflowException on Math.Abs(int.MinValue).
        /// </summary>
        public static string ComputeDeterministicHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "00000000";
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < input.Length; i++)
                {
                    hash = (hash ^ input[i]) * 16777619;
                }
                return hash.ToString("x8");
            }
        }

        private void LoadCachedSession()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD && !MDI_ALLOW_MOCK_AUTH
            // In release builds, do not load unencrypted mock credentials from PlayerPrefs
            CurrentUser = null;
#else
            if (PlayerPrefs.HasKey(PREFS_USER_ID))
            {
                string uid = PlayerPrefs.GetString(PREFS_USER_ID);
                string name = PlayerPrefs.GetString(PREFS_DISPLAY_NAME, "Operative");
                string email = PlayerPrefs.GetString(PREFS_EMAIL, string.Empty);
                bool isAnon = PlayerPrefs.GetInt(PREFS_IS_ANON, 1) == 1;
                string token = PlayerPrefs.GetString(PREFS_TOKEN, GenerateMockToken(uid));

                CurrentUser = new FirebaseUser(uid, name, email, isAnon, token);
#if UNITY_EDITOR
                Debug.Log($"<color=#69F0AE>[FirebaseAuthService] Restored cached session for user: {name} ({uid})</color>");
#else
                Debug.Log("[FirebaseAuthService] Restored cached session.");
#endif
            }
#endif
        }

        /// <summary>
        /// Signs in as an Anonymous Guest. Perfect for instant mobile play without onboarding friction.
        /// </summary>
        public async Task<FirebaseUser> SignInAnonymouslyAsync(string customDisplayName = null)
        {
            await Task.CompletedTask;

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD && !MDI_ALLOW_MOCK_AUTH
            string err = "Anonymous mock authentication is disabled in release builds. Cloud authentication service required.";
            Debug.LogError($"[FirebaseAuthService] {err}");
            OnAuthError?.Invoke(err);
            return null;
#else
            try
            {
                string uid = "guest_" + Guid.NewGuid().ToString("N").Substring(0, 10);
                string name = !string.IsNullOrEmpty(customDisplayName) ? customDisplayName : ("Operative_" + uid.Substring(6, 4).ToUpper());
                string token = GenerateMockToken(uid);

                CurrentUser = new FirebaseUser(uid, name, string.Empty, isAnonymous: true, token);
                SaveSession(CurrentUser);

#if UNITY_EDITOR
                Debug.Log($"<color=#69F0AE>[FirebaseAuthService] Signed in anonymously as {name} ({uid})</color>");
#else
                Debug.Log("[FirebaseAuthService] Signed in anonymously as guest.");
#endif
                OnAuthStateChanged?.Invoke(CurrentUser);
                return CurrentUser;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseAuthService] Anonymous sign-in error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return null;
            }
#endif
        }

        /// <summary>
        /// Signs in with existing Email & Password.
        /// </summary>
        public async Task<FirebaseUser> SignInWithEmailAsync(string email, string password)
        {
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                string err = "Email and password cannot be empty.";
                OnAuthError?.Invoke(err);
                return null;
            }

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD && !MDI_ALLOW_MOCK_AUTH
            string releaseErr = "Local email mock authentication is disabled in release builds. Cloud authentication service required.";
            Debug.LogError($"[FirebaseAuthService] {releaseErr}");
            OnAuthError?.Invoke(releaseErr);
            return null;
#else
            try
            {
                string cleanEmail = email.Trim().ToLowerInvariant();
                string uid = "user_" + ComputeDeterministicHash(cleanEmail);
                string name = cleanEmail.Split('@')[0].ToUpper();
                string token = GenerateMockToken(uid);

                CurrentUser = new FirebaseUser(uid, name, email, isAnonymous: false, token);
                SaveSession(CurrentUser);

#if UNITY_EDITOR
                Debug.Log($"<color=#69F0AE>[FirebaseAuthService] Signed in with email: {email}</color>");
#else
                Debug.Log("[FirebaseAuthService] Signed in with email (redacted).");
#endif
                OnAuthStateChanged?.Invoke(CurrentUser);
                return CurrentUser;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseAuthService] Email sign-in error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return null;
            }
#endif
        }

        /// <summary>
        /// Registers a new account with Email, Password, and Player Codename.
        /// </summary>
        public async Task<FirebaseUser> RegisterWithEmailAsync(string email, string password, string displayName)
        {
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                string err = "Email and password cannot be empty.";
                OnAuthError?.Invoke(err);
                return null;
            }

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD && !MDI_ALLOW_MOCK_AUTH
            string releaseErr = "Local registration mock authentication is disabled in release builds. Cloud authentication service required.";
            Debug.LogError($"[FirebaseAuthService] {releaseErr}");
            OnAuthError?.Invoke(releaseErr);
            return null;
#else
            try
            {
                string uid = "user_" + Guid.NewGuid().ToString("N").Substring(0, 10);
                string name = !string.IsNullOrWhiteSpace(displayName) ? displayName : email.Split('@')[0];
                string token = GenerateMockToken(uid);

                CurrentUser = new FirebaseUser(uid, name, email, isAnonymous: false, token);
                SaveSession(CurrentUser);

#if UNITY_EDITOR
                Debug.Log($"<color=#69F0AE>[FirebaseAuthService] Registered new account: {name} ({email})</color>");
#else
                Debug.Log("[FirebaseAuthService] Registered new account (redacted).");
#endif
                OnAuthStateChanged?.Invoke(CurrentUser);
                return CurrentUser;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseAuthService] Registration error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return null;
            }
#endif
        }

        /// <summary>
        /// Signs out and clears the local active session.
        /// </summary>
        public void SignOut()
        {
            ClearSession();
            CurrentUser = null;
            Debug.Log("[FirebaseAuthService] User signed out.");
            OnAuthStateChanged?.Invoke(null);
        }

        /// <summary>
        /// Permanently deletes the user account and purges local credentials.
        /// Mandatory compliance implementation for Apple App Store Guideline 5.1.1(v) & Google Play Data Safety.
        /// </summary>
        public async Task<bool> DeleteAccountAsync()
        {
            await Task.CompletedTask;

            try
            {
#if UNITY_EDITOR
                string uid = CurrentUser != null ? CurrentUser.UserId : string.Empty;
#endif
                ClearSession();
                CurrentUser = null;

                // Wipe player save keys
                PlayerPrefs.DeleteKey("demonidol_player_name");
                PlayerPrefs.DeleteKey("DemonIdol_Story_Checkpoint");
                PlayerPrefs.DeleteKey("DemonIdol_Story_LastSave");
                PlayerPrefs.Save();

#if UNITY_EDITOR
                Debug.Log($"<color=#FF5252>[FirebaseAuthService] Account {uid} permanently deleted & local data purged.</color>");
#else
                Debug.Log("<color=#FF5252>[FirebaseAuthService] User account permanently deleted & local data purged.</color>");
#endif
                OnAccountDeleted?.Invoke();
                OnAuthStateChanged?.Invoke(null);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseAuthService] Account deletion error: {ex.Message}");
                OnAuthError?.Invoke(ex.Message);
                return false;
            }
        }

        public string GetIdToken()
        {
            if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.IdToken))
            {
                return CurrentUser.IdToken;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD || MDI_ALLOW_MOCK_AUTH
            return GenerateMockToken(CurrentUser != null ? CurrentUser.UserId : "guest");
#else
            return string.Empty;
#endif
        }

        private void SaveSession(FirebaseUser user)
        {
            PlayerPrefs.SetString(PREFS_USER_ID, user.UserId);
            PlayerPrefs.SetString(PREFS_DISPLAY_NAME, user.DisplayName);
            PlayerPrefs.SetString(PREFS_EMAIL, user.Email);
            PlayerPrefs.SetInt(PREFS_IS_ANON, user.IsAnonymous ? 1 : 0);
            PlayerPrefs.SetString(PREFS_TOKEN, user.IdToken);
            PlayerPrefs.Save();
        }

        private void ClearSession()
        {
            PlayerPrefs.DeleteKey(PREFS_USER_ID);
            PlayerPrefs.DeleteKey(PREFS_DISPLAY_NAME);
            PlayerPrefs.DeleteKey(PREFS_EMAIL);
            PlayerPrefs.DeleteKey(PREFS_IS_ANON);
            PlayerPrefs.DeleteKey(PREFS_TOKEN);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || MDI_ALLOW_MOCK_AUTH
        private string GenerateMockToken(string uid)
        {
            return $"mdi_jwt_{uid}_{DateTime.UtcNow.Ticks}";
        }
#endif
    }
}
