using System.Collections.Generic;
using System.Text;
using Phemedrone.Cryptography;

namespace Phemedrone.Classes
{
    public class Asn1DerObject
    {
        public Asn1Der.Type Type { get; set; }
        public int Lenght { get; set; }
        public List<Asn1DerObject> Objects { get; }
        public byte[] Data { get; set; }

        public Asn1DerObject()
        {
            Objects = new List<Asn1DerObject>();
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            var data = new StringBuilder();
            switch (Type)
            {
                case Asn1Der.Type.Sequence:
                    str.AppendLine("SEQUENCE {");
                    break;
                case Asn1Der.Type.Integer:
                    foreach (var octet in Data)
                    {
                        data.AppendFormat("{0:X2}", octet);
                    }
                    str.AppendLine("\tINTEGER " + data);
                    break;
                case Asn1Der.Type.OctetString:

                    foreach (var octet in Data)
                    {
                        data.AppendFormat("{0:X2}", octet);
                    }
                    str.AppendLine("\tOCTETSTRING " + data);
                    break;
                case Asn1Der.Type.ObjectIdentifier:
                    foreach (var octet in Data)
                    {
                        data.AppendFormat("{0:X2}", octet);
                    }
                    str.AppendLine("\tOBJECTIDENTIFIER " + data);
                    break;
            }
            foreach (var obj in Objects)
            {
                str.Append(obj);
            }

            if (Type.Equals(Asn1Der.Type.Sequence))
            {
                str.AppendLine("}");
            }
            return str.ToString();
        }
    }
}