using System.Net;

namespace Apiconvert.Api.Converters;

public static class ForwardUrlValidator
{
    public static (bool Ok, string? Error) Validate(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return (false, "Forward URL is invalid.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return (false, "Forward URL must use http or https.");
        }

        var host = uri.Host.ToLowerInvariant();
        if (host == "localhost" || host.EndsWith(".localhost") || host.EndsWith(".local"))
        {
            return (false, "Forward URL cannot target local hosts.");
        }

        if (IPAddress.TryParse(host, out var address))
        {
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                IsPrivateIpv4(address))
            {
                return (false, "Forward URL cannot target private IPv4 ranges.");
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                IsPrivateIpv6(address))
            {
                return (false, "Forward URL cannot target private IPv6 ranges.");
            }
        }

        return (true, null);
    }

    private static bool IsPrivateIpv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            127 => true,
            169 when bytes[1] == 254 => true,
            192 when bytes[1] == 168 => true,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
            _ => false
        };
    }

    private static bool IsPrivateIpv6(IPAddress address)
    {
        if (IPAddress.IPv6Loopback.Equals(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            0xfc or 0xfd => true,
            0xfe when (bytes[1] & 0xc0) == 0x80 => true,
            _ => false
        };
    }
}
