using System.Security.Cryptography;
using System.Text;
using amorphie.token.core.Enums;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace amorphie.token.core.Helpers
{

    public class PasswordHasherOptions
    {



        private static readonly RandomNumberGenerator _defaultRng = RandomNumberGenerator.Create(); // secure PRNG
        public int IterationCount { get; set; } = 10000;
        internal RandomNumberGenerator Rng { get; set; } = _defaultRng;
    }
    public interface IPasswordHasher
    {
        string HashPassword(string password, string salt = "");
        PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword, string salt = "");



    }
    public class PasswordHasher : IPasswordHasher
    {
        private readonly int _iterCount;
        private readonly RandomNumberGenerator _rng;


    
        public PasswordHasher(PasswordHasherOptions? options = null)
        {
            var opt = options ?? new PasswordHasherOptions();
            _iterCount = opt.IterationCount;
            _rng = opt.Rng;
            if (_iterCount < 1)
            {
                throw new InvalidOperationException("Invalid iteration count for password hasher");
            }
        }

        

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null && b == null)
            {
                return true;
            }
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }
            var areSame = true;
            for (var i = 0; i < a.Length; i++)
            {
                areSame &= (a[i] == b[i]);
            }
            return areSame;
        }
        public string HashPassword(string password, string salt = "")
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }
            var passToHash = password;
            if (!string.IsNullOrWhiteSpace(salt))
            {
                passToHash = $"PX_{salt}_SX_{password}";
            }
            return Convert.ToBase64String(HashPassword(passToHash, _rng));



        }



        private byte[] HashPassword(string password, RandomNumberGenerator rng)
        {
            return HashPassword(password, rng,
                prf: KeyDerivationPrf.HMACSHA256,
                iterCount: _iterCount,
                saltSize: 128 / 8,
                numBytesRequested: 256 / 8);
        }
        private byte[] HashPassword(string password, RandomNumberGenerator rng,
             KeyDerivationPrf prf, int iterCount, int saltSize, int numBytesRequested)
        {
            // Produce a version 3 (see comment above) text hash.
            byte[] salt = new byte[saltSize];
            rng.GetBytes(salt);
            byte[] subkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, numBytesRequested);



            var outputBytes = new byte[13 + salt.Length + subkey.Length];
            outputBytes[0] = 0x01; // format marker
            WriteNetworkByteOrder(outputBytes, 1, (uint)prf);
            WriteNetworkByteOrder(outputBytes, 5, (uint)iterCount);
            WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);
            return outputBytes;
        }
        private void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)(value >> 0);
        }
        private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        {
            return ((uint)(buffer[offset + 0]) << 24)
                | ((uint)(buffer[offset + 1]) << 16)
                | ((uint)(buffer[offset + 2]) << 8)
                | ((uint)(buffer[offset + 3]));
        }
        public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword, string salt = "")
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                throw new ArgumentNullException(nameof(hashedPassword));
            }
            if (string.IsNullOrWhiteSpace(providedPassword))
            {
                throw new ArgumentNullException(nameof(providedPassword));
            }
            var providedPasswordToCheck = providedPassword;
            if (!string.IsNullOrWhiteSpace(salt))
            {
                providedPasswordToCheck = $"PX_{salt}_SX_{providedPassword}";
            }
            byte[] decodedHashedPassword = Convert.FromBase64String(hashedPassword);



            // read the format marker from the hashed password
            if (decodedHashedPassword.Length == 0)
            {
                return PasswordVerificationResult.Failed;
            }
            switch (decodedHashedPassword[0])
            {
                case 0x01:
                    int embeddedIterCount;
                    if (VerifyHashedPassword(decodedHashedPassword, providedPasswordToCheck, out embeddedIterCount))
                    {
                        // If this hasher was configured with a higher iteration count, change the entry now.
                        return (embeddedIterCount < _iterCount)
                            ? PasswordVerificationResult.SuccessRehashNeeded
                            : PasswordVerificationResult.Success;
                    }
                    else
                    {
                        return PasswordVerificationResult.Failed;
                    }



                default:
                    return PasswordVerificationResult.Failed;
            }
        }
        private static bool VerifyHashedPassword(byte[] hashedPassword, string password, out int iterCount)
        {
            iterCount = default(int);



            try
            {
                // Read header information
                var prf = (KeyDerivationPrf)ReadNetworkByteOrder(hashedPassword, 1);
                iterCount = (int)ReadNetworkByteOrder(hashedPassword, 5);
                int saltLength = (int)ReadNetworkByteOrder(hashedPassword, 9);



                // Read the salt: must be >= 128 bits
                if (saltLength < 128 / 8)
                {
                    return false;
                }
                byte[] salt = new byte[saltLength];
                Buffer.BlockCopy(hashedPassword, 13, salt, 0, salt.Length);



                // Read the subkey (the rest of the payload): must be >= 128 bits
                int subkeyLength = hashedPassword.Length - 13 - salt.Length;
                if (subkeyLength < 128 / 8)
                {
                    return false;
                }
                byte[] expectedSubkey = new byte[subkeyLength];
                Buffer.BlockCopy(hashedPassword, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);



                // Hash the incoming password and verify it
                byte[] actualSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, subkeyLength);
                return ByteArraysEqual(actualSubkey, expectedSubkey);
            }
            catch
            {
                // This should never occur except in the case of a malformed payload, where
                // we might go off the end of the array. Regardless, a malformed payload
                // implies verification failed.
                return false;
            }
        }

        public string EncryptString(string text, string keyString, bool isHexAvailable = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(keyString);
            using (Aes aes = Aes.Create())
            {
                using (ICryptoTransform transform = aes.CreateEncryptor(bytes, aes.IV))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(stream))
                            {
                                streamWriter.Write(text);
                            }
                        }

                        byte[] ıV = aes.IV;
                        byte[] array = memoryStream.ToArray();
                        byte[] array2 = new byte[ıV.Length + array.Length];
                        Buffer.BlockCopy(ıV, 0, array2, 0, ıV.Length);
                        Buffer.BlockCopy(array, 0, array2, ıV.Length, array.Length);
                        return isHexAvailable ? BitConverter.ToString(array2).Replace("-", "") : Convert.ToBase64String(array2);
                    }
                }
            }
        }

        public string DecryptString(string cipherText, string keyString)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                return cipherText;
            }

            byte[] array = Convert.FromBase64String(cipherText);
            byte[] array2 = new byte[16];
            byte[] array3 = new byte[array.Length - array2.Length];
            Buffer.BlockCopy(array, 0, array2, 0, array2.Length);
            Buffer.BlockCopy(array, array2.Length, array3, 0, array.Length - array2.Length);
            byte[] bytes = Encoding.UTF8.GetBytes(keyString);
            using (Aes aes = Aes.Create())
            {
                using (ICryptoTransform transform = aes.CreateDecryptor(bytes, array2))
                {
                    using (MemoryStream stream = new MemoryStream(array3))
                    {
                        using (CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(stream2))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}