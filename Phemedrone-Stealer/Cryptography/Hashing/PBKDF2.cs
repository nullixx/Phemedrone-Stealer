using System;
using System.Security.Cryptography;

namespace Phemedrone.Cryptography.Hashing
{
    public class PBKDF2
    {
        private readonly int _blockSize;
        private uint _blockIndex = 1;
        private byte[] _bufferBytes;
        private int _bufferStartIndex;
        private int _bufferEndIndex;
        private HMAC Algorithm { get; }
        private byte[] Salt { get; }
        private int IterationCount { get; }
        public PBKDF2(HMAC algorithm, byte[] password, byte[] salt, int iterations)
        {
            Algorithm = algorithm ?? throw new ArgumentNullException("algorithm", "Algorithm cannot be null.");
            Algorithm.Key = password ?? throw new ArgumentNullException("password", "Password cannot be null.");
            Salt = salt ?? throw new ArgumentNullException("salt", "Salt cannot be null.");
            IterationCount = iterations;
            _blockSize = Algorithm.HashSize / 8;
            _bufferBytes = new byte[_blockSize];
        }
        public byte[] GetBytes(int count)
        {
            var result = new byte[count];
            var resultOffset = 0;
            var bufferCount = _bufferEndIndex - _bufferStartIndex;

            if (bufferCount > 0)
            {
                if (count < bufferCount)
                {
                    Buffer.BlockCopy(_bufferBytes, _bufferStartIndex, result, 0, count);
                    _bufferStartIndex += count;
                    return result;
                }
                Buffer.BlockCopy(_bufferBytes, _bufferStartIndex, result, 0, bufferCount);
                _bufferStartIndex = _bufferEndIndex = 0;
                resultOffset += bufferCount;
            }

            while (resultOffset < count)
            {
                var needCount = count - resultOffset;
                _bufferBytes = Func();
                if (needCount > _blockSize)
                {
                    Buffer.BlockCopy(_bufferBytes, 0, result, resultOffset, _blockSize);
                    resultOffset += _blockSize;
                }
                else
                {
                    Buffer.BlockCopy(_bufferBytes, 0, result, resultOffset, needCount);
                    _bufferStartIndex = needCount;
                    _bufferEndIndex = _blockSize;
                    return result;
                }
            }
            return result;
        }
        private byte[] Func()
        {
            var hash1Input = new byte[Salt.Length + 4];
            Buffer.BlockCopy(Salt, 0, hash1Input, 0, Salt.Length);
            Buffer.BlockCopy(GetBytesFromInt(_blockIndex), 0, hash1Input, Salt.Length, 4);
            var hash1 = Algorithm.ComputeHash(hash1Input);

            var finalHash = hash1;
            for (var i = 2; i <= IterationCount; i++)
            {
                hash1 = Algorithm.ComputeHash(hash1, 0, hash1.Length);
                for (var j = 0; j < _blockSize; j++)
                {
                    finalHash[j] = (byte)(finalHash[j] ^ hash1[j]);
                }
            }
            if (_blockIndex == uint.MaxValue) { throw new InvalidOperationException("Derived key too long."); }
            _blockIndex += 1;

            return finalHash;
        }

        private static byte[] GetBytesFromInt(uint i)
        {
            var bytes = BitConverter.GetBytes(i);
            return BitConverter.IsLittleEndian ? new [] { bytes[3], bytes[2], bytes[1], bytes[0] } : bytes;
        }
    }
}