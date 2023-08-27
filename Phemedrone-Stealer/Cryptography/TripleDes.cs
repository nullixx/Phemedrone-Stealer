using System;
using System.IO;
using System.Security.Cryptography;

namespace Phemedrone.Cryptography
{
    internal class TripleDes
    {
        private byte[] CipherText { get; }
        private byte[] GlobalSalt { get; }
        private byte[] MasterPassword { get; }
        private byte[] EntrySalt { get; }
        public byte[] Key { get; private set; }
        public byte[] Vector { get; private set; }
        
        public TripleDes(byte[] cipherText, byte[] globalSalt, byte[] masterPass, byte[] entrySalt)
        {
            CipherText = cipherText;
            GlobalSalt = globalSalt;
            MasterPassword = masterPass;
            EntrySalt = entrySalt;
        }
        public TripleDes(byte[] globalSalt, byte[] masterPassword, byte[] entrySalt)
        {
            GlobalSalt = globalSalt;
            MasterPassword = masterPassword;
            EntrySalt = entrySalt;
        }
        public void ComputeVoid()
        {
            var sha = new SHA1CryptoServiceProvider();
            byte[] glmp, hp, hpes, chp, pes, peses, k1, tk, k2, k;
            
            glmp = new byte[GlobalSalt.Length + MasterPassword.Length];
            Array.Copy(GlobalSalt, 0, glmp, 0, GlobalSalt.Length);
            Array.Copy(MasterPassword, 0, glmp, GlobalSalt.Length, MasterPassword.Length);
            hp = sha.ComputeHash(glmp);
            hpes = new byte[hp.Length + EntrySalt.Length];
            Array.Copy(hp, 0, hpes, 0, hp.Length);
            Array.Copy(EntrySalt, 0, hpes, hp.Length, EntrySalt.Length);
            chp = sha.ComputeHash(hpes);
            pes = new byte[20];
            Array.Copy(EntrySalt, 0, pes, 0, EntrySalt.Length);
            for (var i = EntrySalt.Length; i < 20; i++)
            {
                pes[i] = 0;
            }
            peses = new byte[pes.Length + EntrySalt.Length];
            Array.Copy(pes, 0, peses, 0, pes.Length);
            Array.Copy(EntrySalt, 0, peses, pes.Length, EntrySalt.Length);

            using (var hmac = new HMACSHA1(chp))
            {
                k1 = hmac.ComputeHash(peses);
                tk = hmac.ComputeHash(pes);
                var tkEs = new byte[tk.Length + EntrySalt.Length];
                Array.Copy(tk, 0, tkEs, 0, tk.Length);
                Array.Copy(EntrySalt, 0, tkEs, tk.Length, EntrySalt.Length);
                k2 = hmac.ComputeHash(tkEs);
            }
            k = new byte[k1.Length + k2.Length];
            Array.Copy(k1, 0, k, 0, k1.Length);
            Array.Copy(k2, 0, k, k1.Length, k2.Length);

            Key = new byte[24];

            for (var i = 0; i < Key.Length; i++)
            {
                Key[i] = k[i];
            }

            Vector = new byte[8];
            var j = Vector.Length - 1;

            for (var i = k.Length - 1; i >= k.Length - Vector.Length; i--)
            {
                Vector[j] = k[i];
                j--;
            }
        }
        public byte[] Compute()
        {
            byte[] glmp, hp, hpes, chp, pes, peses, k1, tk, k2, k;
            
            glmp = new byte[GlobalSalt.Length + MasterPassword.Length];
            Buffer.BlockCopy(GlobalSalt, 0, glmp, 0, GlobalSalt.Length);
            Buffer.BlockCopy(MasterPassword, 0, glmp, GlobalSalt.Length, MasterPassword.Length);
            hp = new SHA1Managed().ComputeHash(glmp);
            hpes = new byte[hp.Length + EntrySalt.Length];
            Buffer.BlockCopy(hp, 0, hpes, 0, hp.Length);
            Buffer.BlockCopy(EntrySalt, 0, hpes, EntrySalt.Length, hp.Length);
            chp = new SHA1Managed().ComputeHash(hpes);
            pes = new byte[20];
            Array.Copy(EntrySalt, 0, pes, 0, EntrySalt.Length);
            for (var i = EntrySalt.Length; i < 20; i++)
            {
                pes[i] = 0;
            }
            peses = new byte[pes.Length + EntrySalt.Length];
            Array.Copy(pes, 0, peses, 0, pes.Length);
            Array.Copy(EntrySalt, 0, peses, pes.Length, EntrySalt.Length);

            using (var hmac = new HMACSHA1(chp))
            {
                k1 = hmac.ComputeHash(peses);
                tk = hmac.ComputeHash(pes);
                var tkEs = new byte[tk.Length + EntrySalt.Length];
                Buffer.BlockCopy(tk, 0, tkEs, 0, tk.Length);
                Buffer.BlockCopy(EntrySalt, 0, tkEs, tk.Length, EntrySalt.Length);
                k2 = hmac.ComputeHash(tkEs);
            }
            k = new byte[k1.Length + k2.Length];
            Array.Copy(k1, 0, k, 0, k1.Length);
            Array.Copy(k2, 0, k, k1.Length, k2.Length);
            Key = new byte[24];
            for (var i = 0; i < Key.Length; i++)
            {
                Key[i] = k[i];
            }
            Vector = new byte[8];
            var j = Vector.Length - 1;
            for (var i = k.Length - 1; i >= k.Length - Vector.Length; i--)
            {
                Vector[j] = k[i];
                j--;
            }
            var decryptedCiphertext = DecryptByteDesCbc(Key, Vector, CipherText);
            var clearText = new byte[24];
            Array.Copy(decryptedCiphertext, clearText, clearText.Length);
            return clearText;
        }
        
        public static string DecryptStringDesCbc(byte[] key, byte[] iv, byte[] input)
        {
            string plaintext;

            using (var tdsAlg = new TripleDESCryptoServiceProvider())
            {
                tdsAlg.Key = key;
                tdsAlg.IV = iv;
                tdsAlg.Mode = CipherMode.CBC;
                tdsAlg.Padding = PaddingMode.None;

                var decryptFunc = tdsAlg.CreateDecryptor(tdsAlg.Key, tdsAlg.IV);

                using (var msDecrypt = new MemoryStream(input))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptFunc, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        public static byte[] DecryptByteDesCbc(byte[] key, byte[] iv, byte[] input)
        {
            var decrypted = new byte[512];

            using (var tdsAlg = new TripleDESCryptoServiceProvider())
            {
                tdsAlg.Key = key;
                tdsAlg.IV = iv;
                tdsAlg.Mode = CipherMode.CBC;
                tdsAlg.Padding = PaddingMode.None;

                var decryptFunc = tdsAlg.CreateDecryptor(tdsAlg.Key, tdsAlg.IV);

                using (var msDecrypt = new MemoryStream(input))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptFunc, CryptoStreamMode.Read))
                    {
                        csDecrypt.Read(decrypted, 0, decrypted.Length);
                    }
                }

            }

            return decrypted;
        }
    }
}