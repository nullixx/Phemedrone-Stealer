using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Phemedrone.Extensions
{
    public class ZipStorage : IDisposable
    {
        public enum Compression : ushort 
        {
            Store = 0,
            Deflate = 8 
        }
        
        public class ZipFileEntry
        {
            public Compression Method;
            public string FilenameInZip;
            public long FileSize;
            public long CompressedSize;
            public long HeaderOffset;
            public uint Crc32;
            public DateTime ModifyTime;
            public DateTime CreationTime;
            public DateTime AccessTime;
            public string Comment;
            public bool EncodeUTF8;
            
            public override string ToString()
            {
                return FilenameInZip;
            }
        }

        private const bool EncodeUtf8 = false;
        private const bool ForceDeflating = false;

        private readonly List<ZipFileEntry> _files = new List<ZipFileEntry>();
        private Stream _zipFileStream;
        private string _comment = string.Empty;
        private readonly byte[] _centralDirImage;
        private readonly long _existingFiles;
        private FileAccess _access;
        private bool _leaveOpen;
        private bool _isDisposed;
        private static readonly uint[] CrcTable;
        private static readonly Encoding DefaultEncoding = Encoding.GetEncoding(437);
        
        static ZipStorage()
        {
            CrcTable = new uint[256];
            for (var i = 0; i < CrcTable.Length; i++)
            {
                var c = (uint)i;
                for (var j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                        c = 3988292384 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                CrcTable[i] = c;
            }
        }

        public static ZipStorage Create(Stream stream, string comment = null, bool leaveOpen = false)
        {
            var zip = new ZipStorage()
            {
                _comment = comment ?? string.Empty,
                _zipFileStream = stream,
                _access = FileAccess.Write,
                _leaveOpen = leaveOpen
            };

            return zip;
        }

        public ZipFileEntry AddStream(Compression method, string filenameInZip, Stream source, DateTime modTime, string comment = null)
        {
            return AddStreamAsync(method, filenameInZip, source, modTime, comment);
        }
        
        private ZipFileEntry AddStreamAsync(Compression method, string filenameInZip, Stream source, DateTime modTime, string comment = null)
        {
            if (_access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not allowed");
            
            var zfe = new ZipFileEntry()
            {
                Method = method,
                EncodeUTF8 = EncodeUtf8,
                FilenameInZip = NormalizedFilename(filenameInZip),
                Comment = comment ?? string.Empty,
                Crc32 = 0,
                HeaderOffset = (uint)_zipFileStream.Position,
                CreationTime = modTime,
                ModifyTime = modTime,
                AccessTime = modTime
            };
            
            WriteLocalHeader(zfe);

            Store(zfe, source);

            source.Close();
            UpdateCrcAndSizes(zfe);
            _files.Add(zfe);

            return zfe;
        }

        private void Close()
        {
            if (_access != FileAccess.Read)
            {
                var centralOffset = (uint)_zipFileStream.Position;
                uint centralSize = 0;

                if (_centralDirImage != null)
                    _zipFileStream.Write(_centralDirImage, 0, _centralDirImage.Length);

                foreach (var f in _files)
                {
                    var pos = _zipFileStream.Position;
                    WriteCentralDirRecord(f);
                    centralSize += (uint)(_zipFileStream.Position - pos);
                }

                if (_centralDirImage != null)
                    WriteEndRecord(centralSize + (uint)_centralDirImage.Length, centralOffset);
                else
                    WriteEndRecord(centralSize, centralOffset);
            }

            if (_zipFileStream == null || _leaveOpen) return;
            
            _zipFileStream.Flush();
            _zipFileStream.Dispose();
            _zipFileStream = null;
        }

        private void WriteLocalHeader(ZipFileEntry zfe)
        {
            var encoder = zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            var encodedFilename = encoder.GetBytes(zfe.FilenameInZip);
            var extraInfo = CreateExtraInfo(zfe);

            _zipFileStream.Write(new byte[] { 80, 75, 3, 4, 20, 0}, 0, 6);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)(zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)zfe.Method), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(zfe.ModifyTime)), 0, 4);
            _zipFileStream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 12);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)extraInfo.Length), 0, 2);

            _zipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            _zipFileStream.Write(extraInfo, 0, extraInfo.Length);
        }

        private void WriteCentralDirRecord(ZipFileEntry zfe)
        {
            var encoder = zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            var encodedFilename = encoder.GetBytes(zfe.FilenameInZip);
            var encodedComment = encoder.GetBytes(zfe.Comment);
            var extraInfo = CreateExtraInfo(zfe);

            _zipFileStream.Write(new byte[] { 80, 75, 1, 2, 23, 0xB, 20, 0 }, 0, 8);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)(zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)zfe.Method), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(zfe.ModifyTime)), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes(zfe.Crc32), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes(Get32BitSize(zfe.CompressedSize)), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes(Get32BitSize(zfe.FileSize)), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)extraInfo.Length), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);

            _zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)0x8100), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes(Get32BitSize(zfe.HeaderOffset)), 0, 4);

            _zipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            _zipFileStream.Write(extraInfo, 0, extraInfo.Length);
            _zipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        private static uint Get32BitSize(long size)
        {
            return size >= 0xFFFFFFFF ? 0xFFFFFFFF : (uint)size;
        }

        private void WriteEndRecord(long size, long offset)
        {
            var dirOffset = _zipFileStream.Length;
            
            _zipFileStream.Position = dirOffset;
            _zipFileStream.Write(new byte[] { 80, 75, 6, 6 }, 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes((long)44), 0, 8);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)45), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)45), 0, 2);
            _zipFileStream.Write(BitConverter.GetBytes((uint)0), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes((uint)0), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes(_files.Count + _existingFiles), 0, 8);
            _zipFileStream.Write(BitConverter.GetBytes(_files.Count+_existingFiles), 0, 8);
            _zipFileStream.Write(BitConverter.GetBytes(size), 0, 8);
            _zipFileStream.Write(BitConverter.GetBytes(offset), 0, 8);
            
            _zipFileStream.Write(new byte[] { 80, 75, 6, 7 }, 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes((uint)0), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes(dirOffset), 0, 8);
            _zipFileStream.Write(BitConverter.GetBytes((uint)1), 0, 4);

            var encoder = DefaultEncoding;
            var encodedComment = encoder.GetBytes(_comment);

            _zipFileStream.Write(new byte[] { 80, 75, 5, 6, 0, 0, 0, 0 }, 0, 8);
            _zipFileStream.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 0, 12);
            _zipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);
            _zipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }
        
        private Compression Store(ZipFileEntry zfe, Stream source)
        {
            var buffer = new byte[16384];
            int bytesRead;
            uint totalRead = 0;

            var posStart = _zipFileStream.Position;
            var sourceStart = source.CanSeek ? source.Position : 0;

            var outStream = zfe.Method == Compression.Store ? _zipFileStream : new DeflateStream(_zipFileStream, CompressionMode.Compress, true);

            zfe.Crc32 = 0 ^ 0xffffffff;
            
            do
            {
                bytesRead = source.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                    outStream.Write(buffer, 0, bytesRead);

                for (uint i = 0; i < bytesRead; i++)
                {
                    zfe.Crc32 = CrcTable[(zfe.Crc32 ^ buffer[i]) & 0xFF] ^ (zfe.Crc32 >> 8);
                }

                totalRead += (uint)bytesRead;
            } while (bytesRead > 0);

            outStream.Flush();

            if (zfe.Method == Compression.Deflate)
                outStream.Dispose();

            zfe.Crc32 ^= 0xFFFFFFFF;
            zfe.FileSize = totalRead;
            zfe.CompressedSize = (uint)(_zipFileStream.Position - posStart);
            
            if (zfe.Method != Compression.Deflate || ForceDeflating || !source.CanSeek ||
                zfe.CompressedSize <= zfe.FileSize) return zfe.Method;
            
            zfe.Method = Compression.Store;
            _zipFileStream.Position = posStart;
            _zipFileStream.SetLength(posStart);
            source.Position = sourceStart;
            
            return Store(zfe, source);
        }
        
        private uint DateTimeToDosTime(DateTime dt)
        {
            return (uint)(
                (dt.Second / 2) | (dt.Minute << 5) | (dt.Hour << 11) | 
                (dt.Day<<16) | (dt.Month << 21) | ((dt.Year - 1980) << 25));
        }

        private static byte[] CreateExtraInfo(ZipFileEntry zfe)
        {
            var buffer = new byte[36+36];
            BitConverter.GetBytes((ushort)0x0001).CopyTo(buffer, 0);
            BitConverter.GetBytes((ushort)32).CopyTo(buffer, 2);
            BitConverter.GetBytes((ushort)1).CopyTo(buffer, 8); 
            BitConverter.GetBytes((ushort)24).CopyTo(buffer, 10); 
            BitConverter.GetBytes(zfe.FileSize).CopyTo(buffer, 12); 
            BitConverter.GetBytes(zfe.CompressedSize).CopyTo(buffer, 20); 
            BitConverter.GetBytes(zfe.HeaderOffset).CopyTo(buffer, 28);

            BitConverter.GetBytes((ushort)0x000A).CopyTo(buffer, 36);
            BitConverter.GetBytes((ushort)32).CopyTo(buffer, 38);
            BitConverter.GetBytes((ushort)1).CopyTo(buffer, 44);
            BitConverter.GetBytes((ushort)24).CopyTo(buffer, 46);
            BitConverter.GetBytes(zfe.ModifyTime.ToFileTime()).CopyTo(buffer, 48);
            BitConverter.GetBytes(zfe.AccessTime.ToFileTime()).CopyTo(buffer, 56);
            BitConverter.GetBytes(zfe.CreationTime.ToFileTime()).CopyTo(buffer, 64);

            return buffer;
        }
        
        private void UpdateCrcAndSizes(ZipFileEntry zfe)
        {
            var lastPos = _zipFileStream.Position; 

            _zipFileStream.Position = zfe.HeaderOffset + 8;
            _zipFileStream.Write(BitConverter.GetBytes((ushort)zfe.Method), 0, 2);

            _zipFileStream.Position = zfe.HeaderOffset + 14;
            _zipFileStream.Write(BitConverter.GetBytes(zfe.Crc32), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes(Get32BitSize(zfe.CompressedSize)), 0, 4);
            _zipFileStream.Write(BitConverter.GetBytes(Get32BitSize(zfe.FileSize)), 0, 4);

            _zipFileStream.Position = lastPos;
        }
        
        private string NormalizedFilename(string filename)
        {
            filename = filename.Replace('\\', '/');

            var pos = filename.IndexOf(':');
            if (pos >= 0)
                filename = filename.Remove(0, pos + 1);

            return filename.Trim('/');
        }

        public void Dispose() 
        {
            Dispose(true);

            GC.SuppressFinalize(this);      
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            
            if (disposing)
                Close();
 
            _isDisposed = true;
        }
    }
}