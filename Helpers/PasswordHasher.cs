using System;
using System.Security.Cryptography;

namespace EclatEtSoin.Helpers
{
    public static class PasswordHasher
    {
        private const int Iterations = 12000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);
            var hash = Derive(password, salt);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored) || stored.IndexOf(':') < 0)
                return false;
            var parts = stored.Split(':');
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Derive(password, salt);
            return SlowEquals(expected, actual);
        }

        private static byte[] Derive(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                return pbkdf2.GetBytes(HashSize);
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            var diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
