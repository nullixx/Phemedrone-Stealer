using System;
using System.Security.Cryptography;

namespace Phemedrone.Cryptography.Hashing
{
    public class PBE
    {
        private byte[] Ciphertext { get; }
        private byte[] GlobalSalt { get; }
        private byte[] MasterPass { get; }
        private byte[] EntrySalt { get; }
        private byte[] PartIv { get; }
        
        public PBE(byte[] ciphertext, byte[] globalSalt, byte[] masterPassword, byte[] entrySalt, byte[] partIv)
        {
            Ciphertext = ciphertext;
            GlobalSalt = globalSalt;
            MasterPass = masterPassword;
            EntrySalt = entrySalt;
            PartIv = partIv;
        }
        public byte[] Compute()
        {
            byte[] glmp, hp, iv, key;
            
            const int iterations = 1;
            const int keyLength = 32;
            
            glmp = new byte[GlobalSalt.Length + MasterPass.Length];
            Buffer.BlockCopy(GlobalSalt, 0, glmp, 0, GlobalSalt.Length);
            Buffer.BlockCopy(MasterPass, 0, glmp, GlobalSalt.Length, MasterPass.Length);
            hp = new SHA1Managed().ComputeHash(glmp);
            var ivPrefix = new byte[2] { 0x04, 0x0e };
            iv = new byte[ivPrefix.Length + PartIv.Length];
            Buffer.BlockCopy(ivPrefix, 0, iv, 0, ivPrefix.Length);
            Buffer.BlockCopy(PartIv, 0, iv, ivPrefix.Length, PartIv.Length);
            var df = new PBKDF2(new HMACSHA256(), hp, EntrySalt, iterations);
            key = df.GetBytes(keyLength);
            var aes = new AesManaged
            {
                Mode = CipherMode.CBC,
                BlockSize = 128,
                KeySize = 256,
                Padding = PaddingMode.Zeros,
            };
            var aesDecrypt = aes.CreateDecryptor(key, iv);
            var clear = aesDecrypt.TransformFinalBlock(Ciphertext, 0, this.Ciphertext.Length);
            return clear;
        }
    }
}