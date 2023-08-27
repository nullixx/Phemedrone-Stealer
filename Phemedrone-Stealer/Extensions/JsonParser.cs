using System;

namespace Phemedrone.Extensions
{
    public class JsonParser
    {
        private int _offset;
        public JsonParser()
        {
            _offset = 0;
        }
        // a method to parse json strings literally from scratch (i do not trust indexof method)
        public string ParseString(string key, string data, bool useOffset = false)
        {
            if (useOffset) data = data.Substring(_offset, data.Length - _offset);
            int startPos;
            var matchCombo = startPos = 0;
            for (var cursor = 0; cursor < data.Length; cursor++)
            {
                if (data[cursor] == key[matchCombo])
                {
                    if (matchCombo + 1 == key.Length)
                    {
                        startPos = cursor + 3; // skipping current character, " and :
                        break;
                    }

                    matchCombo++;
                }
                else
                {
                    matchCombo = 0;
                }
            }

            if (startPos == 0) return string.Empty;

            var result = string.Empty;
            var stringStarted = false;
            var trimmed = data.Substring(startPos, data.Length - startPos);
            for (var cursor = 0; cursor < trimmed.Length; cursor++)
            {
                if (!stringStarted && trimmed[cursor] == '\"')
                {
                    stringStarted = true;
                }
                else if (stringStarted)
                {
                    if (trimmed[cursor] == '\"' && trimmed[cursor - 1] != '\\')
                    {
                        _offset += startPos + ++cursor;
                        break;
                    }

                    result += trimmed[cursor];
                }
            }

            return result;
        }
    }
}