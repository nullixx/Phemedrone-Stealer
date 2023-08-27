using System.Net;

namespace Phemedrone.Panel;

public class ComparableIpAddress : IPAddress, IComparable
{
    public ComparableIpAddress(byte[] address) : base(address) { }

    public int CompareTo(object? other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var thisBytes = GetAddressBytes();
        var otherBytes = (other as ComparableIpAddress)!.GetAddressBytes();

        if (thisBytes.Length != otherBytes.Length)
            throw new ArgumentException("IP addresses have different lengths");

        for (var i = 0; i < thisBytes.Length; i++)
        {
            if (thisBytes[i] < otherBytes[i])
                return -1;
            if (thisBytes[i] > otherBytes[i])
                return 1;
        }

        return 0;
    }
}