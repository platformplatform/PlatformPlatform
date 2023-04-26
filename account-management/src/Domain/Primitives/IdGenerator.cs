using System.Net;
using System.Net.Sockets;

namespace PlatformPlatform.AccountManagement.Domain.Primitives;

/// <summary>
///     IdGenerator is a utility that can generate IDs in a low-latency, distributed, uncoordinated, (roughly) time
///     ordered manner, in highly available system. It uses the Twitter Snowflake algorithm (implemented by the
///     IdGen NuGet package) to ensure uniqueness across distributed systems, such as web farms. Each machine in the
///     distributed system is assigned a unique generator ID based on its IPv4 address. This allows generating up
///     to 4096 unique IDs per millisecond or 4 billion IDs per second, while maintaining the order of creation.
/// </summary>
public static class IdGenerator
{
    private static readonly IdGen.IdGenerator Generator;

    static IdGenerator()
    {
        var generatorId = GetUniqueGeneratorIdFromIpAddress();
        Generator = new IdGen.IdGenerator(generatorId);
    }

    /// <summary>
    ///     Generates a new unique ID based on the Twitter Snowflake algorithm.
    /// </summary>
    public static long NewId()
    {
        return Generator.CreateId();
    }

    /// <summary>
    ///     Retrieves a unique generator ID based on the machine's IPv4 address.
    /// </summary>
    private static int GetUniqueGeneratorIdFromIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        if (ipAddress == null)
        {
            throw new Exception(
                "No network adapters with an IPv4 address in the system. IdGenerator is meant to create unique IDs across multiple machines, and requires an IP address to do so.");
        }

        var lastSegment = ipAddress.ToString().Split('.').Last();
        return int.Parse(lastSegment);
    }
}