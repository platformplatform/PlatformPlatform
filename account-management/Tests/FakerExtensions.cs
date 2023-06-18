using Bogus;

namespace PlatformPlatform.AccountManagement.Tests;

public static class FakerExtensions
{
    public static string PhoneNumber(this Faker faker)
    {
        var random = new Random();
        return $"+{random.Next(1, 9)}-{faker.Phone.PhoneNumberFormat()}";
    }
}