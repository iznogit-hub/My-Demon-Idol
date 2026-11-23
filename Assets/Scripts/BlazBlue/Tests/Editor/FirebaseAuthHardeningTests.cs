#if UNITY_EDITOR
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using DemonIdol.Firebase;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class FirebaseAuthHardeningTests
    {
        private GameObject _authGo;
        private FirebaseAuthService _authService;

        [SetUp]
        public void SetUp()
        {
            _authGo = new GameObject("Temp_AuthTest");
            _authService = _authGo.AddComponent<FirebaseAuthService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_authGo != null)
            {
                Object.DestroyImmediate(_authGo);
            }
        }

        [Test]
        public void DeterministicHash_Consistent_AcrossCalls()
        {
            string email = "fighter.akio@celestialsoul.com";
            string hash1 = FirebaseAuthService.ComputeDeterministicHash(email);
            string hash2 = FirebaseAuthService.ComputeDeterministicHash(email);

            Assert.IsNotNull(hash1);
            Assert.AreEqual(8, hash1.Length, "Hash must be an 8-character hexadecimal string.");
            Assert.AreEqual(hash1, hash2, "Deterministic FNV-1a hash must be 100% stable across invocations.");

            // Different emails must produce distinct hashes
            string otherEmail = "fighter.kanna@celestialsoul.com";
            string otherHash = FirebaseAuthService.ComputeDeterministicHash(otherEmail);
            Assert.AreNotEqual(hash1, otherHash);

            // Empty or null string handling
            Assert.AreEqual("00000000", FirebaseAuthService.ComputeDeterministicHash(null));
            Assert.AreEqual("00000000", FirebaseAuthService.ComputeDeterministicHash(""));
        }

        [Test]
        public async Task SignInWithEmail_ProducesDeterministicUID_WithoutOverflow()
        {
            string email = "akio@raimon.edu";
            string expectedHash = FirebaseAuthService.ComputeDeterministicHash(email);
            string expectedUid = "user_" + expectedHash;

            var user = await _authService.SignInWithEmailAsync(email, "DemonPass123!");

            Assert.IsNotNull(user);
            Assert.AreEqual(expectedUid, user.UserId);
            Assert.AreEqual("AKIO", user.DisplayName);
            Assert.IsFalse(user.IsAnonymous);
            Assert.IsTrue(_authService.IsAuthenticated);
        }

        [Test]
        public async Task AnonymousSignIn_GeneratesGuestUid()
        {
            var user = await _authService.SignInAnonymouslyAsync("ShadowOperative");

            Assert.IsNotNull(user);
            Assert.IsTrue(user.UserId.StartsWith("guest_"));
            Assert.AreEqual("ShadowOperative", user.DisplayName);
            Assert.IsTrue(user.IsAnonymous);
            Assert.IsTrue(_authService.IsAuthenticated);
        }
    }
}
#endif
