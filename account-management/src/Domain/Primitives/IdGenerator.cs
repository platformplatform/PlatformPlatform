using System.Net;
using System.Net.Sockets;

namespace PlatformPlatform.AccountManagement.Domain.Primitives;

public static class IdGenerator
{
    private static readonly IdGen.IdGenerator Generator;

    static IdGenerator()
    {
        var generatorId = GetUniqueGeneratorIdFromIpAddress();
        Generator = new IdGen.IdGenerator(generatorId);
    }

    public static long NewId()
    {
        return Generator.CreateId();
    }

    private static int GetUniqueGeneratorIdFromIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        if (ipAddress == null)
            throw new Exception(
                "No network adapters with an IPv4 address in the system. IdGenerator is meant to create unique IDs across multiple machines, and requires an IP address to do so.");

        var lastSegment = ipAddress.ToString().Split('.').Last();
        return int.Parse(lastSegment);
    }
}