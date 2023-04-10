namespace PlatformPlatform.AccountManagement.Domain.Primitives;

public static class IdGenerator
{
    private static readonly IdGen.IdGenerator Generator = new(0);

    public static long NewId()
    {
        return Generator.CreateId();
    }
}